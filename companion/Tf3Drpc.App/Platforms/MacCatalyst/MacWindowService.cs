using System.Diagnostics.CodeAnalysis;

using Foundation;

using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

using Tf3Drpc.App.Constants;
using Tf3Drpc.App.Services.Interfaces;

using static Tf3Drpc.App.Utilities.ObjCRuntimeUtils;

namespace Tf3Drpc.App.Platforms.MacCatalyst;

/// <summary>
/// Owns the AppKit window behind the settings window: it starts hidden and closing only hides it.
/// </summary>
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "The service and its close target live as long as the app; the container holds it as an instance and never disposes it.")]
internal sealed class MacWindowService : IWindowService
{
    private const nint CloseButton = 0;
    private const nint MoveToActiveSpace = 1 << 1;
    private const nint FullScreenAuxiliary = 1 << 8;
    private const string LightAppearance = "NSAppearanceNameAqua";
    private const string DarkAppearance = "NSAppearanceNameDarkAqua";
    private const string WindowUpdatedNotification = "NSWindowDidUpdateNotification";
    private readonly CloseTarget _closeTarget;
    private NSObject? _updateObserver;
    private nint _window;
    private bool _showWhenReady;

    public MacWindowService()
        => _closeTarget = new CloseTarget(this);

    // AppKit's NSApp.
    private static nint SharedApplication
        => Send(GetClass("NSApplication"), "sharedApplication");

    /// <summary>
    /// The handler for MAUI's <c>SceneOnActivated</c> lifecycle hook; takes over the window as soon as AppKit has created it.
    /// </summary>
    public void Attach()
    {
        if (_window != 0 || _updateObserver is not null)
            return;

        nint window = FindSettingsWindow();

        if (window != 0)
        {
            TakeOver(window);

            return;
        }

        // The scene is active before AppKit creates its window; every window posts this notification once per event loop pass.
        using NSString name = new(WindowUpdatedNotification);
        _updateObserver = NSNotificationCenter.DefaultCenter.AddObserver(name, OnWindowUpdated);
    }

    public void ShowSettings()
        => MainThread.BeginInvokeOnMainThread(ShowOnUiThread);

    // The app's only window that can become the main window; the menu bar icon has a window of its own that can't.
    private static nint FindSettingsWindow()
    {
        nint windows = Send(SharedApplication, "windows");
        nint count = GetInteger(windows, "count");

        for (nint i = 0; i < count; i++)
        {
            nint window = Send(windows, "objectAtIndex:", i);

            if (GetBoolean(window, "canBecomeMainWindow"))
                return window;
        }

        return 0;
    }

    private void OnWindowUpdated(NSNotification notification)
    {
        nint window = notification.Object?.Handle ?? 0;

        if (window == 0 || !GetBoolean(window, "canBecomeMainWindow") || _updateObserver is null)
            return;

        NSNotificationCenter.DefaultCenter.RemoveObserver(_updateObserver);
        _updateObserver.Dispose();
        _updateObserver = null;
        TakeOver(window);
    }

    private void TakeOver(in nint window)
    {
        _window = Send(window, "retain");

        // No window state to restore, and no more frame saves: UIKit writes the first frame to the app's preferences
        // before the app gets the window, but moving and resizing no longer update it.
        SendBoolean(_window, "setRestorable:", false);
        using NSString noAutosave = new(string.Empty);
        Send(_window, "setFrameAutosaveName:", noAutosave.Handle);
        nint closeButton = Send(_window, "standardWindowButton:", CloseButton);
        Send(closeButton, "setTarget:", _closeTarget.Handle);
        Send(closeButton, "setAction:", ObjCRuntime.Selector.GetHandle(CloseTarget.CloseAction));

        // The game runs in a full-screen Space of its own, and macOS doesn't switch Spaces for a menu bar app; the window
        // comes to the Space the player is on, also over the game.
        nint behaviour = GetInteger(_window, "collectionBehavior") | MoveToActiveSpace | FullScreenAuxiliary;
        SendInteger(_window, "setCollectionBehavior:", behaviour);

        // The title bar is AppKit's and doesn't follow UserAppTheme; a light theme on a dark system drew a white title on it.
        ApplyAppearance();

        if (Microsoft.Maui.Controls.Application.Current is Microsoft.Maui.Controls.Application application)
            application.RequestedThemeChanged += OnRequestedThemeChanged;

        // The native title bar shows the window title; the version goes below it, as the Windows title bar shows it.
        using NSString version = new(ThisAssembly.Info.InformationalVersion);
        Send(_window, "setSubtitle:", version.Handle);

        // Mac Catalyst ignores the start size of the MAUI window, so it is set here.
        SendSize(_window, "setContentSize:", new NativeSize(AppConstants.SettingsWindowWidth, AppConstants.SettingsWindowHeight));

        CenterOnPrimaryDisplay();

        if (_showWhenReady)
        {
            ShowOnUiThread();
        }
        else
        {
            Hide();
        }
    }

    private void Hide()
        => Send(_window, "orderOut:", 0);

    // As on Windows; AppKit's center uses the display of the active app. The first screen is the one with the menu bar.
    private void CenterOnPrimaryDisplay()
    {
        nint primary = Send(Send(GetClass("NSScreen"), "screens"), "objectAtIndex:", 0);
        NativeRect display = GetRect(primary, "visibleFrame");
        NativeRect frame = GetRect(_window, "frame");
        SendPoint(_window, "setFrameOrigin:", new NativePoint(display.X + ((display.Width - frame.Width) / 2), display.Y + ((display.Height - frame.Height) / 2)));
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        => ApplyAppearance();

    // The window's own appearance, so the native title bar matches the theme; none follows the system.
    private void ApplyAppearance()
    {
        string? name = Microsoft.Maui.Controls.Application.Current?.UserAppTheme switch
        {
            AppTheme.Light => LightAppearance,
            AppTheme.Dark => DarkAppearance,
            _ => null
        };

        if (name is null)
        {
            Send(_window, "setAppearance:", 0);

            return;
        }

        using NSString appearanceName = new(name);
        nint appearance = Send(GetClass("NSAppearance"), "appearanceNamed:", appearanceName.Handle);
        Send(_window, "setAppearance:", appearance);
    }

    private void ShowOnUiThread()
    {
        if (_window == 0)
        {
            _showWhenReady = true;

            return;
        }

        if (!GetBoolean(_window, "isVisible"))
            CenterOnPrimaryDisplay();

        // A menu bar app is not active on its own; without this the window opens behind the others.
        Send(SharedApplication, "activate");
        Send(_window, "makeKeyAndOrderFront:", 0);
    }

    /// <summary>
    /// The Objective-C target of the window's close button: it hides the window, so the app keeps running.
    /// </summary>
    [Register("Tf3DrpcCloseTarget")]
    private sealed class CloseTarget(MacWindowService owner) : NSObject
    {
        // AppKit also calls an action method that takes no sender.
        public const string CloseAction = "hideWindow";

        [Export(CloseAction)]
        public void HideWindow()
            => owner.Hide();
    }
}
