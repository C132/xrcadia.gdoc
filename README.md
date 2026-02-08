# Google Doc Markdown

An editor-only Unity package that pulls a public Google Doc as Markdown and writes it to a file inside your project for AI agent reference.

## What it does
- Stores a Google Doc URL or ID in a ScriptableObject asset.
- Downloads Markdown from Google Docs and writes it to a configured output path.
- Optionally auto-pulls on editor startup with a minimum interval.

## Create a source asset
1. In the Project window, use `Assets/Create/Google Doc Markdown/Source`.
2. Enter the Google Doc URL or raw document ID.
3. Set an output path relative to the Unity project root.

## Configure output path
- `outputPath` is relative to the Unity project root (the folder containing `Assets/`).
- Example: `Docs/Design.md` writes to `<Project>/Docs/Design.md`.
- If you want the file inside the Assets folder, use `Assets/Docs/Design.md`.

## Pulling
- Click `Pull Now` in the asset inspector.
- Or use the menu item: `Tools/Google Doc Markdown/Pull All Sources`.

## Notes
- Google Docs must be public to export Markdown unless OAuth is added.
- Line endings are normalized to `\n`, and trailing whitespace is trimmed.
