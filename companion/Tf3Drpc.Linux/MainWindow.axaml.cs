using System.Globalization;

using Avalonia.Controls;
using Avalonia.Interactivity;

using Tf3Drpc.Core.Enums;
using Tf3Drpc.Core.Resources;

namespace Tf3Drpc.Linux;

/// <summary>
/// Keeps the desktop feasibility host accessible even when no system tray exists.
/// </summary>
internal sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public string ResourceSample { get; } = OwnTexts.Get(OwnText.StartingTheGame, CultureInfo.GetCultureInfo("de-DE"));

    private void Exit(object? sender, RoutedEventArgs args)
        => Close();
}
