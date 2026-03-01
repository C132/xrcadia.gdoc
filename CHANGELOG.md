# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.6.0] - 2026-02-28

### Added
- **Strikethrough rendering.** Inline `~~text~~` syntax now renders with strikethrough styling using Unity rich text `<s>` tags.
- **Link rendering.** Inline links `[text](url)` and reference-style links `[text][ref]` now display as colored underlined text instead of leaking raw markdown syntax. Link color is theme-aware via a new `LinkColor` property on `MarkdownTheme`.
- **Escaped character support.** Backslash-escaped special characters (`\*`, `\_`, `\[`, etc.) now render as literal characters instead of triggering markdown patterns.
- **Tilde-fenced code blocks.** Code blocks fenced with `~~~` are now rendered identically to backtick-fenced blocks.
- **Indented code blocks.** Lines indented with 4+ spaces (preceded by a blank line) now render as code blocks.
- **Code block language labels.** The language identifier after opening fences (e.g., ` ```csharp `) is displayed as a muted label above the code content.
- **Task list support.** List items with `- [ ]` and `- [x]` syntax render with checkbox characters and appropriate styling.
- **Nested list indentation.** List items at different indentation levels now render with proportional left margins based on leading whitespace depth.
- **Multi-line blockquote grouping.** Consecutive `>` lines are accumulated into a single blockquote container with recursive content rendering, supporting headings, lists, code blocks, and tables inside blockquotes.
- **Nested blockquotes.** Lines with multiple `>` prefixes (e.g., `> > nested`) render as visually nested blockquote containers.
- **GitHub-style alerts and admonitions.** Blockquotes starting with `[!NOTE]`, `[!TIP]`, `[!IMPORTANT]`, `[!WARNING]`, or `[!CAUTION]` render as styled alert boxes with colored left borders and bold type labels.
- **Table column alignment.** Separator rows with `:` markers (`:---`, `:---:`, `---:`) now apply left, center, or right text alignment to table cells.
- **Inline HTML support.** Common HTML tags (`<strong>`, `<em>`, `<b>`, `<i>`, `<s>`, `<del>`, `<u>`, `<kbd>`, `<mark>`, `<sup>`, `<sub>`) are mapped to Unity rich text equivalents instead of being displayed as raw tags.
- **Bare URL auto-linking.** URLs starting with `http://` or `https://` are automatically rendered as colored underlined links without requiring markdown link syntax. URL-internal underscores are protected from italic regex mangling.
- **Bare email auto-linking.** Email addresses like `user@example.com` are automatically rendered with link styling.
- **Block-level HTML tag stripping.** Block HTML tags (`<p>`, `<div>`, `<details>`, `<summary>`, `<br>`, etc.) are silently removed instead of being displayed as raw text.
- **Distinct H4/H5/H6 heading sizes.** Heading levels 4, 5, and 6 now render at 16px, 14px, and 13px respectively instead of sharing a single fallback size. H6 also uses muted text color for visual hierarchy.
- **Clickable links.** Clicking rendered links opens external URLs in the system browser and relative `.md` paths within the viewer. Labels with links display a hand cursor and URL tooltip. Paragraphs with multiple links show a dropdown menu on click.

### Changed
- **Blockquote rendering refactored to recursive model.** Blockquotes now use `RenderMarkdownLines` recursively, enabling full block-level content (headings, lists, code blocks, tables) inside blockquotes instead of plain text only.
- **Main parse loop extracted to `RenderMarkdownLines`.** The line-by-line rendering logic is now a reusable method that accepts any `VisualElement` parent, enabling recursive rendering for nested structures like blockquotes and alerts.

## [1.5.0] - 2026-02-27

### Added
- **Toggleable sidebar with document outline and file browser.** A toggle button in the toolbar opens a resizable left sidebar. The Files section shows Pinned, Recent, and All Files sub-sections. All Files renders a proper nested folder tree (Assets, Packages, etc.) matching Unity's one-column Project layout. Right-click any file to pin or unpin it. The Outline section lists all headings from the current document as a clickable tree that scrolls to the heading. Sidebar width, open state, pinned files, and recently viewed files (up to 15) persist via EditorPrefs.
- **Resizable sidebar with drag handle.** A drag handle between the sidebar and content pane allows resizing from 140px to 500px. The handle shows an OS-level horizontal resize cursor, highlights subtly on hover, and persists the chosen width across sessions.
- **Subtle hover highlighting across the sidebar.** File rows, folder tree entries, and outline headings show a rounded background highlight on hover for clear interactive feedback.
- **Settings gear with dot-path toggle.** A gear icon (⚙) in the toolbar opens a settings menu. The first option toggles "Expand package dots to folders," which controls whether `com.unity.burst` is expanded into a `unity/burst` folder hierarchy or kept as a single entry. The preference persists via EditorPrefs.
- **Comprehensive TEST.md test document.** A `TEST.md` file at the package root exercises every standard Markdown feature: headings, inline formatting, lists, blockquotes, code blocks, tables, images, links, HTML entities, nested structures, edge cases, and extended syntax (footnotes, task lists, math, admonitions). Serves as a visual regression test for the viewer.

### Changed
- **Switching files no longer rebuilds the sidebar.** Opening a file from the browser or outline only re-renders the content pane and outline. The file browser tree preserves its foldout state and scroll position, so navigating between files feels seamless.
- **Performance optimization pass.** All 13 regex patterns are now compiled once as `static readonly` fields instead of being allocated per render. The markdown file list and folder tree are cached and only rebuilt on explicit refresh. Code block accumulation uses `StringBuilder` instead of string concatenation. Character-level guards skip regex matching for lines that cannot match. Folder icons are cached as static `Texture2D` references.

### Fixed
- **Outline strips markdown formatting.** Heading text in the sidebar outline now strips bold (`**`), italic (`*`), inline code, strikethrough, link syntax, and backslash escapes so entries display as clean plain text instead of showing raw markdown markers.
- **Folder icon state no longer corrupted by child foldouts.** Collapsing a subfolder in the file browser no longer causes parent folder icons to show the closed state. The icon now reads the foldout's own `.value` property instead of the bubbled `ChangeEvent` value.

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
