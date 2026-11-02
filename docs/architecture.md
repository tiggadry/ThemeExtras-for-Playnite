# Architecture

## Extension entry point

The extension is declared in `source/extension.yaml`:

- Name: `ThemeExtras`
- Type: `GenericPlugin`
- Module: `Extras.dll`
- Original extension ID: `felixkmh_Extras_Plugin`

The current fork's runtime plugin ID is:

```text
d2039edd-78f5-47c5-b190-72afef560fbe
```

The upstream 1.4.4 installer metadata targets Playnite API 6.4.0 and
Playnite 10 or newer. The project targets .NET Framework 4.6.2.

`ThemeExtras` initializes settings, discovers extended theme manifests,
registers custom elements and converters, restores persistent theme files
when enabled, and creates the banner cache.

## Main components

| Component | Responsibility |
|---|---|
| `ThemeExtras` | Playnite plugin entry point and lifecycle |
| `ExtrasSettings` | Settings, commands, and optional menu integrations |
| `ExtendedTheme` | Theme manifest loading, persistence, and backups |
| `ThemeExtrasManifest` | Model for `themeExtras.yaml` |
| `BannerCache` | Runtime banner lookup and invalidation |
| `DirectoryBannerProvider` | User-provided banner overrides |
| `MergedDictionary` | Combines values from multiple banner providers |
| `Controls/` | XAML controls exposed to themes |
| `Converters/` | XAML value converters exposed by the plugin |

The public theme surface includes rating controls, editable game-property
controls, completion-status controls, banners, links, and media controls.
The plugin also exposes URL-icon, power, inverse, multiply, and divide
converter types for theme bindings.

## Theme manifest

`ExtendedTheme` looks for `themeExtras.yaml` alongside a theme's regular
manifest. The model currently supports:

- `ThemeId`
- `Recommendations`
- `PersistentPaths`
- `PersistentResources`
- `DefaultBannerPath`
- `BannersBySpecIdPath`
- `BannersByPluginIdPath`
- `BannersByPlatformNamePath`
- `BannersBySourceNamePath`
- `BannerDecodeHeight`
- `WebsiteIconsPath`

Theme-relative paths are resolved against the theme root. Missing optional
manifest values are allowed; features depending on them are simply not
configured.

Banner file names are resolved from the relevant platform specification ID,
platform name, plugin ID, or source name. Website icon files are resolved
from the target domain or subdomain. PNG and JPG are the normal supported
image formats for banner providers.

## Banner providers and precedence

The cache combines the current theme's provider with the user override
provider. User override paths are:

```text
%AppData%\Playnite\ExtensionsData\d2039edd-78f5-47c5-b190-72afef560fbe\Banners\
```

The provider categories are:

- `ByPlatformSpecId`
- `ByPlatformName`
- `ByPluginId`
- `BySourceName`
- `Default.png`

The cache key includes the game's platform, plugin, and source identity.
When a value is available from multiple providers, the merged provider
order gives the user override precedence for normal banner categories.
The default banner follows the provider behavior implemented in
`BannerCache`; do not assume it has the same precedence without checking
that code.

Banner controls observe game context changes and invalidate or refresh
their lookup when platform, plugin, source, or game identity changes.

## Theme persistence and backups

When backup and restore is enabled in ThemeExtras settings, `ExtendedTheme`
uses `PersistentPaths` to preserve theme files across theme updates.
Backups are stored in the plugin's Playnite extension data. The plugin
restores files when a theme is loaded and backs up relevant files during
shutdown or theme transitions.

`PersistentResources` is represented in the manifest model, but parts of
resource persistence remain intentionally limited in the current
implementation. Verify `ExtendedTheme.cs` before relying on resource
backup behavior.

Themes should load images under `PersistentPaths` with WPF
`BitmapCacheOption.OnLoad`. Keeping a file stream open can prevent the
backup/restore process from replacing that image during a theme update.

## Settings and commands

The settings model currently exposes:

- game-menu rating toggle,
- selection preservation while switching views,
- theme backup and restore,
- applying theme icons to Playnite shortcuts,
- commands and converter resources for XAML themes.

The settings view also exposes actions for opening user link-icon and banner
override directories and for inspecting theme backup folders.

## Optional integrations

`ExtrasSettings` discovers optional plugins through Playnite's plugin
collection. It builds menu entries from the plugin's own
`GetGameMenuItems` result instead of introducing compile-time references.
This keeps integrations optional and allows the third-party extension to
own its commands and settings. The shared `CreateGameMenuItems` helper accepts
an optional filter so an integration can expose only part of a plugin's menu
(used for the Metadata Utilities Image Utilities submenu).

The current integration map is maintained in
[integrations.md](integrations.md).

## Screenshot handling

ThemeExtras delegates screenshot-folder opening to the configured
ScreenshotsVisualizer menu action. The obsolete duplicate screenshot path
resolver was removed after it became unused. ThemeExtras does not own the
current list of screenshot sources; ScreenshotsVisualizer does.

## ImageRotater folder integration

ThemeExtras provides direct folder commands for ImageRotater's per-game
artwork. The integration uses ImageRotater's plugin ID and resolves folders
under the Playnite configuration path:

```text
%AppData%\Playnite\ExtensionsData\
  72b7d457-0621-429b-8368-665bc53ff896\Images\<GameId>\
    backgrounds\
    covers\
    logos\
```

The Details View context menu exposes these commands under **Open ImageRotater
Folders**. The parent command is enabled when at least one supported artwork
file exists in `backgrounds`, `covers`, or `logos`; each child command checks
its own directory independently. Supported files are `.jpg`, `.jpeg`, `.png`,
`.bmp`, `.webp`, `.gif`, `.mp4`, and `.webm`. The check only examines files
directly in the artwork directory, so ImageRotater metadata and cache
subdirectories do not make an empty artwork folder appear available.

## Build constraints

The project uses the existing non-SDK solution in `source/Extras.sln`.
Playnite assemblies are external references. A successful build therefore
requires a local Playnite installation or matching development binaries.
Do not use SDK-style migration as a shortcut.
