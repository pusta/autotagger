using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.AutoTagger.Configuration;

/// <summary>
/// One library and the tags that should be applied to items added to it.
/// </summary>
public class LibraryTagRule
{
    /// <summary>
    /// The library's collection folder id, as a GUID string.
    /// Matches VirtualFolderInfo.ItemId from /Library/VirtualFolders.
    /// </summary>
    public string LibraryId { get; set; } = string.Empty;

    /// <summary>Display name, stored only so the config page stays readable.</summary>
    public string LibraryName { get; set; } = string.Empty;

    /// <summary>Tags to apply. Comparison is case-insensitive.</summary>
    public string[] Tags { get; set; } = [];
}

public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Libraries being watched. Libraries with no tags are not stored.</summary>
    public LibraryTagRule[] Rules { get; set; } = [];

    /// <summary>
    /// Tag seasons and episodes in addition to movies and series.
    /// Off by default — see the README on how Jellyfin evaluates tag-based blocking.
    /// </summary>
    public bool TagEpisodesAndSeasons { get; set; }

    /// <summary>
    /// Lock the Tags metadata field after tagging so a later refresh cannot clear it.
    /// </summary>
    public bool LockTags { get; set; } = true;
}
