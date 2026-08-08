# Jellyfin Auto Tagger

Automatically tags media as it's added to a library.

Pick a library, pick the tags you want applied to everything in it, and Auto Tagger handles the rest as new media arrives. This pairs well with Jellyfin's built-in parental controls: tag a library, then set that tag under a user's **Allowed tags** or **Blocked tags** and their access follows the library without any manual upkeep.

Tags are only ever added. Anything already on an item stays put.

## Install

Add this repository to Jellyfin:

**Dashboard → Plugins → Repositories → +**

```
https://pusta.github.io/jellyfin-plugins/manifest.json
```

Then go to **Catalog**, find **Auto Tagger**, and install it. Restart Jellyfin when prompted.

## Setup

**Dashboard → Plugins → Auto Tagger**

Every library on your server is listed with a text box. Type the tags you want applied to that library, separated by commas. Leave a library blank and it's ignored.

```
Kids Movies     →  kids-ok, preschool
Family TV       →  kids-ok
Movies          →
```

Two options at the bottom:

**Also tag seasons and episodes.** Off by default, so only movies and series get tagged. Turn this on if you find that blocking a series by tag doesn't hide its individual episodes. It works, but it writes a tag to every episode, which takes a while on a large library.

**Lock the Tags field after tagging.** On by default. This stops a metadata refresh from wiping the tags back off. The tradeoff is that you can't edit tags by hand in the Jellyfin UI until you unlock the field on that item.

Save, and new media gets tagged from then on.

## Tagging what's already there

The plugin only sees media as it's added, so anything already in your libraries won't have tags yet. To catch those up:

**Dashboard → Scheduled Tasks → Apply auto-tags to existing items → Run**

Progress shows in the dashboard. It's a one-time thing after you set up your rules, though it's safe to run again whenever.

## Notes

Tagging isn't instant. New items go into a queue behind whatever else the library scan is doing, so give it a minute before assuming something's wrong. The Jellyfin log shows a line for each item tagged if you want to watch it happen.

Requires Jellyfin 10.11.x. Older versions need a build against their own SDK.

## Building from source

```bash
dotnet publish --configuration=Release Jellyfin.Plugin.AutoTagger.sln
```

Drop `Jellyfin.Plugin.AutoTagger.dll` into a subfolder of your plugins directory and restart:

| Platform | Path |
| --- | --- |
| Docker | `/config/plugins/AutoTagger/` |
| Linux | `/var/lib/jellyfin/plugins/AutoTagger/` |
| Windows | `%ProgramData%\Jellyfin\Server\plugins\AutoTagger\` |

The `Jellyfin.Controller` and `Jellyfin.Model` versions in the csproj have to match your server, or the plugin shows up as `NotSupported` and won't load.

## License

GPL-3.0