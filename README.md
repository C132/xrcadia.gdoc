# Google Doc Markdown

An editor-only Unity package that pulls public Google Docs as Markdown and writes them to files inside your project for AI agent reference.

## What it does
- Manages multiple Google Doc sources from a single centralized settings asset.
- Downloads Markdown from Google Docs and writes it to a configured output path.
- Automatically extracts and saves images from the doc to a local project folder.
- Optionally auto-pulls on editor startup with a configurable minimum interval.

## Quick Start
1. Open the settings window: `xrcadia/Google Doc Markdown/Settings`.
2. Click **+ Add New Source**.
3. Enter the **Google Doc URL** or raw document ID.
4. Set the **Output Path** (e.g., `Assets/Documentation/MyDoc.md`).
5. Click **Pull** on the source item, or **Pull All Sources Now** at the bottom.

## Configuration
- **Auto Pull On Editor Startup**: If enabled, the tool checks all sources whenever the Unity project is opened.
- **Interval (minutes)**: Minimum time to wait between automatic pulls to avoid redundant network traffic.
- **Output Path**: Relative to the Unity project root. Example: `Assets/Documentation/Design.md`.

## Features
- **Image Handling**: Base64-encoded images in Google Docs are automatically saved as PNG/JPG files in a `[DocName]_images` folder next to your Markdown file.
- **Automatic Renaming**: If you use the default `Design.md` path, the tool will automatically rename the file to match the Google Doc's title on the first successful pull.

## Notes
- **Public Access**: Google Docs must be shared as "Anyone with the link can view" for the export tool to work without OAuth.
- **Line Endings**: Line endings are normalized to `\n`, and trailing whitespace is trimmed for cleaner diffs.
