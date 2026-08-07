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
    public string[] Tags { get; set; }
    public string[] ExcludeTags { get; set; }
}
