using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;

namespace Xrcadia.GoogleDocMarkdown.Editor
{
    public class GoogleDocMarkdownWizard : EditorWindow
    {
        private ScrollView _scrollView;

        public static void ShowWindow()
        {
            var window = GetWindow<GoogleDocMarkdownWizard>("Google Doc Markdown");
            window.minSize = new Vector2(450, 550);
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;

            // Header
            var header = new Label("Google Doc Markdown");
            header.style.fontSize = 20;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 10;
            root.Add(header);

            // Description
            var description = new Label("This package allows you to pull public Google Docs as Markdown files directly into your Unity project.");
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.marginBottom = 10;
            root.Add(description);

            // HelpBox
            var helpBox = new HelpBox("Google Docs must be public ('Anyone with the link can view') for this to work.", HelpBoxMessageType.Info);
            helpBox.style.marginBottom = 15;
            root.Add(helpBox);

            // Steps section
            var stepsTitle = new Label("Getting Started");
            stepsTitle.style.fontSize = 16;
            stepsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            stepsTitle.style.marginBottom = 5;
            root.Add(stepsTitle);

            var stepsContainer = new VisualElement();
            stepsContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.1f);
            stepsContainer.style.paddingLeft = 10;
            stepsContainer.style.paddingRight = 10;
            stepsContainer.style.paddingTop = 10;
            stepsContainer.style.paddingBottom = 10;
            stepsContainer.style.borderBottomLeftRadius = 5;
            stepsContainer.style.borderBottomRightRadius = 5;
            stepsContainer.style.borderTopLeftRadius = 5;
            stepsContainer.style.borderTopRightRadius = 5;
            stepsContainer.style.marginBottom = 15;

            stepsContainer.Add(new Label("1. Create a Source asset using the button below."));
            stepsContainer.Add(new Label("2. Open the asset and paste your Google Doc URL."));
            stepsContainer.Add(new Label("3. Set the output path (e.g., Assets/Docs/Design.md)."));
            stepsContainer.Add(new Label("4. Click 'Pull Now' or use the 'Pull All' button."));
            
            var createButton = new Button(CreateNewSource)
            {
                text = "Create New Source Asset",
                style = { marginTop = 10, height = 30 }
            };
            stepsContainer.Add(createButton);
            root.Add(stepsContainer);

            // Active Sources section
            var sourcesTitle = new Label("Active Sources");
            sourcesTitle.style.fontSize = 16;
            sourcesTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            sourcesTitle.style.marginBottom = 5;
            root.Add(sourcesTitle);

            _scrollView = new ScrollView();
            _scrollView.style.flexGrow = 1;
            _scrollView.style.marginBottom = 10;
            root.Add(_scrollView);

            RefreshSourceList();

            // Footer
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.FlexEnd;
            
            var pullAllButton = new Button(() => {
                GoogleDocMarkdownPuller.PullAllSources(false);
                RefreshSourceList();
            })
            {
                text = "Pull All Sources",
                style = { width = 150, height = 30 }
            };
            footer.Add(pullAllButton);
            root.Add(footer);
        }

        private void OnEnable()
        {
            EditorApplication.projectChanged += RefreshSourceList;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= RefreshSourceList;
        }

        private void OnFocus()
        {
            RefreshSourceList();
        }

        private void RefreshSourceList()
        {
            if (_scrollView == null) return;
            
            _scrollView.Clear();
            var sources = GoogleDocMarkdownPuller.FindSources();

            if (sources.Count == 0)
            {
                _scrollView.Add(new Label("No sources found. Create one to get started."));
                return;
            }

            foreach (var source in sources)
            {
                if (source == null) continue;

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.paddingLeft = 5;
                row.style.paddingRight = 5;
                row.style.paddingTop = 5;
                row.style.paddingBottom = 5;
                row.style.marginBottom = 2;
                row.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.2f);
                row.style.alignItems = Align.Center;

                var nameLabel = new Label(source.name);
                nameLabel.style.flexGrow = 1;
                nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                row.Add(nameLabel);

                var pathLabel = new Label(source.outputPath);
                pathLabel.style.width = 150;
                pathLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                pathLabel.style.marginRight = 10;
                row.Add(pathLabel);

                var pullButton = new Button(() => {
                    GoogleDocMarkdownPuller.PullSource(source);
                })
                {
                    text = "Pull"
                };
                row.Add(pullButton);

                var selectButton = new Button(() => {
                    Selection.activeObject = source;
                })
                {
                    text = "Select"
                };
                row.Add(selectButton);

                _scrollView.Add(row);
            }
        }

        private void CreateNewSource()
        {
            var path = "Assets/GoogleDocMarkdownSource.asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            
            var asset = ScriptableObject.CreateInstance<GoogleDocMarkdownSource>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            RefreshSourceList();
        }
    }
}
