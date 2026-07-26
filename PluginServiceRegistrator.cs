using Jellyfin.Plugin.AutoTagger.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.AutoTagger;

/// <summary>
/// Jellyfin discovers this by reflection. The signature must match exactly or the
/// server throws "RegisterServices does not have an implementation" at startup.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<Tagger>();
        serviceCollection.AddHostedService<AutoTagService>();
    }
}
