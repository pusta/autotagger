using System;
using System.Threading;
using System.Threading.Tasks;
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
        _logger.LogInformation("Auto Tagger is listening for new library items");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded -= OnItemAdded;
        return Task.CompletedTask;
    }

    /// <summary>
    /// ItemAdded is raised synchronously on the library scan thread, so the work is
    /// pushed onto the thread pool. An exception must never escape back into the scanner.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The event arguments.</param>
    private void OnItemAdded(object? sender, ItemChangeEventArgs e)
    {
        var item = e.Item;
        if (item is null)
        {
            return;
        }

        _ = Task.Run(
            async () =>
            {
                try
                {
                    await _tagger.ApplyAsync(item, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to auto-tag {ItemName}", item.Name);
                }
            },
            CancellationToken.None);
    }
}
