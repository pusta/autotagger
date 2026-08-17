using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoTagger.Services;

/// <summary>
/// Listens for newly added library items and tags them.
/// IServerEntryPoint was replaced by IHostedService in Jellyfin 10.9; older tutorials
/// showing the former will not work.
/// </summary>
public sealed class AutoTagService : IHostedService
{
    /// <summary>
    /// The update reasons that mean a metadata provider just wrote to the item. The plugin's
    /// own writes use MetadataEdit, which is deliberately absent here so that re-applying a
    /// tag cannot retrigger this handler.
    /// </summary>
    private const ItemUpdateType MetadataReasons =
        ItemUpdateType.MetadataImport | ItemUpdateType.MetadataDownload;

    private readonly ILibraryManager _libraryManager;
    private readonly Tagger _tagger;
    private readonly ILogger<AutoTagService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoTagService"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="tagger">The tagger.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
    public AutoTagService(
        ILibraryManager libraryManager,
        Tagger tagger,
        ILogger<AutoTagService> logger)
    {
        _libraryManager = libraryManager;
        _tagger = tagger;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded += OnItemAdded;
        _libraryManager.ItemUpdated += OnItemUpdated;
        _logger.LogInformation("Auto Tagger is listening for new library items");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded -= OnItemAdded;
        _libraryManager.ItemUpdated -= OnItemUpdated;
        return Task.CompletedTask;
    }

    /// <summary>
    /// ItemAdded fires as soon as the item row is written, which is before the metadata
    /// providers have run. Tags are applied here so an item that never triggers a refresh
    /// save still gets them, but the Tags field is left unlocked so the providers can
    /// still contribute their own; OnItemUpdated takes the lock afterwards.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The event arguments.</param>
    private void OnItemAdded(object? sender, ItemChangeEventArgs e)
    {
        Apply(e.Item, metadataSettled: false);
    }

    /// <summary>
    /// Runs once a metadata provider has saved the item. Tags merge additively unless the
    /// field is locked, so re-applying here restores a tag that a "replace all metadata"
    /// refresh discarded, and is the only point at which locking is safe.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The event arguments.</param>
    private void OnItemUpdated(object? sender, ItemChangeEventArgs e)
    {
        if ((e.UpdateReason & MetadataReasons) == 0)
        {
            return;
        }

        Apply(e.Item, metadataSettled: true);
    }

    /// <summary>
    /// Both events are raised synchronously on the library scan thread, so the work is
    /// pushed onto the thread pool. An exception must never escape back into the scanner.
    /// </summary>
    /// <param name="item">The item to tag.</param>
    /// <param name="metadataSettled">Whether the metadata providers have finished with the item.</param>
    private void Apply(BaseItem? item, bool metadataSettled)
    {
        if (item is null)
        {
            return;
        }

        // A scan raises these events for every item on the server. Bailing out before the
        // thread pool is involved keeps an unconfigured plugin off the scan's critical path.
        var configuration = Plugin.Instance?.Configuration;
        if (configuration is null || configuration.Rules.Length == 0)
        {
            return;
        }

        _ = Task.Run(
            async () =>
            {
                try
                {
                    await _tagger.ApplyAsync(item, metadataSettled, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to auto-tag {ItemName}", item.Name);
                }
            },
            CancellationToken.None);
    }
}
