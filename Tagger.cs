using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="Tagger"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
    public Tagger(ILibraryManager libraryManager, ILogger<Tagger> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Determines whether an item is eligible for tagging. Movies and series always
    /// qualify; seasons and episodes are opt-in because tagging every episode of a
    /// large series is a lot of database writes.
    /// </summary>
    /// <param name="item">The item to test.</param>
    /// <param name="configuration">The current plugin configuration.</param>
    /// <returns><c>true</c> if the item should be considered for tagging.</returns>
    public static bool IsTaggable(BaseItem item, PluginConfiguration configuration)
    {
        return item switch
        {
            Movie or Series => true,
            Season or Episode => configuration.TagEpisodesAndSeasons,
            _ => false
        };
    }

    /// <summary>
    /// Resolves the tags configured for whichever libraries contain the item.
    /// An item present in two watched libraries receives the union of both rule sets.
    /// </summary>
    /// <param name="item">The item to resolve tags for.</param>
    /// <returns>The distinct tags configured for this item's libraries.</returns>
    public IReadOnlyList<string> GetConfiguredTags(BaseItem item)
    {
        var configuration = Plugin.Instance?.Configuration;
        if (configuration is null || configuration.Rules.Length == 0)
        {
            return [];
        }

        var containingLibraries = _libraryManager.GetCollectionFolders(item);
        if (containingLibraries is null || containingLibraries.Count == 0)
        {
            return [];
        }

        var libraryIds = containingLibraries.Select(folder => folder.Id).ToHashSet();

        return configuration.Rules
            .Where(rule => Guid.TryParse(rule.LibraryId, out var id) && libraryIds.Contains(id))
            .SelectMany(rule => rule.Tags)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Applies the configured tags to an item. Tagging is additive; existing tags
    /// are never removed.
    /// </summary>
    /// <param name="item">The item to tag.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> if the item was written to the repository.</returns>
    public async Task<bool> ApplyAsync(BaseItem item, CancellationToken cancellationToken)
    {
        var configuration = Plugin.Instance?.Configuration;
        if (configuration is null || !IsTaggable(item, configuration))
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
        var needsLock = configuration.LockTags && !lockedFields.Contains(MetadataField.Tags);

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
