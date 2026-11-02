# ThemeExtras

ThemeExtras is a Playnite Desktop extension that adds theme-oriented
controls, banner providers, theme persistence, converters, and optional
integrations with other Playnite extensions.

This repository is a maintained fork of
[felixkmh/ThemeExtras-for-Playnite](https://github.com/felixkmh/ThemeExtras-for-Playnite).
The fork preserves the original extension identity so existing themes can
continue to use the ThemeExtras manifest format.

## Features

- Theme-provided banners with user override folders.
- Banner lookup by platform specification, platform name, plugin ID, and
  game source.
- Theme backup and restore for paths declared by a theme.
- Custom theme controls such as banners, ratings, links, completion
  status, editable tags, and media controls.
- Additional value converters and commands exposed to XAML themes,
  including URL icon, power, inverse, multiply, and divide converters.
- Optional game-menu integrations for selected Playnite extensions,
  including the Metadata Utilities Image Utilities menu (flip and rotate
  backgrounds, covers, icons, and logos).
- ImageRotater folder actions for opening the selected game's covers,
  backgrounds, and logos directories.
- Theme and plugin folder actions from the Playnite game menu.

## Requirements and compatibility

- Playnite Desktop 10 or newer.
- Playnite API 6.4.0 or newer for the upstream 1.4.4 compatibility line.
- .NET Framework 4.6.2.
- A Playnite installation capable of loading the ThemeExtras extension.
- A theme that uses the ThemeExtras manifest format for theme-specific
  banners or persistent files.

The project is a legacy .NET Framework project and is not an SDK-style
project. Build instructions and external Playnite references are described
in [the architecture documentation](docs/architecture.md).

## Installation

Install the built extension through the normal Playnite extension
installation workflow. The repository itself is primarily a development
repository; it does not currently publish a separate package from this
README.

Optional integrations are detected at runtime. ThemeExtras does not require
those extensions to be installed.

## Theme configuration

A theme can provide `themeExtras.yaml` next to its regular theme manifest.
The file may contain:

```yaml
ThemeId: your-theme-id
PersistentPaths:
  - Images/Banners
BannersBySpecIdPath: Images/Banners/PlatformSpecId
BannersByPlatformNamePath: Images/Banners/PlatformName
BannersByPluginIdPath: Images/Banners/PluginId
BannersBySourceNamePath: Images/Banners/SourceName
DefaultBannerPath: Images/Banners/UnknownLibrary.png
BannerDecodeHeight: 75
WebsiteIconsPath: Images/LinkIcons
```

All paths are relative to the theme directory. See
[architecture.md](docs/architecture.md) for the complete manifest model and
backup behavior.

Platform and plugin banner files use the relevant platform specification
ID, platform name, plugin ID, or source name as the file name. PNG and JPG
banner files are supported. Website icons use the relevant domain or
subdomain name.

## User banner overrides

ThemeExtras creates user override folders under Playnite's plugin data
directory:

```text
%AppData%\Playnite\ExtensionsData\d2039edd-78f5-47c5-b190-72afef560fbe\Banners\
├── Default.png
├── ByPlatformSpecId\
├── ByPlatformName\
├── ByPluginId\
└── BySourceName\
```

User overrides are loaded in addition to theme-provided banners. The
lookup order is documented in [architecture.md](docs/architecture.md).

Images stored under `PersistentPaths` should be loaded by a theme with WPF
`CacheOption=OnLoad`; otherwise an open image stream can prevent ThemeExtras
from replacing the file during theme restoration.

## Optional integrations

ThemeExtras can expose commands from optional extensions in the selected
game menu. Current integrations and their behavior are listed in
[integrations.md](docs/integrations.md).

The ImageRotater integration adds an **Open ImageRotater Folders** submenu
to the selected game's Details View context menu. It contains separate
actions for `backgrounds`, `covers`, and `logos` under ImageRotater's
per-game image directory. The parent item is disabled when none of those
directories contains supported artwork.

## Troubleshooting

If a menu entry or banner is missing, start with
[troubleshooting.md](docs/troubleshooting.md).

## Development

Open `source\Extras.sln` with a .NET Framework-compatible Visual Studio
installation. Playnite assemblies are external references and must be
available on the development machine.

The repository uses a legacy project format. Do not migrate it to
SDK-style or introduce newer .NET requirements without a deliberate
compatibility decision.

## License and upstream

See [LICENSE.txt](LICENSE.txt) for licensing information. The upstream
project and its original documentation are available at
[felixkmh/ThemeExtras-for-Playnite](https://github.com/felixkmh/ThemeExtras-for-Playnite).
