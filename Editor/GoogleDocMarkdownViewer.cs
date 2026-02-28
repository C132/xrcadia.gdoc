using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Xrcadia.GoogleDocMarkdown.Editor
{
    public class GoogleDocMarkdownViewer : EditorWindow
    {
        private const string ThemePrefKey = "GoogleDocMarkdownViewer_Theme";
        private const string LegacyPaperwhitePrefKey = "GoogleDocMarkdownViewer_Paperwhite";

        private string _filePath;
        private string _content;
        private Dictionary<string, string> _references = new Dictionary<string, string>();
        private MarkdownTheme _theme;

        private static readonly string[] MonoFontNames = { "Menlo", "Consolas", "Courier New", "Courier" };
        private static Font _monoFont;

        private static Font MonoFont
        {
            get
            {
                if (_monoFont == null)
                {
                    foreach (var name in MonoFontNames)
                    {
                        _monoFont = Font.CreateDynamicFontFromOSFont(name, 12);
                        if (_monoFont != null) break;
                    }
                }
                return _monoFont;
            }
        }

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
            _theme = LoadTheme();
            BuildUI();
        }

        private MarkdownTheme LoadTheme()
        {
            var saved = EditorPrefs.GetString(ThemePrefKey, "");
            if (!string.IsNullOrEmpty(saved))
                return MarkdownTheme.FindByName(saved);

            // Migrate from legacy paperwhite toggle
            if (EditorPrefs.GetBool(LegacyPaperwhitePrefKey, false))
                return MarkdownTheme.FindByName("Paperwhite");

            return MarkdownTheme.Default;
        }

        private void SetTheme(MarkdownTheme theme)
        {
            _theme = theme;
            EditorPrefs.SetString(ThemePrefKey, theme.Name);
            BuildUI();
        }

        private void ShowThemeMenu()
        {
            var menu = new GenericMenu();
            bool addedDarkHeader = false;
            bool addedLightHeader = false;

            foreach (var theme in MarkdownTheme.All)
            {
                if (!theme.IsLight && !addedDarkHeader)
                {
                    menu.AddDisabledItem(new GUIContent("Dark"));
                    addedDarkHeader = true;
                }
                else if (theme.IsLight && !addedLightHeader)
                {
                    menu.AddSeparator("");
                    menu.AddDisabledItem(new GUIContent("Light"));
                    addedLightHeader = true;
                }

                bool isActive = theme.Name == _theme.Name;
                var captured = theme;
                menu.AddItem(new GUIContent("  " + theme.Name), isActive, () => SetTheme(captured));
            }

            menu.ShowAsContext();
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.backgroundColor = _theme.Background;

            // Toolbar
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.paddingLeft = 10;
            toolbar.style.paddingRight = 10;
            toolbar.style.paddingTop = 5;
            toolbar.style.paddingBottom = 5;
            toolbar.style.backgroundColor = _theme.ToolbarBackground;
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = _theme.ToolbarBorder;

            var pathLabel = new Label(_filePath);
            pathLabel.style.flexGrow = 1;
            pathLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            pathLabel.style.fontSize = 11;
            pathLabel.style.color = _theme.TextMuted;

            toolbar.Add(pathLabel);

            var themeBtn = new Button(() => ShowThemeMenu()) { text = _theme.Name + " \u25BE" };
            themeBtn.style.width = 140;
            themeBtn.style.height = 20;
            themeBtn.style.marginRight = 4;
            toolbar.Add(themeBtn);

            var reloadBtn = new Button(Refresh) { text = "Reload" };
            reloadBtn.style.height = 20;
            toolbar.Add(reloadBtn);

            rootVisualElement.Add(toolbar);

            // Scroll area
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            rootVisualElement.Add(scroll);

            // Book-like centered content column
            var page = new VisualElement();
            page.style.maxWidth = 680;
            page.style.alignSelf = Align.Center;
            page.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            page.style.paddingLeft = 48;
            page.style.paddingRight = 48;
            page.style.paddingTop = 36;
            page.style.paddingBottom = 60;
            scroll.Add(page);

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
                        page.Add(CreateTable(tableLines));
                        tableLines.Clear();
                    }

                    if (inCodeBlock)
                    {
                        page.Add(CreateCodeBlock(codeBlockContent.TrimEnd()));
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
                    page.Add(CreateTable(tableLines));
                    tableLines.Clear();
                }

                // Headers
                if (trimmedLine.StartsWith("#"))
                {
                    page.Add(CreateHeader(trimmedLine));
                    continue;
                }

                // Horizontal Rule
                if (trimmedLine == "---" || trimmedLine == "***" || trimmedLine == "___")
                {
                    page.Add(CreateHorizontalRule());
                    continue;
                }

                // Images (possibly linked)
                if (TryMatchImage(trimmedLine, out var imgPath, out var altText))
                {
                    page.Add(CreateImage(imgPath, altText));
                    continue;
                }

                // Lists
                if (trimmedLine.StartsWith("- ") || trimmedLine.StartsWith("* ") || Regex.IsMatch(trimmedLine, @"^\d+\. "))
                {
                    page.Add(CreateListItem(line));
                    continue;
                }

                // Blockquotes
                if (trimmedLine.StartsWith("> "))
                {
                    page.Add(CreateBlockquote(line));
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
                    page.Add(CreateParagraph(line));
                }
                else
                {
                    var spacer = new VisualElement { style = { height = 8 } };
                    page.Add(spacer);
                }
            }

            if (tableLines.Count > 0)
            {
                page.Add(CreateTable(tableLines));
            }
        }

        private VisualElement CreateTable(List<string> rows)
        {
            var table = new VisualElement();
            table.style.marginTop = 14;
            table.style.marginBottom = 14;
            table.style.borderTopWidth = 1;
            table.style.borderBottomWidth = 1;
            table.style.borderLeftWidth = 1;
            table.style.borderRightWidth = 1;
            table.style.borderTopColor = _theme.RuleBorder;
            table.style.borderBottomColor = _theme.RuleBorder;
            table.style.borderLeftColor = _theme.RuleBorder;
            table.style.borderRightColor = _theme.RuleBorder;
            table.style.backgroundColor = _theme.TableBackground;
            table.style.borderTopLeftRadius = 3;
            table.style.borderTopRightRadius = 3;
            table.style.borderBottomLeftRadius = 3;
            table.style.borderBottomRightRadius = 3;

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
                rowElement.style.backgroundColor = _theme.TableHeaderBackground;

            rowElement.style.borderBottomWidth = 1;
            rowElement.style.borderBottomColor = _theme.RuleBorder;

            var cellTexts = row.Trim('|').Split('|');
            foreach (var cellText in cellTexts)
            {
                var cell = new VisualElement();
                cell.style.flexGrow = 1;
                cell.style.flexBasis = 0;
                cell.style.paddingLeft = 10;
                cell.style.paddingRight = 10;
                cell.style.paddingTop = 7;
                cell.style.paddingBottom = 7;
                cell.style.borderRightWidth = 1;
                cell.style.borderRightColor = _theme.RuleBorder;

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
                label.style.fontSize = 13;
                label.style.color = _theme.TextBody;
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

            var wrapper = new VisualElement();

            var label = new Label(ProcessRichText(text));
            label.enableRichText = true;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = _theme.Heading;

            switch (level)
            {
                case 1:
                    label.style.fontSize = 28;
                    wrapper.style.marginTop = 28;
                    wrapper.style.marginBottom = 12;
                    wrapper.style.paddingBottom = 8;
                    wrapper.style.borderBottomWidth = 1;
                    wrapper.style.borderBottomColor = _theme.RuleBorder;
                    break;
                case 2:
                    label.style.fontSize = 22;
                    wrapper.style.marginTop = 24;
                    wrapper.style.marginBottom = 8;
                    wrapper.style.paddingBottom = 6;
                    wrapper.style.borderBottomWidth = 1;
                    wrapper.style.borderBottomColor = _theme.RuleBorder;
                    break;
                case 3:
                    label.style.fontSize = 18;
                    wrapper.style.marginTop = 20;
                    wrapper.style.marginBottom = 6;
                    break;
                default:
                    label.style.fontSize = 15;
                    wrapper.style.marginTop = 16;
                    wrapper.style.marginBottom = 4;
                    break;
            }

            wrapper.Add(label);
            return wrapper;
        }

        private VisualElement CreateParagraph(string text)
        {
            var label = new Label(ProcessRichText(text));
            label.enableRichText = true;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = 14;
            label.style.color = _theme.TextBody;
            label.style.marginBottom = 6;
            return label;
        }

        private VisualElement CreateListItem(string line)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.marginLeft = 20;
            container.style.marginBottom = 3;

            var bullet = new Label("\u2022");
            if (Regex.IsMatch(line.TrimStart(), @"^\d+\. "))
            {
                var match = Regex.Match(line.TrimStart(), @"^(\d+\.)");
                bullet.text = match.Groups[1].Value;
                bullet.style.marginRight = 6;
            }
            else
            {
                bullet.style.marginRight = 10;
            }

            bullet.style.fontSize = 14;
            bullet.style.color = _theme.TextBody;
            container.Add(bullet);

            string text = Regex.Replace(line.TrimStart(), @"^([-*]|\d+\.)\s+", "");
            var label = new Label(ProcessRichText(text));
            label.enableRichText = true;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = 14;
            label.style.color = _theme.TextBody;
            label.style.flexGrow = 1;
            container.Add(label);

            return container;
        }

        private VisualElement CreateBlockquote(string line)
        {
            var container = new VisualElement();
            container.style.marginLeft = 12;
            container.style.marginTop = 8;
            container.style.marginBottom = 8;
            container.style.paddingLeft = 16;
            container.style.paddingTop = 4;
            container.style.paddingBottom = 4;
            container.style.borderLeftWidth = 3;
            container.style.borderLeftColor = _theme.BlockquoteBorder;

            string text = line.TrimStart().Substring(1).Trim();
            var label = new Label(ProcessRichText(text));
            label.enableRichText = true;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = 14;
            label.style.color = _theme.BlockquoteText;
            label.style.unityFontStyleAndWeight = FontStyle.Italic;
            container.Add(label);

            return container;
        }

        private VisualElement CreateCodeBlock(string code)
        {
            var container = new VisualElement();
            container.style.backgroundColor = _theme.CodeBackground;
            container.style.paddingLeft = 14;
            container.style.paddingRight = 14;
            container.style.paddingTop = 10;
            container.style.paddingBottom = 10;
            container.style.marginTop = 12;
            container.style.marginBottom = 12;
            container.style.borderTopLeftRadius = 4;
            container.style.borderTopRightRadius = 4;
            container.style.borderBottomLeftRadius = 4;
            container.style.borderBottomRightRadius = 4;
            container.style.borderTopWidth = 1;
            container.style.borderBottomWidth = 1;
            container.style.borderLeftWidth = 1;
            container.style.borderRightWidth = 1;
            container.style.borderTopColor = _theme.RuleBorder;
            container.style.borderBottomColor = _theme.RuleBorder;
            container.style.borderLeftColor = _theme.RuleBorder;
            container.style.borderRightColor = _theme.RuleBorder;

            var label = new Label(WebUtility.HtmlDecode(code));
            if (MonoFont != null)
                label.style.unityFont = MonoFont;

            label.style.fontSize = 12;
            label.style.color = _theme.CodeText;
            container.Add(label);

            return container;
        }

        private VisualElement CreateImage(string path, string alt)
        {
            var container = new VisualElement();
            container.style.marginTop = 14;
            container.style.marginBottom = 14;
            container.style.alignItems = Align.Center;

            string baseDir = Path.GetDirectoryName(_filePath);
            string fullImagePath = Path.Combine(baseDir, path).Replace("\\", "/");

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(fullImagePath);
            if (texture != null)
            {
                var image = new Image { image = texture };
                float maxWidth = 580;
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
                errorLabel.style.color = _theme.TextMuted;
                errorLabel.style.fontSize = 11;
                container.Add(errorLabel);
            }

            if (!string.IsNullOrEmpty(alt))
            {
                var altLabel = new Label(alt);
                altLabel.style.fontSize = 11;
                altLabel.style.color = _theme.TextMuted;
                altLabel.style.marginTop = 4;
                container.Add(altLabel);
            }

            return container;
        }

        private VisualElement CreateHorizontalRule()
        {
            var container = new VisualElement();
            container.style.marginTop = 20;
            container.style.marginBottom = 20;
            container.style.alignItems = Align.Center;

            var hr = new VisualElement();
            hr.style.height = 1;
            hr.style.backgroundColor = _theme.RuleBorder;
            hr.style.width = new StyleLength(new Length(60, LengthUnit.Percent));
            container.Add(hr);

            return container;
        }

        private string ProcessRichText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            // Decode HTML entities from source (e.g., Google Docs exports &lt; &gt; &amp;)
            text = WebUtility.HtmlDecode(text);

            // Shelter angle brackets from Unity rich text parsing using placeholders
            text = text.Replace("<", "\x01").Replace(">", "\x02");

            // Bold **text** or __text__
            text = Regex.Replace(text, @"(\*\*|__)(.*?)\1", "<b>$2</b>");

            // Italic *text* or _text_
            text = Regex.Replace(text, @"(\*|_)(.*?)\1", "<i>$2</i>");

            // Inline code `text`
            text = Regex.Replace(text, @"`(.*?)`", $"<color={_theme.InlineCodeColor}>$1</color>");

            // Restore angle brackets wrapped in noparse to display literally
            text = text.Replace("\x01", "<noparse><</noparse>").Replace("\x02", "<noparse>></noparse>");

            return text;
        }
    }
}
