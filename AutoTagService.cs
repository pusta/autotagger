using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoTagger.Services;

/// <summary>
/// Listens for new library items and tags them.
/// IServerEntryPoint was replaced by IHostedService in Jellyfin 10.9 — do not use the old interface.
/// </summary>
public sealed class AutoTagService : IHostedService
{
    private readonly ILibraryManager _libraryManager;
    private readonly Tagger _tagger;
    private readonly ILogger<AutoTagService> _logger;

    public AutoTagService(
        ILibraryManager libraryManager,
        Tagger tagger,
        ILogger<AutoTagService> logger)
    {
        _libraryManager = libraryManager;
        _tagger = tagger;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded += OnItemAdded;
        _logger.LogInformation("Auto Tagger is listening for new library items");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded -= OnItemAdded;
        return Task.CompletedTask;
    }

    /// <summary>
    /// ItemAdded is a synchronous event raised on the scan thread, so the work is
    /// pushed onto the thread pool. Never let an exception escape back into the scanner.
    /// </summary>
    private void OnItemAdded(object? sender, ItemChangeEventArgs e)
    {
        var item = e.Item;
        if (item is null)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _tagger.ApplyAsync(item, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-tag {ItemName}", item.Name);
            }
        });
    }
}
