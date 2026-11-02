# Changelog

This file summarizes user-visible and maintenance changes in the fork.
For the complete technical history, use `git log`.

## Unreleased

### Added

- Exposed the Metadata Utilities Image Utilities menu (flip and rotate
  background, cover, icon, and logo) with a settings entry in the selected
  game's ThemeExtras menu, as `Menus.ImageUtilitiesGameMenuItems`.
- Exposed ScreenshotUtilities commands and settings in the selected
  game's ThemeExtras menu.
- Added Details View context-menu actions for selected Playnite and
  optional extension folders and settings.

### Fixed

- Restored opening of configured ScreenshotsVisualizer folders after the
  extension changed its menu integration.
- Restored SaveFileView save and configuration folder access for newly
  added games by resolving placeholders and fallback paths.
- Prevented backup errors during Playnite shutdown when deleted-file
  markers are created.

### Refactored

- Added an optional filter to the shared game-menu item builder so an
  integration can expose only part of a plugin's menu.

- Removed the obsolete screenshot-folder resolver from ThemeExtras.
  ScreenshotsVisualizer now owns screenshot-folder configuration and
  exposes the action used by ThemeExtras.

### Maintenance

- Added local development and formatting-tool configuration.
- Improved project metadata and local development ignore rules.
- Applied automated C# formatting while evaluating code-quality tools.

## Historical notes

- The fork was created in July 2025 as a learning and experimentation
  space for AI-assisted development while the original addon author had
  been inactive for an extended period.
- The fork retains the original extension identity and remains compatible
  with themes using the ThemeExtras manifest format.
