# Jellyfin Auto Tagger

Applies configured tags to media as it is added to selected libraries.

Intended for tag-based parental controls: map a library to a tag, then use that tag
in each user's **Allowed tags** / **Blocked tags** policy.

## Building

```bash
dotnet publish Jellyfin.Plugin.AutoTagger.csproj -c Release -o ./dist
```

Copy `dist/Jellyfin.Plugin.AutoTagger.dll` into a folder under your plugins directory:

| Platform | Path |
| --- | --- |
| Docker | `/config/plugins/AutoTagger/` |
| Linux | `/var/lib/jellyfin/plugins/AutoTagger/` |
| Windows | `%ProgramData%\Jellyfin\Server\plugins\AutoTagger\` |

Restart Jellyfin, then configure at **Dashboard → Plugins → Auto Tagger**.

For a proper packaged release with a repository manifest, use
[jprm](https://github.com/jellyfin/jprm) against `build.yaml`.

## Version matching

The `Jellyfin.Controller` / `Jellyfin.Model` versions in the csproj **must match your
server version**, or the plugin loads as `NotSupported`.

| Server | TargetFramework | Package version |
| --- | --- | --- |
| 10.11.x | `net9.0` | `10.11.x` |
| 10.10.x | `net8.0` | `10.10.x` |

Jellyfin 12.0 is in release candidate and drops the leading `10.`. It is an ABI break —
the 12.0 RC notes tell users to reinstall plugins from the unstable repository. Expect to
rebuild against the 12.0 packages when it ships.

## How it works

- `AutoTagService` is an `IHostedService` that subscribes to `ILibraryManager.ItemAdded`.
  (`IServerEntryPoint` was removed in 10.9 — don't use it.)
- `Tagger` resolves an item's libraries via `ILibraryManager.GetCollectionFolders`, unions
  the tags from every matching rule, and writes with `UpdateToRepositoryAsync`.
- `ApplyTagsTask` is a manual scheduled task that backfills items already in the library.

Tagging is additive. Existing tags are never removed.

## Things to verify on your server

**Whether episodes inherit series tags for blocking.** This is the key question for your
use case and it's worth testing directly rather than assuming: tag a series, then log in as
a restricted user and check whether individual episodes are hidden. If they aren't, enable
**Also tag seasons and episodes**. Be aware that writes one row per episode.

**Metadata refresh behaviour.** `LockTags` is on by default because a refresh with
"replace existing metadata" can otherwise clear the Tags field. If you'd rather manage tags
by hand later, turn it off — a locked field can't be edited from the UI until unlocked.

**ItemAdded timing.** The event fires when the item record is created, which can be before
the metadata provider has finished. If you see tags occasionally not sticking, the next step
is to also implement `ILibraryPostScanTask` and re-apply after each scan completes.

## Namespace notes

Two imports are the most likely to need adjusting if you target a different SDK version:

- `Jellyfin.Data.Enums.BaseItemKind` — still present in 10.11, but the 10.11 database
  rewrite moved neighbouring types around, so confirm against your package.
- `MediaBrowser.Controller.Library.ItemUpdateType`

If the build fails on either, the fastest fix is to open the NuGet package in a decompiler
and search for the type name.
