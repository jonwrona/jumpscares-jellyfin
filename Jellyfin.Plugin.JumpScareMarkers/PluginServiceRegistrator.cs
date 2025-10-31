using Jellyfin.Plugin.JumpScareMarkers.Providers;
using Jellyfin.Plugin.JumpScareMarkers.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.JumpScareMarkers;

/// <summary>
/// Service registrator for Jump Scare Markers plugin.
/// Registers services with Jellyfin's dependency injection container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Register services as singletons (one instance shared across the application)
        serviceCollection.AddSingleton<ItemMatchingService>();
        serviceCollection.AddSingleton<CsvImporter>();
        serviceCollection.AddSingleton<ImportService>();

        // Register the segment provider
        serviceCollection.AddSingleton<IMediaSegmentProvider, JumpScareSegmentProvider>();
    }
}
