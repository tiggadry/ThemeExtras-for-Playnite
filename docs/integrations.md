# Optional integrations

ThemeExtras discovers these integrations at runtime. They are optional:
ThemeExtras should still load when one or more of them are not installed.

## Integration overview

| Extension | Plugin ID | ThemeExtras behavior |
|---|---|---|
| Extra Metadata Loader | `705fdbca-e1fc-4004-b839-1d040b8b4429` | Exposes the extension's selected-game menu items and a settings entry |
| BackgroundChanger | `3afdd02b-db6c-4b60-8faa-2971d6dfad2a` | Exposes game menu items and opens the game's BackgroundChanger folder when available |
| Metadata Utilities | `485ab5f0-bfb1-4c17-93cc-20d8338673be` | Exposes only the Image Utilities submenu (flip/rotate background, cover, icon, logo) and a settings entry |
| ScreenshotsVisualizer | `c6c8276f-91bf-48e5-a1d1-4bee0b493488` | Exposes menu items and delegates configured screenshot-folder opening to the extension |
| ScreenshotUtilities | `485d682f-73e9-4d54-b16f-b8dd49e88f90` | Exposes the extension's selected-game commands and settings |
| UniPlaySong | `a1b2c3d4-e5f6-7890-abcd-ef1234567890` | Exposes the extension's selected-game menu items and a settings entry |
| SaveFileView | `f68f302b-9799-4b77-a982-4bfca97130e2` | Resolves configured save and configuration folders for the selected game |

## Menu delegation

For integrations that expose Playnite game-menu items, ThemeExtras asks the
optional plugin for its own menu items and reconstructs their menu hierarchy
inside the ThemeExtras menu. The third-party action is invoked when the user
selects the resulting entry.

This approach means:

- ThemeExtras does not compile against optional extension assemblies.
- The optional extension remains responsible for its action behavior.
- A missing or incompatible extension should not prevent ThemeExtras from
  loading.
- Changes to a third-party extension's menu descriptions or IDs can require
  a ThemeExtras compatibility fix.

Menu delegation can optionally filter the third-party items before the menu
is built. Metadata Utilities uses this: its menu also contains metadata
editing, merging and quick-add actions, so ThemeExtras keeps only items whose
`MenuSection` equals the localized `LOCMetadataUtilitiesMenuImageUtilities`
string.

## Metadata Utilities

ThemeExtras exposes Metadata Utilities' Image Utilities context menu
(`Menus.ImageUtilitiesGameMenuItems`), added in Metadata Utilities release
2026-09-29, with a settings entry first. Themes bind it with
`{PluginSettings Plugin=ThemeExtras, Path=Menus.ImageUtilitiesGameMenuItems}`.
Items for a given image type (background, cover, icon, logo) are offered by
Metadata Utilities only when the selected game has that image. The rest of
the Metadata Utilities menu is not exposed.

## ScreenshotsVisualizer

ThemeExtras uses the ScreenshotsVisualizer plugin ID and locates its
open-screenshots-directory menu item by the localized resource description
`LOCSsvOpenScreenshotsDirectory`. It invokes that action for the selected
game rather than calculating screenshot paths itself.

Configure screenshot sources in ScreenshotsVisualizer. ThemeExtras does not
maintain a second screenshot-source configuration.

If the action is unavailable, ThemeExtras does not expose a working
folder command and reports that no screenshot folder was found.

## SaveFileView

ThemeExtras reads SaveFileView's per-game data from its extension-data
directory and resolves placeholders such as user profile, AppData, Steam,
Ubisoft, and wildcard user identifiers. The integration only exposes
folders that can be resolved from the available data.

This is a compatibility bridge, not a replacement for SaveFileView's
online directory data. If SaveFileView has no saved data for a game,
ThemeExtras cannot invent the missing paths.

## ScreenshotUtilities

ThemeExtras adds ScreenshotUtilities' selected-game commands and a settings
entry to the game menu. It does not manage ScreenshotUtilities' screenshot
storage or duplicate its configuration.

## Folder actions

ThemeExtras also contains direct folder commands for selected games and
optional extensions. These commands validate that a path exists before
opening it and show a Playnite message or log an error when the path cannot
be resolved.
