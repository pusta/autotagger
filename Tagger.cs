using Jellyfin.Plugin.AutoTagger.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoTagger.Services;

/// <summary>
/// Decides which tags an item should carry and writes them to the repository.
/// </summary>
public class Tagger
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<Tagger> _logger;

    public Tagger(ILibraryManager libraryManager, ILogger<Tagger> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Movies and series always qualify. Seasons and episodes are opt-in because
    /// tagging every episode of a large series is a lot of database writes.
    /// </summary>
    public static bool IsTaggable(BaseItem item, PluginConfiguration config) => item switch
    {
        Movie or Series => true,
        Season or Episode => config.TagEpisodesAndSeasons,
        _ => false
    };

    /// <summary>
    /// Resolves the tags configured for whichever libraries contain this item.
    /// An item in two watched libraries gets the union of both rule sets.
    /// </summary>
    public IReadOnlyList<string> GetConfiguredTags(BaseItem item)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null || config.Rules.Length == 0)
        {
            return [];
        }

        var containingLibraries = _libraryManager.GetCollectionFolders(item);
        if (containingLibraries is null || containingLibraries.Count == 0)
        {
            return [];
        }

        var libraryIds = containingLibraries.Select(f => f.Id).ToHashSet();

        return config.Rules
            .Where(rule => Guid.TryParse(rule.LibraryId, out var id) && libraryIds.Contains(id))
            .SelectMany(rule => rule.Tags)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Applies the configured tags. Returns true if the item was written to.
    /// Existing tags are preserved — this only ever adds.
    /// </summary>
    public async Task<bool> ApplyAsync(BaseItem item, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null || !IsTaggable(item, config))
        {
            return false;
        }

        var wanted = GetConfiguredTags(item);
        if (wanted.Count == 0)
        {
            return false;
        }

        var existing = item.Tags ?? [];
        var missing = wanted
            .Where(tag => !existing.Contains(tag, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        var lockedFields = item.LockedFields ?? [];
        var needsLock = config.LockTags && !lockedFields.Contains(MetadataField.Tags);

        if (missing.Length == 0 && !needsLock)
        {
            return false;
        }

        if (missing.Length > 0)
        {
            item.Tags = [.. existing, .. missing];
        }

        if (needsLock)
        {
            item.LockedFields = [.. lockedFields, MetadataField.Tags];
        }

        await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Tagged {ItemName} ({ItemType}) with [{Tags}]",
            item.Name,
            item.GetType().Name,
            string.Join(", ", missing));

        return true;
    }
}
