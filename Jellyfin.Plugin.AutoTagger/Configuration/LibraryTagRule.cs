using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.AutoTagger.Configuration;

/// <summary>
/// A single watched library and the tags applied to items added to it.
/// </summary>
public class LibraryTagRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryTagRule"/> class.
    /// </summary>
    public LibraryTagRule()
    {
        LibraryId = string.Empty;
        LibraryName = string.Empty;
        Tags = [];
        ExcludeTags = [];
    }

    /// <summary>
    /// Gets or sets the collection folder id of the library, as a GUID string.
    /// This matches the ItemId returned by /Library/VirtualFolders.
    /// </summary>
    public string LibraryId { get; set; }

    /// <summary>
    /// Gets or sets the library display name, stored so the configuration page stays readable.
    /// </summary>
    public string LibraryName { get; set; }

    /// <summary>
    /// Gets or sets the tags to apply. Comparison is case-insensitive.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Plugin configuration is round-tripped through XmlSerializer, which requires a settable array.")]
    public string[] Tags { get; set; }

    /// <summary>
    /// Gets or sets the tags that suppress this rule. If an item already carries any of
    /// these tags, this library's tags are not applied to it. Comparison is case-insensitive.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Plugin configuration is round-tripped through XmlSerializer, which requires a settable array.")]
    public string[] ExcludeTags { get; set; }
}
