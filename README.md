# Jellyfin Auto Tagger

Applies configured tags to media as it is added to selected libraries.

Intended for tag-based parental controls: map a library to a tag, then use that tag in each
user's **Allowed tags** / **Blocked tags** policy.

Built from the [official plugin template](https://github.com/jellyfin/jellyfin-plugin-template).

## Configuration

**Dashboard → Plugins → Auto Tagger.** Every library is listed with a text field; enter
comma-separated tags, or leave a library blank to skip it. Tagging is additive — existing
tags are never removed.

New items are tagged as they arrive. To tag items already in a library, run **Apply
auto-tags to existing items** from Dashboard → Scheduled Tasks.

## Building

```bash
dotnet publish --configuration=Release Jellyfin.Plugin.AutoTagger.sln
```

For local development, the template's VS Code tasks are included: set `jellyfinDir`,
`jellyfinWebDir`, and the data directory in `.vscode/settings.json`, then run the
**build-and-copy** task to build and drop the DLL straight into your server's plugin
directory.

To install manually, copy `Jellyfin.Plugin.AutoTagger.dll` into a folder under your
plugins directory:

| Platform | Path |
| --- | --- |
| Docker | `/config/plugins/AutoTagger/` |
| Linux | `/var/lib/jellyfin/plugins/AutoTagger/` |
| Windows | `%ProgramData%\Jellyfin\Server\plugins\AutoTagger\` |

For a packaged release with a repository manifest, use [jprm](https://github.com/jellyfin/jprm)
against `build.yaml`.

## Version matching

The `Jellyfin.Controller` / `Jellyfin.Model` versions in the csproj **must match your server
version** or the plugin loads as `NotSupported`.

| Server | TargetFramework | Package version |
| --- | --- | --- |
| 10.11.x | `net9.0` | `10.11.x` |
| 10.10.x | `net8.0` | `10.10.x` |

Note that the upstream template still pins `10.9.11`; this project bumps to `10.11.11`.

Jellyfin 12.0 is in release candidate and drops the leading `10.`. It is an ABI break — the
12.0 RC notes tell users to reinstall plugins from the unstable repository. Expect to rebuild
against the 12.0 packages when it ships.

## How it works

- `AutoTagService` is an `IHostedService` subscribed to `ILibraryManager.ItemAdded`.
  (`IServerEntryPoint` was replaced in 10.9 — older tutorials showing it will not work.)
- `Tagger` resolves an item's libraries via `ILibraryManager.GetCollectionFolders`, unions
  the tags from every matching rule, and writes with `UpdateToRepositoryAsync`.
- `ApplyTagsTask` is a manual scheduled task that backfills existing items.
- `PluginServiceRegistrator` wires both into the host container.

## Things to verify on your server

**Whether episodes inherit series tags for blocking.** This is the crux of the parental
control use case and is worth testing directly rather than assuming: tag a series, log in as
a restricted user, and check whether individual episodes are hidden. If they are not, enable
**Also tag seasons and episodes** — but that writes one row per episode.

**Metadata refresh behaviour.** `LockTags` defaults to on because a refresh with "replace
existing metadata" can otherwise clear the Tags field. A locked field cannot be edited from
the UI until it is unlocked.

**ItemAdded timing.** The event fires when the item record is created, which can precede the
metadata provider finishing. If tags occasionally do not stick, the next step is to also
implement `ILibraryPostScanTask` and re-apply after each scan.

## Notes on the analyzers

The template sets `TreatWarningsAsErrors` with `AnalysisMode=AllEnabledByDefault`, so a few
things are not optional:

- every public type and member needs an XML doc comment (`GenerateDocumentationFile` is on)
- no `ImplicitUsings` — `using` directives are explicit, `System` first, alphabetised
- one class per file (StyleCop SA1402), which is why `LibraryTagRule` has its own file
- `CA1819` is suppressed on the array-typed configuration properties, since `XmlSerializer`
  requires settable arrays

## License

GPL-3.0, inherited from the template.
