# xrcadia.gdoc

An editor-only Unity package that automatically syncs public Google Docs into your project as clean Markdown files. Extracts embedded images, renders with 11 professional themes, and keeps everything current with auto-pull on editor startup. Zero dependencies. Zero runtime overhead.

**[View the product page &rarr;](https://c132.github.io/xrcadia.gdoc/)**

## Features

- **Auto-Sync Engine.** Pulls on editor startup with a configurable minimum interval. Batch all sources or sync individually. Tracks timestamps and errors per document.
- **Image Extraction.** Detects base64-encoded images in Google Doc exports and saves them as files in organized folders alongside each document.
- **Built-in Markdown Viewer.** Full GitHub Flavored Markdown rendering inside the Unity editor. Tables with column alignment, fenced and indented code blocks, task lists, nested blockquotes, strikethrough, GitHub-style alerts, inline HTML tags, bare URL auto-linking, and language labels on code fences.
- **11 Professional Themes.** 8 dark and 3 light themes built on an Open-Closed architecture. Add your own without touching the renderer.
- **Sidebar Navigation.** Toggleable sidebar with document outline, project-wide file browser, pinned favorites, recent files, and a resizable handle.
- **Clickable Links.** External URLs open in your browser. Relative `.md` paths open in the viewer. Multi-link paragraphs show a dropdown menu.
- **Inspector Integration.** Select any `.md` file in the Project window and a "View as Markdown" button appears in the Inspector.
- **Automatic Renaming.** Files using the default name are automatically renamed to match the Google Doc's title on first pull.

## Quick Start

1. Open the settings window: **xrcadia &rarr; Google Doc Markdown &rarr; Settings**.
2. Click **+ Add New Source**.
3. Enter the Google Doc URL or raw document ID.
4. Set the output path (e.g., `Assets/Documentation`).
5. Click **Pull** on the source, or **Pull All Sources Now** at the bottom.

Google Docs must be shared as *"Anyone with the link can view"* or published to the web for the export to work without OAuth.

## Configuration

| Setting | Description |
|---|---|
| **Auto Pull On Editor Startup** | Re-downloads all sources when the Unity project is opened, subject to the minimum interval. |
| **Minimum Minutes Between Auto-Pulls** | Throttle to avoid redundant network requests. Default: 60 minutes. |
| **Output Path** | Directory relative to the project root where Markdown files are written. Default: `Assets/Documentation`. |

Each source tracks its own last-pull timestamp (UTC ISO 8601) and last error message for debugging failed syncs.

## Viewer

The built-in Markdown Viewer renders any `.md` file in the project with professional styling. Open it from the Inspector button on any TextAsset, from the settings panel, or from the sidebar file browser.

### Supported Markdown

- Headings (H1–H6 with distinct sizes)
- Bold, italic, strikethrough, inline code
- Fenced code blocks (backtick and tilde) with language labels
- Indented code blocks
- Tables with column alignment (`:---`, `:---:`, `---:`)
- Task lists (`- [ ]` and `- [x]`)
- Blockquotes (multi-line grouping, nested)
- GitHub-style alerts (`[!NOTE]`, `[!TIP]`, `[!IMPORTANT]`, `[!WARNING]`, `[!CAUTION]`)
- Inline HTML tags (`<strong>`, `<em>`, `<kbd>`, `<mark>`, `<sup>`, `<sub>`, etc.)
- Inline and reference-style links (clickable)
- Bare URL and email auto-linking
- Escaped characters
- Horizontal rules
- Images (rendered inline)

### Sidebar

The toggleable sidebar provides:

- **Outline.** Clickable heading tree that scrolls to each section.
- **Pinned Files.** Favorite files shown at the top for quick access.
- **Recent Files.** Up to 15 recently viewed Markdown files.
- **All Files.** Full project file tree (Assets, Packages) with optional dot-path expansion for package names.

The sidebar is resizable between 140–500px. State persists across sessions.

## Themes

11 built-in themes, each implementing the `MarkdownTheme` base class via sealed subclasses. The system follows the Open-Closed Principle — add a theme by subclassing and registering, with zero changes to rendering code.

| Theme | Type |
|---|---|
| Dark | Dark |
| Solarized Dark | Dark |
| Nord | Dark |
| Dracula | Dark |
| Monokai | Dark |
| One Dark | Dark |
| Gruvbox Dark | Dark |
| GitHub Dark | Dark |
| Paperwhite | Light |
| Solarized Light | Light |
| GitHub | Light |

Theme selection persists via EditorPrefs.

## Installation

### Unity Package Manager (Git URL)

Add the following to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.xrcadia.gdoc": "https://github.com/C132/xrcadia.gdoc.git"
  }
}
```

Or use the Package Manager window: **+** &rarr; **Add package from git URL** &rarr; paste:

```
https://github.com/C132/xrcadia.gdoc.git
```

### Local Copy

Clone or copy the repository into your project's `Packages/` directory:

```
Packages/
  com.xrcadia.gdoc/
    package.json
    ...
```

## Requirements

- **Unity 6000.3** or later.
- **Editor-only.** The entire package uses an editor-only assembly definition (`includePlatforms: ["Editor"]`). Zero code ships in player builds.
- **No dependencies.** Uses only Unity's built-in UIElements and UnityWebRequest APIs.

## License

[MIT](LICENSE) &copy; 2026 xrcadia
