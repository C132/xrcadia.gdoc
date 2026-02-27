# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2026-02-27

### Added
- **View as Markdown button for TextAsset Inspector.** Selecting any `.md` file in the Project window now shows a "View as Markdown" button in the Inspector that opens the Markdown Viewer with rich formatting, themes, and image support.

## [1.2.1] - 2026-02-19

### Changed
- **package.json field order corrected.** Moved `author` before `keywords` to match the package manifest specification.

## [1.2.0] - 2026-02-16

### Changed
- **Editor menu paths moved from `Tools/` to `xrcadia/`.** Settings and Pull All Sources are now under `xrcadia/Google Doc Markdown/` in the Unity menu bar to distinguish xrcadia tools from third-party and built-in Unity menus.

## [1.1.0] - 2026-02-16

### Changed
- **Book-like layout for the Markdown Viewer.** Content is now rendered in a centered column (max 680px) with generous padding, larger font sizes, and improved heading hierarchy with underlines on H1/H2. Horizontal rules are centered at 60% width for a more editorial feel.

### Added
- **Paperwhite theme toggle.** A toolbar button switches between the default dark theme and a warm cream e-ink inspired theme with sepia tones and low-contrast colors. The preference is persisted via EditorPrefs across sessions.

## [1.0.0] - 2026-02-08
- Initial release.
