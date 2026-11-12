using System;

using CommunityToolkit.Mvvm.Messaging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Tf3Drpc.App.Services;
using Tf3Drpc.App.Services.Interfaces;
using Tf3Drpc.App.ViewModels;
using Tf3Drpc.App.Views;

namespace Tf3Drpc.App.Extensions;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the services; the platform objects are created early because MAUI's lifecycle hooks need them.
        /// </summary>
        public void AddAppServices
        (
            PlatformPathService pathService,
            PlatformSingleInstanceService singleInstanceService,
            PlatformWindowService windowService
        )
        {
            // These services do not depend on anything
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
            services.AddSingleton<IPathService>(pathService);
            services.AddSingleton<ISettingsPaths>(pathService);
            services.AddSingleton<ISingleInstanceService>(singleInstanceService);
            services.AddSingleton<IWindowService>(windowService);
            services.AddSingleton<IGameProcessService, GameProcessService>();
            services.AddSingleton<ITranslationService, TranslationService>();
            services.AddSingleton<IDiscordService, DiscordService>();

            // These services depend on the above services
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IPresenceService, PresenceService>();

            // IMonitorService depends on all of the above
            services.AddSingleton<IMonitorService>(static provider => new MonitorService
            (
                provider.GetRequiredService<IGameProcessService>(),
                provider.GetRequiredService<IPathService>(),
                provider.GetRequiredService<ISettingsService>(),
                provider.GetRequiredService<ITranslationService>(),
                provider.GetRequiredService<IPresenceService>(),
                provider.GetRequiredService<IDiscordService>(),
                provider.GetRequiredService<IMessenger>(),
                provider.GetRequiredService<TimeProvider>(),
                provider.GetRequiredService<ILoggerFactory>()
            ));

            // These services control the monitor
            services.AddSingleton<IAppLifecycleService, AppLifecycleService>();
            services.AddSingleton<ITrayService, PlatformTrayService>();
        }

        public void AddAppViewModels()
        {
            // The view model depends on the services and lives as long as the hidden window
            services.AddSingleton<SettingsViewModel>();

            // The page is resolved once for the window
            services.AddTransient<SettingsPage>();

            // The window is resolved once, when MAUI creates it, and takes the page
            services.AddTransient<SettingsWindow>();
        }
    }
}
