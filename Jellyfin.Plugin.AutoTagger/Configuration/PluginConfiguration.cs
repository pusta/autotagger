using System.Diagnostics.CodeAnalysis;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.AutoTagger.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        Rules = [];
        TagEpisodesAndSeasons = false;
        LockTags = true;
    }

    /// <summary>
    /// Gets or sets the watched libraries and their tags. Libraries with no tags are not stored.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Plugin configuration is round-tripped through XmlSerializer, which requires a settable array.")]
    public LibraryTagRule[] Rules { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether seasons and episodes are tagged
    /// in addition to movies and series.
    /// </summary>
    public bool TagEpisodesAndSeasons { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Tags metadata field is locked after
    /// tagging, so that a later metadata refresh cannot clear it.
    /// </summary>
    public bool LockTags { get; set; }
}
