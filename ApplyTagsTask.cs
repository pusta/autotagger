using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AutoTagger.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoTagger.ScheduledTasks;

/// <summary>
/// The ItemAdded hook only catches new arrivals. Run this once after configuring
/// rules to tag everything already in the watched libraries.
/// </summary>
public class ApplyTagsTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly Tagger _tagger;
    private readonly ILogger<ApplyTagsTask> _logger;

    public ApplyTagsTask(
        ILibraryManager libraryManager,
        Tagger tagger,
        ILogger<ApplyTagsTask> logger)
    {
        _libraryManager = libraryManager;
        _tagger = tagger;
        _logger = logger;
    }

    public string Name => "Apply auto-tags to existing items";

    public string Key => "AutoTaggerApplyExisting";

    public string Description => "Tags items already present in the watched libraries.";

    public string Category => "Library";

    // No default schedule — this is a manual, run-when-you-need-it task.
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [];

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null || config.Rules.Length == 0)
        {
            _logger.LogInformation("No library rules configured, nothing to do");
            progress.Report(100);
            return;
        }

        var kinds = new List<BaseItemKind> { BaseItemKind.Movie, BaseItemKind.Series };
        if (config.TagEpisodesAndSeasons)
        {
            kinds.Add(BaseItemKind.Season);
            kinds.Add(BaseItemKind.Episode);
        }

        var rules = config.Rules
            .Where(rule => Guid.TryParse(rule.LibraryId, out _))
            .ToArray();

        var tagged = 0;
        var processed = 0;

        for (var i = 0; i < rules.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var libraryId = Guid.Parse(rules[i].LibraryId);
            var items = _libraryManager.GetItemList(new InternalItemsQuery
            {
                ParentId = libraryId,
                Recursive = true,
                IncludeItemTypes = [.. kinds]
            });

            _logger.LogInformation(
                "Checking {Count} items in {Library}",
                items.Count,
                rules[i].LibraryName);

            for (var j = 0; j < items.Count; j++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (await _tagger.ApplyAsync(items[j], cancellationToken).ConfigureAwait(false))
                {
                    tagged++;
                }

                processed++;

                // Progress is reported per-library so a big library doesn't stall the bar.
                var libraryFraction = items.Count == 0 ? 1d : (double)(j + 1) / items.Count;
                progress.Report(100d * (i + libraryFraction) / rules.Length);
            }
        }

        _logger.LogInformation("Auto Tagger updated {Tagged} of {Processed} items", tagged, processed);
        progress.Report(100);
    }
}
