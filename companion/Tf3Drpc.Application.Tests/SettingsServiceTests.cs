using System;
using System.IO;

using CommunityToolkit.Mvvm.Messaging;

using Microsoft.Extensions.Logging.Abstractions;

using Tf3Drpc.Application.Messages;
using Tf3Drpc.Application.Services;
using Tf3Drpc.Application.Services.Interfaces;
using Tf3Drpc.Core.Constants;
using Tf3Drpc.Core.Enums;
using Tf3Drpc.Core.Settings;

using Xunit;

namespace Tf3Drpc.Application.Tests;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("tf3drpc-settings-").FullName;

    [Fact]
    public void ChangedSettingsAreSavedAndNotifiedButUnchangedSettingsAreNot()
    {
        TestPaths paths = CreatePaths();
        StrongReferenceMessenger messenger = new();
        Recipient recipient = new();
        messenger.Register<SettingsChangedMessage>(recipient);
        SettingsService service = new(paths, messenger, NullLogger<SettingsService>.Instance);

        Assert.True(service.SetLanguage("de"));
        Assert.True(service.SetPreset(PresencePreset.Network));
        Assert.True(service.SetTheme(ThemeMode.Dark));
        Assert.True(service.SetStoreButton(!service.Current.StoreButton));
        Assert.Equal(4, recipient.Count);
        Assert.Equal(service.Current, SettingsStore.Load(paths.SettingsFilePath));
        Assert.Equal(service.Current, recipient.Last);
        Assert.False(service.LastSaveFailed);
        Assert.True(service.CanSave);

        Assert.True(service.SetLanguage("de"));
        Assert.True(service.SetPreset(PresencePreset.Network));
        Assert.True(service.SetTheme(ThemeMode.Dark));
        Assert.True(service.SetStoreButton(service.Current.StoreButton));
        Assert.True(service.SetTemplates(service.Current.Templates));
        Assert.Equal(4, recipient.Count);
        Assert.Equal(0, paths.Probes);
    }

    [Fact]
    public void FailedSaveKeepsMemoryAndNotifiesThenUnchangedValueRetries()
    {
        TestPaths paths = CreatePaths();
        paths.IsDataWritable = false;
        StrongReferenceMessenger messenger = new();
        Recipient recipient = new();
        messenger.Register<SettingsChangedMessage>(recipient);
        SettingsService service = new(paths, messenger, NullLogger<SettingsService>.Instance);

        Assert.False(service.SetLanguage("de"));
        Assert.Equal("de", service.Current.Language);
        Assert.True(service.LastSaveFailed);
        Assert.False(File.Exists(paths.SettingsFilePath));
        Assert.Equal(1, paths.Probes);
        Assert.Equal(1, recipient.Count);

        paths.Repaired = true;
        Assert.True(service.SetLanguage("de"));
        Assert.False(service.LastSaveFailed);
        Assert.True(service.CanSave);
        Assert.Equal(2, paths.Probes);
        Assert.Equal(2, recipient.Count);
        Assert.Equal(service.Current, SettingsStore.Load(paths.SettingsFilePath));
    }

    [Fact]
    public void FileWriteFailureIsReportedEvenWhenDataProbeWasWritable()
    {
        TestPaths paths = CreatePaths();
        Directory.CreateDirectory(paths.SettingsFilePath);
        SettingsService service = new(paths, new StrongReferenceMessenger(), NullLogger<SettingsService>.Instance);

        Assert.False(service.SetLanguage("de"));
        Assert.True(service.LastSaveFailed);
        Assert.Equal(0, paths.Probes);

        Directory.Delete(paths.SettingsFilePath);
        Assert.True(service.SetLanguage("de"));
        Assert.False(service.LastSaveFailed);
        Assert.Equal("de", SettingsStore.Load(paths.SettingsFilePath).Language);
    }

    [Fact]
    public void TemplatesAreNormalisedAndInvalidLanguageDoesNotNotify()
    {
        TestPaths paths = CreatePaths();
        StrongReferenceMessenger messenger = new();
        Recipient recipient = new();
        messenger.Register<SettingsChangedMessage>(recipient);
        SettingsService service = new(paths, messenger, NullLogger<SettingsService>.Instance);
        PresenceTemplates templates = new() { Details = new string('x', PresenceConstants.MaxTemplateLength + 1) };

        Assert.True(service.SetTemplates(templates));
        Assert.Equal(PresenceConstants.MaxTemplateLength, service.Current.Templates.Details.Length);
        Assert.Equal(service.Current, SettingsStore.Load(paths.SettingsFilePath));
        Assert.True(service.SetTemplates(templates));
        Assert.Equal(1, recipient.Count);
        Assert.False(service.SetLanguage("invalid-language"));
        Assert.Equal(1, recipient.Count);
        Assert.Throws<ArgumentNullException>(() => service.SetTemplates(null!));

        SettingsService reloaded = new(paths, messenger, NullLogger<SettingsService>.Instance);
        Assert.Equal(service.Current, reloaded.Current);
    }

    public void Dispose()
        => Directory.Delete(_directory, recursive: true);

    private TestPaths CreatePaths()
        => new(Path.Combine(_directory, "settings.json"));

    private sealed class TestPaths(string path) : ISettingsPaths
    {
        public string SettingsFilePath { get; } = path;

        public bool IsDataWritable { get; set; } = true;

        public bool Repaired { get; set; }

        public int Probes { get; private set; }

        public bool RefreshDataWritable()
        {
            Probes++;
            IsDataWritable = Repaired;

            return IsDataWritable;
        }
    }

    internal sealed class Recipient : IRecipient<SettingsChangedMessage>
    {
        public int Count { get; private set; }

        public AppSettings? Last { get; private set; }

        public void Receive(SettingsChangedMessage message)
        {
            Count++;
            Last = message.Settings;
        }
    }
}
