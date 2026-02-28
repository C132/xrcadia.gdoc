# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.5.0] - 2026-02-27

### Added
- **Toggleable sidebar with document outline and file browser.** A toggle button in the toolbar opens a resizable left sidebar. The Files section shows Pinned, Recent, and All Files sub-sections. All Files renders a proper nested folder tree (Assets, Packages, etc.) matching Unity's one-column Project layout. Right-click any file to pin or unpin it. The Outline section lists all headings from the current document as a clickable tree that scrolls to the heading. Sidebar width, open state, pinned files, and recently viewed files (up to 15) persist via EditorPrefs.
- **Resizable sidebar with drag handle.** A drag handle between the sidebar and content pane allows resizing from 140px to 500px. The handle shows an OS-level horizontal resize cursor, highlights subtly on hover, and persists the chosen width across sessions.
- **Subtle hover highlighting across the sidebar.** File rows, folder tree entries, and outline headings show a rounded background highlight on hover for clear interactive feedback.

### Changed
- **Switching files no longer rebuilds the sidebar.** Opening a file from the browser or outline only re-renders the content pane and outline. The file browser tree preserves its foldout state and scroll position, so navigating between files feels seamless.

## [1.4.0] - 2026-02-27

### Added
- **Theme system with 11 built-in themes.** The Markdown Viewer now offers a grouped dropdown in the toolbar organized by Dark and Light sections. Includes Dark, Solarized Dark, Nord, Dracula, Monokai, One Dark, Gruvbox Dark, GitHub Dark, Paperwhite, Solarized Light, and GitHub themes. The selected theme persists across sessions via EditorPrefs.

### Changed
- **Theme architecture extracted to follow OCP.** Color definitions moved from inline ternary properties in the viewer to an abstract `MarkdownTheme` base class (`Editor/MarkdownTheme.cs`) with sealed subclasses for each theme. Adding a new theme requires only a new subclass and one registry entry. The old paperwhite toggle button has been replaced by the theme dropdown, with automatic migration from the legacy preference.

### Fixed
- **Code block font lookup no longer uses IMGUI.** Replaced `GUI.skin.FindStyle()` with a cached `Font.CreateDynamicFontFromOSFont()` call to avoid `ArgumentException` when rebuilding the UI outside of an `OnGUI` context (e.g., when switching themes via the dropdown).

## [1.3.1] - 2026-02-27

### Fixed
- **HTML entities displayed literally in the Markdown Viewer.** Angle brackets and other HTML entities (e.g., `&lt;`, `&gt;`, `&amp;`) from Google Docs exports were shown as raw entity text instead of being decoded. The viewer now decodes HTML entities before rendering and uses `<noparse>` tags to safely display angle brackets without interfering with Unity rich text parsing.

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
