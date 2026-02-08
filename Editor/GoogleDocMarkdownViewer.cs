using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Xrcadia.GoogleDocMarkdown.Editor
{
    public class GoogleDocMarkdownViewer : EditorWindow
    {
        private string _filePath;
        private string _content;
        private Dictionary<string, string> _references = new Dictionary<string, string>();

        public static void ShowWindow(string relativePath)
        {
            var window = GetWindow<GoogleDocMarkdownViewer>("Markdown Viewer");
            window._filePath = relativePath;
            window.titleContent = new GUIContent("MD: " + Path.GetFileName(relativePath), EditorGUIUtility.IconContent("TextAsset Icon").image);
            window.minSize = new Vector2(400, 500);
            window.Refresh();
        }

        private void Refresh()
        {
            if (string.IsNullOrEmpty(_filePath)) return;
            
            var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", _filePath));
            if (!File.Exists(fullPath))
            {
                _content = $"# File Not Found\n\nThe file at path `{_filePath}` does not exist. Please pull the document first.";
            }
            else
            {
                _content = File.ReadAllText(fullPath);
            }
            
            if (rootVisualElement != null)
            {
                BuildUI();
            }
        }

        public void CreateGUI()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);

            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.paddingLeft = 10;
            toolbar.style.paddingRight = 10;
            toolbar.style.paddingTop = 5;
            toolbar.style.paddingBottom = 5;
            toolbar.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f);

            var pathLabel = new Label(_filePath);
            pathLabel.style.flexGrow = 1;
            pathLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            pathLabel.style.fontSize = 11;
            pathLabel.style.opacity = 0.7f;
            toolbar.Add(pathLabel);

            var reloadBtn = new Button(Refresh) { text = "Reload" };
            reloadBtn.style.height = 20;
            toolbar.Add(reloadBtn);

            rootVisualElement.Add(toolbar);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            scroll.style.paddingLeft = 20;
            scroll.style.paddingRight = 20;
            scroll.style.paddingTop = 20;
            scroll.style.paddingBottom = 40;
            rootVisualElement.Add(scroll);

            if (string.IsNullOrEmpty(_content)) return;

            ParseReferences();

            var lines = _content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            bool inCodeBlock = false;
            string codeBlockContent = "";
            List<string> tableLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Code blocks
                if (trimmedLine.StartsWith("```"))
                {
                    if (tableLines.Count > 0)
                    {
                        scroll.Add(CreateTable(tableLines));
                        tableLines.Clear();
                    }

                    if (inCodeBlock)
                    {
                        scroll.Add(CreateCodeBlock(codeBlockContent.TrimEnd()));
                        codeBlockContent = "";
                        inCodeBlock = false;
                    }
                    else
                    {
                        inCodeBlock = true;
                    }
                    continue;
                }

                if (inCodeBlock)
                {
                    codeBlockContent += line + "\n";
                    continue;
                }

                // Table detection (very basic)
                if (trimmedLine.StartsWith("|") && trimmedLine.EndsWith("|") && trimmedLine.Contains("|"))
                {
                    tableLines.Add(trimmedLine);
                    continue;
                }
                else if (tableLines.Count > 0)
                {
                    scroll.Add(CreateTable(tableLines));
                    tableLines.Clear();
                }

                // Headers
                if (trimmedLine.StartsWith("#"))
                {
                    scroll.Add(CreateHeader(trimmedLine));
                    continue;
                }

                // Horizontal Rule
                if (trimmedLine == "---" || trimmedLine == "***" || trimmedLine == "___")
                {
                    scroll.Add(CreateHorizontalRule());
                    continue;
                }

                // Images (possibly linked)
                if (TryMatchImage(trimmedLine, out var imgPath, out var altText))
                {
                    scroll.Add(CreateImage(imgPath, altText));
                    continue;
                }

                // Lists
                if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* ") || Regex.IsMatch(trimmedLine, @"^\d+\. "))
                {
                    scroll.Add(CreateListItem(line)); // Keep original indentation/line for lists
                    continue;
                }

                // Blockquotes
                if (trimmedLine.StartsWith("> "))
                {
                    scroll.Add(CreateBlockquote(line)); // Keep original for blockquote
                    continue;
                }

                // Reference definitions (skip)
                if (Regex.IsMatch(trimmedLine, @"^\[.*\]:"))
                {
                    continue;
                }

                // Paragraph or empty line
                if (!string.IsNullOrWhiteSpace(line))
                {
                    scroll.Add(CreateParagraph(line));
                }
                else
                {
                    var spacer = new VisualElement { style = { height = 10 } };
                    scroll.Add(spacer);
                }
            }

            if (tableLines.Count > 0)
            {
                scroll.Add(CreateTable(tableLines));
            }
        }

        private VisualElement CreateTable(List<string> rows)
        {
            var table = new VisualElement();
            table.style.marginTop = 10;
            table.style.marginBottom = 10;
            table.style.borderTopWidth = 1;
            table.style.borderBottomWidth = 1;
            table.style.borderLeftWidth = 1;
            table.style.borderRightWidth = 1;
            table.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            table.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);

            if (rows.Count == 0) return table;

            // Check for separator row at index 1
            bool hasHeader = rows.Count > 1 && Regex.IsMatch(rows[1], @"^\|[\s\-:|]+\|$");
            
            int startIdx = 0;
            if (hasHeader)
            {
                table.Add(CreateTableRow(rows[0], true));
                startIdx = 2; // Skip header and separator
            }

            for (int i = startIdx; i < rows.Count; i++)
            {
                table.Add(CreateTableRow(rows[i], false));
            }

            return table;
        }

        private VisualElement CreateTableRow(string row, bool isHeader)
        {
            var rowElement = new VisualElement();
            rowElement.style.flexDirection = FlexDirection.Row;
            if (isHeader)
                rowElement.style.backgroundColor = new Color(0.28f, 0.28f, 0.28f);
            
            rowElement.style.borderBottomWidth = 1;
            rowElement.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

            // Split and remove leading/trailing empty strings from the outer pipes
            var cellTexts = row.Trim('|').Split('|');
            foreach (var cellText in cellTexts)
            {
                var cell = new VisualElement();
                cell.style.flexGrow = 1;
                cell.style.flexBasis = 0;
                cell.style.paddingLeft = 8;
                cell.style.paddingRight = 8;
                cell.style.paddingTop = 6;
                cell.style.paddingBottom = 6;
                cell.style.borderRightWidth = 1;
                cell.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
                
                ProcessCellContent(cell, cellText, isHeader);
                rowElement.Add(cell);
            }

            return rowElement;
        }

        private void ProcessCellContent(VisualElement cell, string content, bool isHeader)
        {
            content = content.Trim();
            if (string.IsNullOrWhiteSpace(content)) return;

            if (TryMatchImage(content, out var imgPath, out var altText))
            {
                cell.Add(CreateImage(imgPath, altText));
            }
            else
            {
                var label = new Label(ProcessRichText(content));
                label.enableRichText = true;
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.fontSize = 12;
                label.style.color = new Color(0.85f, 0.85f, 0.85f);
                if (isHeader) label.style.unityFontStyleAndWeight = FontStyle.Bold;
                cell.Add(label);
            }
        }

        private bool TryMatchImage(string line, out string path, out string alt)
        {
            path = null;
            alt = null;

            if (string.IsNullOrEmpty(line)) return false;

            // Handle linked images: [![alt][ref]](url) or [![alt](path)](url)
            // We strip the outer link and look at the inner content
            var linkedMatch = Regex.Match(line, @"^\[\s*(!\[.*?\]\s*(?:\[.*?\]|\(.*?\)))\s*\]\(.*?\)$");
            if (linkedMatch.Success)
            {
                line = linkedMatch.Groups[1].Value.Trim();
            }

            // Reference style: ![alt][ref] or ![][ref] or ![ref][]
            var refMatch = Regex.Match(line, @"^!\[(?<alt>.*?)\]\s*\[(?<ref>.*?)\]$");
            if (refMatch.Success)
            {
                alt = refMatch.Groups["alt"].Value;
                string refKey = refMatch.Groups["ref"].Value;
                if (string.IsNullOrEmpty(refKey)) refKey = alt;
                
                if (_references.TryGetValue(refKey, out path))
                {
                    return true;
                }
            }

            // Inline style: ![alt](path)
            var inlineMatch = Regex.Match(line, @"^!\[(?<alt>.*?)\]\s*\((?<path>.*?)\)$");
            if (inlineMatch.Success)
            {
                alt = inlineMatch.Groups["alt"].Value;
                path = inlineMatch.Groups["path"].Value;
                return true;
            }

            return false;
        }

        private void ParseReferences()
        {
            _references.Clear();
            // Match [id]: path (handles optional <path> and optional title)
            // Added \s* at the beginning because Google Docs often indents these.
            var matches = Regex.Matches(_content, @"^\s*\[([^\]]+)\]:\s*<?([^>\s]+)>?(?:\s+[""(].*?["")])?\s*$", RegexOptions.Multiline);
            foreach (Match m in matches)
            {
                _references[m.Groups[1].Value] = m.Groups[2].Value.Trim();
            }
        }

        private VisualElement CreateHeader(string line)
        {
            int level = 0;
            while (level < line.Length && line[level] == '#') level++;
            
            string text = line.Substring(level).Trim();
            var label = new Label(ProcessRichText(text));
            label.enableRichText = true;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginTop = 15;
            label.style.marginBottom = 5;

            switch (level)
            {
                case 1: label.style.fontSize = 24; label.style.color = new Color(1, 1, 1); break;
                case 2: label.style.fontSize = 20; label.style.color = new Color(0.9f, 0.9f, 0.9f); break;
                case 3: label.style.fontSize = 18; label.style.color = new Color(0.85f, 0.85f, 0.85f); break;
                default: label.style.fontSize = 16; label.style.color = new Color(0.8f, 0.8f, 0.8f); break;
            }

            return label;
        }

        private VisualElement CreateParagraph(string text)
        {
            var label = new Label(ProcessRichText(text));
            label.enableRichText = true;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = 13;
            label.style.color = new Color(0.85f, 0.85f, 0.85f);
            label.style.marginBottom = 4;
            return label;
        }

        private VisualElement CreateListItem(string line)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.marginLeft = 15;
            container.style.marginBottom = 2;

            var bullet = new Label("•");
            if (Regex.IsMatch(line.TrimStart(), @"^\d+\. "))
            {
                var match = Regex.Match(line.TrimStart(), @"^(\d+\.)");
                bullet.text = match.Groups[1].Value;
                bullet.style.marginRight = 5;
            }
            else
            {
                bullet.style.marginRight = 8;
            }
            
            bullet.style.fontSize = 13;
            bullet.style.color = new Color(0.85f, 0.85f, 0.85f);
            container.Add(bullet);

            string text = Regex.Replace(line.TrimStart(), @"^([-*]|\d+\.)\s+", "");
            var label = new Label(ProcessRichText(text));
            label.enableRichText = true;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = 13;
            label.style.color = new Color(0.85f, 0.85f, 0.85f);
            label.style.flexGrow = 1;
            container.Add(label);

            return container;
        }

        private VisualElement CreateBlockquote(string line)
        {
            var container = new VisualElement();
            container.style.marginLeft = 10;
            container.style.marginTop = 5;
            container.style.marginBottom = 5;
            container.style.paddingLeft = 10;
            container.style.borderLeftWidth = 4;
            container.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f);

            string text = line.TrimStart().Substring(1).Trim();
            var label = new Label(ProcessRichText(text));
            label.enableRichText = true;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = 13;
            label.style.color = new Color(0.7f, 0.7f, 0.7f);
            label.style.unityFontStyleAndWeight = FontStyle.Italic;
            container.Add(label);

            return container;
        }

        private VisualElement CreateCodeBlock(string code)
        {
            var container = new VisualElement();
            container.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            container.style.paddingLeft = 10;
            container.style.paddingRight = 10;
            container.style.paddingTop = 8;
            container.style.paddingBottom = 8;
            container.style.marginTop = 10;
            container.style.marginBottom = 10;
            container.style.borderBottomLeftRadius = 4;
            container.style.borderBottomRightRadius = 4;
            container.style.borderTopLeftRadius = 4;
            container.style.borderTopRightRadius = 4;

            var label = new Label(code);
            // Attempt to use a monospaced font if available, fallback to standard if not
            var monoStyle = GUI.skin.FindStyle("monospacedLabel") ?? GUI.skin.FindStyle("TextArea");
            if (monoStyle != null && monoStyle.font != null)
                label.style.unityFont = monoStyle.font;

            label.style.fontSize = 12;
            label.style.color = new Color(0.8f, 0.9f, 0.8f);
            container.Add(label);

            return container;
        }

        private VisualElement CreateImage(string path, string alt)
        {
            var container = new VisualElement();
            container.style.marginTop = 10;
            container.style.marginBottom = 10;
            container.style.alignItems = Align.Center;

            // Resolve relative path
            string baseDir = Path.GetDirectoryName(_filePath);
            string fullImagePath = Path.Combine(baseDir, path).Replace("\\", "/");
            
            // Ensure path starts with Assets/ if it's for AssetDatabase
            if (!fullImagePath.StartsWith("Assets/"))
            {
                // This might happen if the output path is outside Assets (though not recommended)
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(fullImagePath);
            if (texture != null)
            {
                var image = new Image { image = texture };
                // Constrain size but keep aspect ratio? 
                // UIToolkit Image doesn't automatically keep aspect ratio well without help
                float maxWidth = 600;
                float width = texture.width;
                float height = texture.height;

                if (width > maxWidth)
                {
                    height = height * (maxWidth / width);
                    width = maxWidth;
                }

                image.style.width = width;
                image.style.height = height;
                container.Add(image);
            }
            else
            {
                var errorLabel = new Label($"[Image missing: {path}]");
                errorLabel.style.color = Color.gray;
                errorLabel.style.fontSize = 10;
                container.Add(errorLabel);
            }

            if (!string.IsNullOrEmpty(alt))
            {
                var altLabel = new Label(alt);
                altLabel.style.fontSize = 10;
                altLabel.style.opacity = 0.5f;
                altLabel.style.marginTop = 4;
                container.Add(altLabel);
            }

            return container;
        }

        private VisualElement CreateHorizontalRule()
        {
            var hr = new VisualElement();
            hr.style.height = 1;
            hr.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            hr.style.marginTop = 15;
            hr.style.marginBottom = 15;
            return hr;
        }

        private string ProcessRichText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            
            // Basic escape for existing tags if any
            text = text.Replace("<", "&lt;").Replace(">", "&gt;");
            
            // Bold **text** or __text__
            text = Regex.Replace(text, @"(\*\*|__)(.*?)\1", "<b>$2</b>");
            
            // Italic *text* or _text_
            text = Regex.Replace(text, @"(\*|_)(.*?)\1", "<i>$2</i>");
            
            // Inline code `text`
            text = Regex.Replace(text, @"`(.*?)`", "<color=#9cdcfe>$1</color>");

            return text;
        }
    }
}
