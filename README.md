# Jellyfin Auto Tagger

Automatically tags media as it's added to a library.

Specify a library, enter the tags you want applied to everything in it, and Auto Tagger applies the tags as new media arrives. This pairs well with Jellyfin's built-in parental controls: tag a library, then set that tag under a user's **Allowed tags** or **Blocked tags** and their access follows the library without any manual upkeep.

Tags are only ever added. Anything already on an item reamins as-is.

## Install

Add this repository to Jellyfin:

**Dashboard → Plugins → Repositories → +**

```
https://pusta.github.io/jellyfin-plugins/manifest.json
```

Then go to **Catalog**, find **Auto Tagger**, and install it. Restart Jellyfin when prompted.

## Setup

**Dashboard → Plugins → Auto Tagger**

Tags are applied on a per-library basis. Type the tags you want applied to that library, separated by commas. Leave a library blank and the plug-in will ignore that library.

```
Kids Movies     →  kids-ok, preschool
Family TV       →  kids-ok
Movies          →
```

Two additional options:

**Also tag seasons and episodes.** Off by default, so only movies and series get tagged. Turn this on if you find that blocking a series by tag doesn't hide its individual episodes. This will take a long time to tag every epiosde in larger libraries.

**Lock the Tags field after tagging.** On by default. This stops a metadata refresh from wiping the tags back off. The tradeoff is that you can't edit tags by hand in the Jellyfin UI until you unlock the field on that item.


## Tagging what's already there

The plugin only sees media as it's added, so anything already in your libraries won't have tags yet. To add tags to existing items:

**Dashboard → Scheduled Tasks → Apply auto-tags to existing items → Run**

Progress shows in the dashboard. It's a one-time process after you set up your rules, though it's safe to run again whenever.

## Notes

Tagging isn't instant. New items go into a queue behind whatever else the library scan is doing, so give it a minute before assuming something's wrong. The Jellyfin log shows a line for each item tagged if you want to watch it happen.

Requires Jellyfin 10.11.x. Older versions need a build against their own SDK.

## Building from source

```bash
dotnet publish --configuration=Release Jellyfin.Plugin.AutoTagger.sln
```

Place `Jellyfin.Plugin.AutoTagger.dll` into a subfolder of your plugins directory and restart:

| Platform | Path |
| --- | --- |
| Docker | `/config/plugins/AutoTagger/` |
| Linux | `/var/lib/jellyfin/plugins/AutoTagger/` |
| Windows | `%ProgramData%\Jellyfin\Server\plugins\AutoTagger\` |

The `Jellyfin.Controller` and `Jellyfin.Model` versions in the csproj have to match your server, or the plugin shows up as `NotSupported` and won't load.

## License

GPL-3.0