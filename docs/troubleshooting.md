# Troubleshooting

## A ThemeExtras menu item is missing

Check the following:

1. A game is selected in Playnite.
2. The related optional extension is installed and enabled.
3. The extension exposes a game-menu item for the selected game.
4. The selected game has the data required by the integration.
5. Playnite's log does not contain a ThemeExtras integration error.

ThemeExtras intentionally does not expose entries for unavailable optional
extensions.

## Screenshots folder cannot be opened

1. Open ScreenshotsVisualizer settings.
2. Confirm that the game's screenshot source or folder is configured.
3. Confirm that the configured folder exists.
4. Confirm that the ScreenshotsVisualizer game-menu action is available.
5. Retry from the selected game's ThemeExtras menu.

ThemeExtras delegates this action to ScreenshotsVisualizer. Do not maintain
a second path list in ThemeExtras.

## Save or configuration folder cannot be opened

1. Confirm that SaveFileView is installed and enabled.
2. Confirm that SaveFileView has stored data for the game.
3. Check that the game's SaveFileView JSON exists under Playnite's
   `ExtensionsData` directory.
4. Check that the resolved directory exists on disk.
5. Review the Playnite log for placeholder or path-resolution errors.

ThemeExtras cannot open a folder when SaveFileView has no data for the game
or when the resolved path does not exist.

## A banner is missing

Check the lookup inputs and paths:

1. Confirm that the theme contains `themeExtras.yaml`.
2. Confirm that `ThemeId` matches the installed theme.
3. Confirm that the configured banner path is relative to the theme root.
4. Check the platform specification, platform name, plugin ID, or source
   name used by the game.
5. Check the user override directory under:
   `%AppData%\Playnite\ExtensionsData\d2039edd-78f5-47c5-b190-72afef560fbe\Banners`.
6. Confirm that the image file name and extension match the expected key.
7. Restart or reload the relevant Playnite view if a previously cached
   banner is still visible.

## Theme files are not preserved after an update

1. Open ThemeExtras settings.
2. Confirm that **Backup and Restore** is enabled.
3. Confirm that the theme declares the required relative paths in
   `themeExtras.yaml` under `PersistentPaths`.
4. Check that the theme is not being treated as a development theme.
5. Review the Playnite log for backup or restore errors.

Only paths declared by the theme are eligible for path persistence.

## Where to find logs

Use Playnite's normal log location and search for `ThemeExtras`,
`ExtendedTheme`, or the related optional extension name. When reporting a
problem, include:

- Playnite version,
- ThemeExtras version or commit,
- active theme,
- installed optional extensions,
- selected game,
- relevant configuration,
- the complete error message and stack trace.

## Development troubleshooting

For source changes:

- build `source\Extras.sln`, not an SDK-style project,
- ensure Playnite assemblies are available,
- avoid changing optional integrations into hard references,
- preserve the existing plugin IDs and Playnite menu contracts.
