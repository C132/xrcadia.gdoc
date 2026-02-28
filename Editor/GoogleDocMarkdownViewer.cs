using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Xrcadia.GoogleDocMarkdown.Editor
{
    public class GoogleDocMarkdownViewer : EditorWindow
    {
        // --- Pref keys ---
        private const string ThemePrefKey = "GoogleDocMarkdownViewer_Theme";
        private const string LegacyPaperwhitePrefKey = "GoogleDocMarkdownViewer_Paperwhite";
        private const string SidebarPrefKey = "GoogleDocMarkdownViewer_Sidebar";
        private const string PinnedFilesPrefKey = "GoogleDocMarkdownViewer_PinnedFiles";
        private const string RecentFilesPrefKey = "GoogleDocMarkdownViewer_RecentFiles";
        private const string SidebarWidthPrefKey = "GoogleDocMarkdownViewer_SidebarWidth";
        private const string ExpandDotPathsPrefKey = "GoogleDocMarkdownViewer_ExpandDotPaths";

        // --- Constants ---
        private const int MaxRecentFiles = 15;
        private const float DefaultSidebarWidth = 220;
        private const float MinSidebarWidth = 140;
        private const float MaxSidebarWidth = 500;

        // --- Compiled regex (allocated once) ---
        private static readonly Regex RxBold = new Regex(@"(\*\*|__)(.*?)\1", RegexOptions.Compiled);
        private static readonly Regex RxItalic = new Regex(@"(\*|_)(.*?)\1", RegexOptions.Compiled);
        private static readonly Regex RxInlineCode = new Regex(@"`(.*?)`", RegexOptions.Compiled);
        private static readonly Regex RxStripTags = new Regex(@"<.*?>", RegexOptions.Compiled);
        private static readonly Regex RxOrderedList = new Regex(@"^\d+\. ", RegexOptions.Compiled);
        private static readonly Regex RxOrderedListNum = new Regex(@"^(\d+\.)", RegexOptions.Compiled);
        private static readonly Regex RxListPrefix = new Regex(@"^([-*]|\d+\.)\s+", RegexOptions.Compiled);
        private static readonly Regex RxTableSep = new Regex(@"^\|[\s\-:|]+\|$", RegexOptions.Compiled);
        private static readonly Regex RxRefDef = new Regex(@"^\[.*\]:", RegexOptions.Compiled);
        private static readonly Regex RxRefParse = new Regex(@"^\s*\[([^\]]+)\]:\s*<?([^>\s]+)>?(?:\s+[""(].*?["")])?\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex RxLinkedImage = new Regex(@"^\[\s*(!\[.*?\]\s*(?:\[.*?\]|\(.*?\)))\s*\]\(.*?\)$", RegexOptions.Compiled);
        private static readonly Regex RxRefImage = new Regex(@"^!\[(?<alt>.*?)\]\s*\[(?<ref>.*?)\]$", RegexOptions.Compiled);
        private static readonly Regex RxInlineImage = new Regex(@"^!\[(?<alt>.*?)\]\s*\((?<path>.*?)\)$", RegexOptions.Compiled);
        private static readonly Regex RxStrikethrough = new Regex(@"~~(.*?)~~", RegexOptions.Compiled);
        private static readonly Regex RxLink = new Regex(@"\[([^\]]+)\]\([^\)]+\)", RegexOptions.Compiled);
        private static readonly Regex RxEscape = new Regex(@"\\([^A-Za-z0-9\s])", RegexOptions.Compiled);

        // --- State ---
        private string _filePath;
        private string _content;
        private Dictionary<string, string> _references = new Dictionary<string, string>();
        private MarkdownTheme _theme;

        private bool _sidebarOpen;
        private float _sidebarWidth;
        private bool _expandDotPaths;
        private ScrollView _contentScroll;
        private VisualElement _sidebarElement;
        private VisualElement _page;
        private VisualElement _outlineContainer;
        private Label _pathLabel;
        private List<(int level, string text, VisualElement element)> _headings = new List<(int, string, VisualElement)>();
        private List<(string path, Label label)> _fileLabels = new List<(string, Label)>();
        private List<string> _pinnedFiles = new List<string>();
        private List<string> _recentFiles = new List<string>();

        // --- Cached file data (rebuilt on full BuildUI, not per-file-switch) ---
        private List<string> _cachedMdFiles;
        private FolderNode _cachedFileTree;
        private FolderNode _cachedFileTreeFlat;

        // --- Static caches ---
        private static readonly string[] MonoFontNames = { "Menlo", "Consolas", "Courier New", "Courier" };
        private static Font _monoFont;
        private static StyleCursor? _resizeCursor;
        private static Texture2D _folderIcon;
        private static Texture2D _folderOpenIcon;

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

        private static StyleCursor ResizeHorizontalCursor
        {
            get
            {
                if (_resizeCursor == null)
                    _resizeCursor = CreateSystemCursor(MouseCursor.ResizeHorizontal);
                return _resizeCursor.Value;
            }
        }

        private static StyleCursor CreateSystemCursor(MouseCursor cursorType)
        {
            var cursor = new UnityEngine.UIElements.Cursor();
            var field = typeof(UnityEngine.UIElements.Cursor).GetField(
                "m_DefaultCursorId",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                object boxed = cursor;
                field.SetValue(boxed, (int)cursorType);
                cursor = (UnityEngine.UIElements.Cursor)boxed;
            }
            return new StyleCursor(cursor);
        }

        private static Texture2D GetFolderIcon(bool open)
        {
            if (open)
            {
                if (_folderOpenIcon == null)
                    _folderOpenIcon = EditorGUIUtility.IconContent("FolderOpened Icon")?.image as Texture2D;
                return _folderOpenIcon;
            }
            if (_folderIcon == null)
                _folderIcon = EditorGUIUtility.IconContent("Folder Icon")?.image as Texture2D;
            return _folderIcon;
        }

        private Color HoverBackground => _theme.IsLight
            ? new Color(0f, 0f, 0f, 0.06f)
            : new Color(1f, 1f, 1f, 0.06f);

        // --- Folder tree ---

        private class FolderNode
        {
            public readonly SortedDictionary<string, FolderNode> Folders =
                new SortedDictionary<string, FolderNode>(StringComparer.OrdinalIgnoreCase);
            public readonly List<string> Files = new List<string>();
        }

        private static FolderNode BuildFileTree(List<string> paths, bool expandDots)
        {
            var root = new FolderNode();
            foreach (var path in paths)
            {
                var segments = new List<string>();
                var rawParts = path.Replace("\\", "/").Split('/');

                for (int i = 0; i < rawParts.Length - 1; i++)
                {
                    var part = rawParts[i];

                    // Only expand dots for reverse-DNS package names (com.* segments).
                    if (expandDots && part.StartsWith("com.", StringComparison.OrdinalIgnoreCase))
                    {
                        var stripped = part.Substring(4);
                        segments.AddRange(stripped.Split('.'));
                    }
                    else
                    {
                        segments.Add(part);
                    }
                }

                var current = root;
                foreach (var seg in segments)
                {
                    if (!current.Folders.TryGetValue(seg, out var child))
                    {
                        child = new FolderNode();
                        current.Folders[seg] = child;
                    }
                    current = child;
                }
                current.Files.Add(path);
            }
            return root;
        }

        private void RenderFolderTree(VisualElement parent, FolderNode node)
        {
            foreach (var kvp in node.Folders)
            {
                var foldout = new Foldout { text = kvp.Key, value = false };
                StyleTreeFoldout(foldout);
                RenderFolderTree(foldout, kvp.Value);
                parent.Add(foldout);
            }

            foreach (var filePath in node.Files)
            {
                var row = CreateFileRow(filePath, false);
                row.style.marginLeft = 18;
                parent.Add(row);
            }
        }

        private void InvalidateFileCache()
        {
            _cachedMdFiles = null;
            _cachedFileTree = null;
            _cachedFileTreeFlat = null;
        }

        private List<string> GetMarkdownFiles()
        {
            if (_cachedMdFiles == null)
            {
                _cachedMdFiles = new List<string>();
                var guids = AssetDatabase.FindAssets("t:TextAsset");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                        _cachedMdFiles.Add(path);
                }
                _cachedMdFiles.Sort(StringComparer.OrdinalIgnoreCase);
            }
            return _cachedMdFiles;
        }

        private FolderNode GetFileTree()
        {
            if (_expandDotPaths)
            {
                if (_cachedFileTree == null)
                    _cachedFileTree = BuildFileTree(GetMarkdownFiles(), true);
                return _cachedFileTree;
            }
            if (_cachedFileTreeFlat == null)
                _cachedFileTreeFlat = BuildFileTree(GetMarkdownFiles(), false);
            return _cachedFileTreeFlat;
        }

        // --- Window entry points ---

        public static void ShowWindow(string relativePath)
        {
            var window = GetWindow<GoogleDocMarkdownViewer>("Markdown Viewer");
            window._filePath = relativePath;
            window.titleContent = new GUIContent("MD: " + Path.GetFileName(relativePath), EditorGUIUtility.IconContent("TextAsset Icon").image);
            window.minSize = new Vector2(400, 500);
            window.AddRecentFile(relativePath);
            window.Refresh();
        }

        private void OpenFile(string relativePath)
        {
            _filePath = relativePath;
            titleContent = new GUIContent("MD: " + Path.GetFileName(relativePath), EditorGUIUtility.IconContent("TextAsset Icon").image);
            AddRecentFile(relativePath);
            LoadFileContent();
            RebuildContent();
        }

        private void LoadFileContent()
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
        }

        private void Refresh()
        {
            if (string.IsNullOrEmpty(_filePath)) return;
            LoadFileContent();
            InvalidateFileCache();

            if (rootVisualElement != null)
            {
                BuildUI();
            }
        }

        public void CreateGUI()
        {
            _theme = LoadTheme();
            _sidebarOpen = EditorPrefs.GetBool(SidebarPrefKey, false);
            _sidebarWidth = EditorPrefs.GetFloat(SidebarWidthPrefKey, DefaultSidebarWidth);
            _expandDotPaths = EditorPrefs.GetBool(ExpandDotPathsPrefKey, true);
            _pinnedFiles = LoadFileList(PinnedFilesPrefKey);
            _recentFiles = LoadFileList(RecentFilesPrefKey);
            BuildUI();
        }

        // --- Theme ---

        private MarkdownTheme LoadTheme()
        {
            var saved = EditorPrefs.GetString(ThemePrefKey, "");
            if (!string.IsNullOrEmpty(saved))
                return MarkdownTheme.FindByName(saved);

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

        // --- Settings ---

        private void ShowSettingsMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Expand package dots to folders"), _expandDotPaths, () =>
            {
                _expandDotPaths = !_expandDotPaths;
                EditorPrefs.SetBool(ExpandDotPathsPrefKey, _expandDotPaths);
                InvalidateFileCache();
                BuildUI();
            });
            menu.ShowAsContext();
        }

        // --- File list persistence ---

        private static List<string> LoadFileList(string prefKey)
        {
            var raw = EditorPrefs.GetString(prefKey, "");
            if (string.IsNullOrEmpty(raw)) return new List<string>();
            return new List<string>(raw.Split('\n'));
        }

        private static void SaveFileList(string prefKey, List<string> list)
        {
            EditorPrefs.SetString(prefKey, string.Join("\n", list));
        }

        private void AddRecentFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            _recentFiles.Remove(path);
            _recentFiles.Insert(0, path);
            if (_recentFiles.Count > MaxRecentFiles)
                _recentFiles.RemoveRange(MaxRecentFiles, _recentFiles.Count - MaxRecentFiles);
            SaveFileList(RecentFilesPrefKey, _recentFiles);
        }

        private void TogglePin(string path)
        {
            if (_pinnedFiles.Contains(path))
                _pinnedFiles.Remove(path);
            else
                _pinnedFiles.Insert(0, path);
            SaveFileList(PinnedFilesPrefKey, _pinnedFiles);
            BuildUI();
        }

        private void ShowFileContextMenu(string path)
        {
            var menu = new GenericMenu();
            bool isPinned = _pinnedFiles.Contains(path);
            menu.AddItem(new GUIContent(isPinned ? "Unpin" : "Pin"), false, () => TogglePin(path));
            menu.ShowAsContext();
        }

        // --- Hover helper ---

        private void AddHoverHighlight(VisualElement element)
        {
            var bg = HoverBackground;
            element.RegisterCallback<MouseEnterEvent>(_ => element.style.backgroundColor = bg);
            element.RegisterCallback<MouseLeaveEvent>(_ => element.style.backgroundColor = StyleKeyword.Null);
        }

        // --- Full UI build (theme change, sidebar toggle, pin/unpin) ---

        private void BuildUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.backgroundColor = _theme.Background;
            _fileLabels.Clear();

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

            var sidebarBtn = new Label(_sidebarOpen ? "\u25C0" : "\u2261");
            sidebarBtn.style.width = 22;
            sidebarBtn.style.height = 20;
            sidebarBtn.style.marginRight = 6;
            sidebarBtn.style.fontSize = 13;
            sidebarBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            sidebarBtn.style.color = _theme.TextMuted;
            sidebarBtn.RegisterCallback<ClickEvent>(_ =>
            {
                _sidebarOpen = !_sidebarOpen;
                EditorPrefs.SetBool(SidebarPrefKey, _sidebarOpen);
                BuildUI();
            });
            sidebarBtn.RegisterCallback<MouseEnterEvent>(_ => sidebarBtn.style.color = _theme.Heading);
            sidebarBtn.RegisterCallback<MouseLeaveEvent>(_ => sidebarBtn.style.color = _theme.TextMuted);
            toolbar.Add(sidebarBtn);

            _pathLabel = new Label(_filePath);
            _pathLabel.style.flexGrow = 1;
            _pathLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            _pathLabel.style.fontSize = 11;
            _pathLabel.style.color = _theme.TextMuted;
            toolbar.Add(_pathLabel);

            var themeBtn = new Button(() => ShowThemeMenu()) { text = _theme.Name + " \u25BE" };
            themeBtn.style.width = 140;
            themeBtn.style.height = 20;
            themeBtn.style.marginRight = 4;
            toolbar.Add(themeBtn);

            // Settings gear
            var gearBtn = new Label("\u2699");
            gearBtn.style.width = 22;
            gearBtn.style.height = 20;
            gearBtn.style.marginRight = 4;
            gearBtn.style.fontSize = 15;
            gearBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            gearBtn.style.color = _theme.TextMuted;
            gearBtn.RegisterCallback<ClickEvent>(_ => ShowSettingsMenu());
            gearBtn.RegisterCallback<MouseEnterEvent>(_ => gearBtn.style.color = _theme.Heading);
            gearBtn.RegisterCallback<MouseLeaveEvent>(_ => gearBtn.style.color = _theme.TextMuted);
            toolbar.Add(gearBtn);

            var reloadBtn = new Button(Refresh) { text = "Reload" };
            reloadBtn.style.height = 20;
            toolbar.Add(reloadBtn);

            rootVisualElement.Add(toolbar);

            // Body: sidebar + handle + content
            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1;
            rootVisualElement.Add(body);

            // Content scroll
            _contentScroll = new ScrollView();
            _contentScroll.style.flexGrow = 1;

            _page = new VisualElement();
            _page.style.maxWidth = 680;
            _page.style.alignSelf = Align.Center;
            _page.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            _page.style.paddingLeft = 48;
            _page.style.paddingRight = 48;
            _page.style.paddingTop = 36;
            _page.style.paddingBottom = 60;
            _contentScroll.Add(_page);

            // Sidebar
            if (_sidebarOpen)
            {
                _sidebarElement = BuildSidebar();
                body.Add(_sidebarElement);

                // Drag handle
                var handle = new VisualElement();
                handle.style.width = 12;
                handle.style.justifyContent = Justify.Center;
                handle.style.alignItems = Align.Center;
                handle.style.cursor = ResizeHorizontalCursor;

                var handleLine = new VisualElement();
                handleLine.style.width = 1;
                handleLine.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
                handleLine.style.backgroundColor = _theme.RuleBorder;
                handle.Add(handleLine);

                bool dragging = false;
                float dragStartX = 0;
                float dragStartWidth = 0;

                var handleHoverColor = _theme.IsLight
                    ? new Color(0f, 0f, 0f, 0.25f)
                    : new Color(1f, 1f, 1f, 0.20f);

                handle.RegisterCallback<MouseEnterEvent>(_ => handleLine.style.backgroundColor = handleHoverColor);
                handle.RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    if (!dragging)
                        handleLine.style.backgroundColor = _theme.RuleBorder;
                });

                handle.RegisterCallback<MouseDownEvent>(e =>
                {
                    if (e.button != 0) return;
                    dragging = true;
                    dragStartX = e.mousePosition.x;
                    dragStartWidth = _sidebarWidth;
                    handle.CaptureMouse();
                    handleLine.style.backgroundColor = handleHoverColor;
                    e.StopPropagation();
                });
                handle.RegisterCallback<MouseMoveEvent>(e =>
                {
                    if (!dragging) return;
                    float delta = e.mousePosition.x - dragStartX;
                    float newWidth = Mathf.Clamp(dragStartWidth + delta, MinSidebarWidth, MaxSidebarWidth);
                    _sidebarWidth = newWidth;
                    _sidebarElement.style.width = newWidth;
                    _sidebarElement.style.minWidth = newWidth;
                    e.StopPropagation();
                });
                handle.RegisterCallback<MouseUpEvent>(e =>
                {
                    if (!dragging) return;
                    dragging = false;
                    handle.ReleaseMouse();
                    EditorPrefs.SetFloat(SidebarWidthPrefKey, _sidebarWidth);
                    handleLine.style.backgroundColor = _theme.RuleBorder;
                    e.StopPropagation();
                });

                body.Add(handle);
            }

            body.Add(_contentScroll);

            RebuildContent();
        }

        /// <summary>
        /// Rebuilds only the content pane and outline. The sidebar file browser stays intact.
        /// </summary>
        private void RebuildContent()
        {
            if (_page == null || _contentScroll == null) return;

            _page.Clear();
            _headings.Clear();

            if (_pathLabel != null)
                _pathLabel.text = _filePath;

            if (!string.IsNullOrEmpty(_content))
            {
                ParseReferences();

                var lines = _content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                bool inCodeBlock = false;
                var codeBuilder = new StringBuilder();
                List<string> tableLines = new List<string>();

                foreach (var line in lines)
                {
                    var trimmedLine = line.TrimStart();
                    // Quick length guard before StartsWith on trimmed
                    int trimLen = trimmedLine.Length;

                    if (trimLen >= 3 && trimmedLine[0] == '`' && trimmedLine[1] == '`' && trimmedLine[2] == '`')
                    {
                        if (tableLines.Count > 0)
                        {
                            _page.Add(CreateTable(tableLines));
                            tableLines.Clear();
                        }

                        if (inCodeBlock)
                        {
                            _page.Add(CreateCodeBlock(codeBuilder.ToString().TrimEnd()));
                            codeBuilder.Clear();
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
                        codeBuilder.AppendLine(line);
                        continue;
                    }

                    if (trimLen > 0 && trimmedLine[0] == '|' && trimmedLine[trimLen - 1] == '|')
                    {
                        tableLines.Add(trimmedLine);
                        continue;
                    }
                    else if (tableLines.Count > 0)
                    {
                        _page.Add(CreateTable(tableLines));
                        tableLines.Clear();
                    }

                    if (trimLen > 0 && trimmedLine[0] == '#')
                    {
                        _page.Add(CreateHeader(trimmedLine));
                        continue;
                    }

                    if (trimmedLine == "---" || trimmedLine == "***" || trimmedLine == "___")
                    {
                        _page.Add(CreateHorizontalRule());
                        continue;
                    }

                    if (TryMatchImage(trimmedLine, out var imgPath, out var altText))
                    {
                        _page.Add(CreateImage(imgPath, altText));
                        continue;
                    }

                    if (trimLen >= 2 && ((trimmedLine[0] == '-' || trimmedLine[0] == '*') && trimmedLine[1] == ' ')
                        || RxOrderedList.IsMatch(trimmedLine))
                    {
                        _page.Add(CreateListItem(line));
                        continue;
                    }

                    if (trimLen >= 2 && trimmedLine[0] == '>' && trimmedLine[1] == ' ')
                    {
                        _page.Add(CreateBlockquote(line));
                        continue;
                    }

                    if (RxRefDef.IsMatch(trimmedLine))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _page.Add(CreateParagraph(line));
                    }
                    else
                    {
                        var spacer = new VisualElement { style = { height = 8 } };
                        _page.Add(spacer);
                    }
                }

                if (tableLines.Count > 0)
                {
                    _page.Add(CreateTable(tableLines));
                }
            }

            RebuildOutline();
            UpdateFileHighlights();
        }

        private void RebuildOutline()
        {
            if (_outlineContainer == null) return;

            _outlineContainer.Clear();

            if (_headings.Count == 0)
            {
                var empty = new Label("No headings");
                empty.style.color = _theme.TextMuted;
                empty.style.fontSize = 11;
                empty.style.paddingLeft = 8;
                _outlineContainer.Add(empty);
            }
            else
            {
                var hoverBg = HoverBackground;

                foreach (var (level, text, element) in _headings)
                {
                    var row = new VisualElement();
                    row.style.paddingLeft = 8 + (level - 1) * 14;
                    row.style.paddingTop = 2;
                    row.style.paddingBottom = 2;
                    row.style.paddingRight = 4;
                    row.style.borderTopLeftRadius = 3;
                    row.style.borderTopRightRadius = 3;
                    row.style.borderBottomLeftRadius = 3;
                    row.style.borderBottomRightRadius = 3;
                    row.tooltip = $"H{level}: {text}";

                    var entry = new Label(text);
                    entry.style.fontSize = 12;
                    entry.style.color = _theme.TextBody;

                    if (level == 1)
                        entry.style.unityFontStyleAndWeight = FontStyle.Bold;

                    row.Add(entry);

                    var captured = element;
                    row.RegisterCallback<ClickEvent>(_ => _contentScroll.ScrollTo(captured));
                    row.RegisterCallback<MouseEnterEvent>(_ =>
                    {
                        row.style.backgroundColor = hoverBg;
                        entry.style.color = _theme.Heading;
                    });
                    row.RegisterCallback<MouseLeaveEvent>(_ =>
                    {
                        row.style.backgroundColor = StyleKeyword.Null;
                        entry.style.color = _theme.TextBody;
                    });

                    _outlineContainer.Add(row);
                }
            }
        }

        private void UpdateFileHighlights()
        {
            foreach (var (path, label) in _fileLabels)
            {
                bool isCurrent = path == _filePath;
                label.style.color = isCurrent ? _theme.Heading : _theme.TextBody;
                label.style.unityFontStyleAndWeight = isCurrent ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        // --- Sidebar ---

        private VisualElement BuildSidebar()
        {
            var sidebar = new ScrollView();
            sidebar.style.width = _sidebarWidth;
            sidebar.style.minWidth = _sidebarWidth;
            sidebar.style.backgroundColor = _theme.ToolbarBackground;
            sidebar.style.paddingTop = 8;
            sidebar.style.paddingBottom = 8;

            // Files section
            var filesFoldout = new Foldout { text = "Files", value = true };
            StyleSectionFoldout(filesFoldout);

            // Pinned
            if (_pinnedFiles.Count > 0)
            {
                var pinnedFoldout = new Foldout { text = "Pinned", value = true };
                StyleSubFoldout(pinnedFoldout);

                foreach (var path in _pinnedFiles)
                {
                    if (!FileExists(path)) continue;
                    pinnedFoldout.Add(CreateFileRow(path, true));
                }

                filesFoldout.Add(pinnedFoldout);
            }

            // Recent
            if (_recentFiles.Count > 0)
            {
                var recentFoldout = new Foldout { text = "Recent", value = true };
                StyleSubFoldout(recentFoldout);

                foreach (var path in _recentFiles)
                {
                    if (!FileExists(path)) continue;
                    recentFoldout.Add(CreateFileRow(path, false));
                }

                filesFoldout.Add(recentFoldout);
            }

            // All Files — folder tree
            var allFoldout = new Foldout { text = "All Files", value = false };
            StyleSubFoldout(allFoldout);

            RenderFolderTree(allFoldout, GetFileTree());

            filesFoldout.Add(allFoldout);
            sidebar.Add(filesFoldout);

            // Outline section
            var outlineFoldout = new Foldout { text = "Outline", value = true };
            StyleSectionFoldout(outlineFoldout);

            _outlineContainer = new VisualElement();
            outlineFoldout.Add(_outlineContainer);
            sidebar.Add(outlineFoldout);

            return sidebar;
        }

        private void StyleSectionFoldout(Foldout foldout)
        {
            foldout.style.marginLeft = 4;
            foldout.style.marginRight = 4;
            foldout.style.marginTop = 4;
            foldout.style.marginBottom = 4;
            var toggle = foldout.Q<Toggle>();
            if (toggle != null)
            {
                var label = toggle.Q<Label>();
                if (label != null)
                {
                    label.style.color = _theme.Heading;
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                    label.style.fontSize = 12;
                }
            }
        }

        private void StyleSubFoldout(Foldout foldout)
        {
            foldout.style.marginLeft = 4;
            foldout.style.marginTop = 2;
            foldout.style.marginBottom = 2;
            var toggle = foldout.Q<Toggle>();
            if (toggle != null)
            {
                var label = toggle.Q<Label>();
                if (label != null)
                {
                    label.style.color = _theme.TextMuted;
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                    label.style.fontSize = 11;
                }
            }
        }

        private void StyleTreeFoldout(Foldout foldout)
        {
            foldout.style.marginLeft = 0;
            foldout.style.marginTop = 0;
            foldout.style.marginBottom = 0;
            var toggle = foldout.Q<Toggle>();
            if (toggle != null)
            {
                toggle.style.borderTopLeftRadius = 3;
                toggle.style.borderTopRightRadius = 3;
                toggle.style.borderBottomLeftRadius = 3;
                toggle.style.borderBottomRightRadius = 3;
                AddHoverHighlight(toggle);

                var icon = new Image();
                icon.image = GetFolderIcon(foldout.value);
                icon.style.width = 14;
                icon.style.height = 14;
                icon.style.marginRight = 3;
                icon.style.flexShrink = 0;

                var input = toggle.Q(className: "unity-toggle__input");
                if (input != null)
                    input.Insert(1, icon);

                var capturedIcon = icon;
                var capturedFoldout = foldout;
                foldout.RegisterValueChangedCallback(_ =>
                {
                    capturedIcon.image = GetFolderIcon(capturedFoldout.value);
                });

                var label = toggle.Q<Label>();
                if (label != null)
                {
                    label.style.color = _theme.TextBody;
                    label.style.fontSize = 12;
                }
            }
        }

        private VisualElement CreateFileRow(string path, bool showUnpin)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 8;
            row.style.paddingTop = 2;
            row.style.paddingBottom = 2;
            row.style.paddingRight = 4;
            row.style.borderTopLeftRadius = 3;
            row.style.borderTopRightRadius = 3;
            row.style.borderBottomLeftRadius = 3;
            row.style.borderBottomRightRadius = 3;

            bool isCurrent = path == _filePath;
            var fileName = Path.GetFileNameWithoutExtension(path);

            var label = new Label(fileName);
            label.style.fontSize = 12;
            label.style.flexGrow = 1;
            label.style.color = isCurrent ? _theme.Heading : _theme.TextBody;
            if (isCurrent)
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.tooltip = path;

            _fileLabels.Add((path, label));

            var hoverBg = HoverBackground;
            var capturedPath = path;

            label.RegisterCallback<ClickEvent>(_ =>
            {
                if (capturedPath != _filePath)
                    OpenFile(capturedPath);
            });

            row.RegisterCallback<MouseEnterEvent>(_ =>
            {
                row.style.backgroundColor = hoverBg;
                if (capturedPath != _filePath)
                    label.style.color = _theme.Heading;
            });
            row.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                row.style.backgroundColor = StyleKeyword.Null;
                label.style.color = (capturedPath == _filePath) ? _theme.Heading : _theme.TextBody;
            });
            label.RegisterCallback<ContextClickEvent>(_ => ShowFileContextMenu(capturedPath));

            row.Add(label);

            if (showUnpin)
            {
                var unpinBtn = new Button(() => TogglePin(capturedPath)) { text = "\u2715" };
                unpinBtn.style.width = 18;
                unpinBtn.style.height = 16;
                unpinBtn.style.fontSize = 10;
                unpinBtn.style.marginLeft = 2;
                unpinBtn.style.paddingLeft = 0;
                unpinBtn.style.paddingRight = 0;
                row.Add(unpinBtn);
            }

            return row;
        }

        private static bool FileExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            var full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            return File.Exists(full);
        }

        // --- Content elements ---

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

            bool hasHeader = rows.Count > 1 && RxTableSep.IsMatch(rows[1]);

            int startIdx = 0;
            if (hasHeader)
            {
                table.Add(CreateTableRow(rows[0], true));
                startIdx = 2;
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

            if (string.IsNullOrEmpty(line) || line[0] != '!' && line[0] != '[') return false;

            var linkedMatch = RxLinkedImage.Match(line);
            if (linkedMatch.Success)
            {
                line = linkedMatch.Groups[1].Value.Trim();
            }

            var refMatch = RxRefImage.Match(line);
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

            var inlineMatch = RxInlineImage.Match(line);
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
            var matches = RxRefParse.Matches(_content);
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

            _headings.Add((level, StripMarkdownFormatting(text), wrapper));

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

            var trimmed = line.TrimStart();
            var bullet = new Label("\u2022");
            if (RxOrderedList.IsMatch(trimmed))
            {
                var match = RxOrderedListNum.Match(trimmed);
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

            string text = RxListPrefix.Replace(trimmed, "");
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

        private static string StripMarkdownFormatting(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = WebUtility.HtmlDecode(text);
            text = RxStrikethrough.Replace(text, "$1");
            text = RxBold.Replace(text, "$2");
            text = RxItalic.Replace(text, "$2");
            text = RxInlineCode.Replace(text, "$1");
            text = RxLink.Replace(text, "$1");
            text = RxStripTags.Replace(text, "");
            text = RxEscape.Replace(text, "$1");
            return text.Trim();
        }

        private string ProcessRichText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            text = WebUtility.HtmlDecode(text);
            text = text.Replace("<", "\x01").Replace(">", "\x02");

            text = RxBold.Replace(text, "<b>$2</b>");
            text = RxItalic.Replace(text, "<i>$2</i>");
            text = RxInlineCode.Replace(text, $"<color={_theme.InlineCodeColor}>$1</color>");

            text = text.Replace("\x01", "<noparse><</noparse>").Replace("\x02", "<noparse>></noparse>");

            return text;
        }
    }
}
