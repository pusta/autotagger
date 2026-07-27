using Jellyfin.Plugin.AutoTagger.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.AutoTagger;

/// <summary>
/// Registers the plugin's services with the host container. Jellyfin discovers this by
/// reflection, so the signature must match exactly or the server throws
/// "RegisterServices does not have an implementation" at startup.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<Tagger>();
        serviceCollection.AddHostedService<AutoTagService>();
    }
}
