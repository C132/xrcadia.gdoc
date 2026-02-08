using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Xrcadia.GoogleDocMarkdown.Editor
{
    public class GoogleDocMarkdownSettingsView : VisualElement
    {
        private GoogleDocMarkdownSettings _settings;
        private SerializedObject _serializedSettings;
        private VisualElement _sourcesContainer;
        private int _lastSourceCount = -1;

        public GoogleDocMarkdownSettingsView(SerializedObject serializedSettings)
        {
            _serializedSettings = serializedSettings;
            _settings = (GoogleDocMarkdownSettings)serializedSettings.targetObject;

            style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            style.flexGrow = 1;

            // Banner
            var banner = new VisualElement();
            banner.style.height = 100;
            banner.style.backgroundColor = new Color(0.15f, 0.4f, 0.65f);
            banner.style.justifyContent = Justify.Center;
            banner.style.paddingLeft = 25;
            banner.style.marginBottom = 10;
            banner.style.borderBottomWidth = 2;
            banner.style.borderBottomColor = new Color(0.1f, 0.3f, 0.5f);
            
            var title = new Label("Google Doc Markdown");
            title.style.fontSize = 28;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            banner.Add(title);

            var subtitle = new Label("Documentation Workflow for Unity & AI Agents");
            subtitle.style.fontSize = 13;
            subtitle.style.color = new Color(0.85f, 0.85f, 0.85f);
            subtitle.style.marginTop = 2;
            banner.Add(subtitle);
            
            Add(banner);

            var mainScroll = new ScrollView();
            mainScroll.style.paddingLeft = 15;
            mainScroll.style.paddingRight = 15;
            Add(mainScroll);

            // Help Box
            var helpBox = new HelpBox("Automate your documentation by pulling Google Docs as Markdown. Ensure your documents are shared as 'Anyone with the link can view'.", HelpBoxMessageType.Info);
            helpBox.style.marginBottom = 20;
            mainScroll.Add(helpBox);

            // Global Settings
            var settingsGroup = CreateGroup("Configuration");
            var autoPullField = new PropertyField(_serializedSettings.FindProperty("autoPullOnEditorStartup"), "Auto Pull on Startup");
            autoPullField.tooltip = "If enabled, the tool will check all sources on editor startup.";
            
            var minMinutesField = new PropertyField(_serializedSettings.FindProperty("minimumMinutesBetweenAutoPulls"), "Interval (minutes)");
            minMinutesField.tooltip = "Minimum time between automatic pulls to avoid excessive network requests.";
            
            settingsGroup.Add(autoPullField);
            settingsGroup.Add(minMinutesField);
            mainScroll.Add(settingsGroup);

            // Sources Section Header
            var sourcesHeader = new VisualElement();
            sourcesHeader.style.flexDirection = FlexDirection.Row;
            sourcesHeader.style.justifyContent = Justify.SpaceBetween;
            sourcesHeader.style.alignItems = Align.Center;
            sourcesHeader.style.marginTop = 10;
            sourcesHeader.style.marginBottom = 8;

            var sourcesLabel = new Label("Document Sources");
            sourcesLabel.style.fontSize = 18;
            sourcesLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            sourcesLabel.style.color = new Color(0.9f, 0.9f, 0.9f);
            sourcesHeader.Add(sourcesLabel);

            var addButton = new Button(AddSource) { text = " + Add New Source " };
            addButton.style.height = 25;
            addButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            sourcesHeader.Add(addButton);
            mainScroll.Add(sourcesHeader);

            // Sources List Container
            _sourcesContainer = new VisualElement();
            mainScroll.Add(_sourcesContainer);

            _lastSourceCount = _serializedSettings.FindProperty("sources").arraySize;
            RefreshSources();

            // Footer / Actions
            var footer = new VisualElement();
            footer.style.paddingTop = 20;
            footer.style.paddingBottom = 20;
            footer.style.marginTop = 10;
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.FlexEnd;

            var pullAllButton = new Button(() => {
                GoogleDocMarkdownPuller.PullAllSources(false);
                RefreshSources();
            })
            {
                text = "Pull All Sources Now",
                style = { 
                    width = 180, 
                    height = 35, 
                    backgroundColor = new Color(0.22f, 0.55f, 0.22f),
                    fontSize = 13,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            pullAllButton.style.color = Color.white;
            footer.Add(pullAllButton);
            mainScroll.Add(footer);

            this.Bind(_serializedSettings);
            this.schedule.Execute(OnUpdate).Every(100);
        }

        private void OnUpdate()
        {
            if (_serializedSettings == null || _serializedSettings.targetObject == null) return;

            if (_serializedSettings.UpdateIfRequiredOrScript())
            {
                var sourcesProp = _serializedSettings.FindProperty("sources");
                if (sourcesProp.arraySize != _lastSourceCount)
                {
                    _lastSourceCount = sourcesProp.arraySize;
                    RefreshSources();
                }
            }
        }

        private VisualElement CreateGroup(string title)
        {
            var group = new VisualElement();
            group.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            group.style.paddingLeft = 12;
            group.style.paddingRight = 12;
            group.style.paddingTop = 10;
            group.style.paddingBottom = 12;
            group.style.borderBottomLeftRadius = 6;
            group.style.borderBottomRightRadius = 6;
            group.style.borderTopLeftRadius = 6;
            group.style.borderTopRightRadius = 6;
            group.style.marginBottom = 15;
            group.style.borderTopWidth = 1;
            group.style.borderBottomWidth = 1;
            group.style.borderLeftWidth = 1;
            group.style.borderRightWidth = 1;
            group.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            group.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            group.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            group.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);

            if (!string.IsNullOrEmpty(title))
            {
                var label = new Label(title);
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.marginBottom = 8;
                label.style.fontSize = 14;
                label.style.color = new Color(0.8f, 0.8f, 0.8f);
                group.Add(label);
            }

            return group;
        }

        private void RefreshSources()
        {
            if (_sourcesContainer == null) return;
            
            _sourcesContainer.Clear();
            _serializedSettings.Update();
            var sourcesProp = _serializedSettings.FindProperty("sources");
            
            for (int i = 0; i < sourcesProp.arraySize; i++)
            {
                var index = i;
                var sourceProp = sourcesProp.GetArrayElementAtIndex(i);
                _sourcesContainer.Add(CreateSourceItem(sourceProp, index));
            }
            
            if (sourcesProp.arraySize == 0)
            {
                var emptyContainer = new VisualElement();
                emptyContainer.style.paddingTop = 40;
                emptyContainer.style.paddingBottom = 40;
                emptyContainer.style.alignItems = Align.Center;
                
                var emptyLabel = new Label("No documents configured yet. Click '+ Add New Source' to begin.");
                emptyLabel.style.fontSize = 14;
                emptyLabel.style.opacity = 0.5f;
                emptyContainer.Add(emptyLabel);
                
                _sourcesContainer.Add(emptyContainer);
            }

            _sourcesContainer.Bind(_serializedSettings);
        }

        private VisualElement CreateSourceItem(SerializedProperty property, int index)
        {
            var item = new VisualElement();
            item.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            item.style.paddingLeft = 12;
            item.style.paddingRight = 12;
            item.style.paddingTop = 10;
            item.style.paddingBottom = 8;
            item.style.marginBottom = 12;
            item.style.borderBottomLeftRadius = 6;
            item.style.borderBottomRightRadius = 6;
            item.style.borderTopLeftRadius = 6;
            item.style.borderTopRightRadius = 6;
            item.style.borderTopWidth = 1;
            item.style.borderBottomWidth = 1;
            item.style.borderLeftWidth = 1;
            item.style.borderRightWidth = 1;
            item.style.borderTopColor = new Color(0.35f, 0.35f, 0.35f);
            item.style.borderBottomColor = new Color(0.35f, 0.35f, 0.35f);
            item.style.borderLeftColor = new Color(0.35f, 0.35f, 0.35f);
            item.style.borderRightColor = new Color(0.35f, 0.35f, 0.35f);

            // Header Row: Name and Actions
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.marginBottom = 8;
            headerRow.style.alignItems = Align.Center;

            var nameProp = property.FindPropertyRelative("name");
            var nameField = new PropertyField(nameProp, "");
            nameField.style.flexGrow = 1;
            nameField.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameField.tooltip = "Enter a name for this document";
            headerRow.Add(nameField);

            var actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;
            
            var pullBtn = new Button(() => GoogleDocMarkdownPuller.PullSource(_settings.sources[index])) { 
                text = "Pull", 
                tooltip = "Pull this document now" 
            };
            pullBtn.style.height = 20;
            
            var removeBtn = new Button(() => RemoveSource(index)) { 
                text = "✕", 
                tooltip = "Remove this source" 
            };
            removeBtn.style.height = 20;
            removeBtn.style.marginLeft = 4;
            removeBtn.style.color = new Color(0.9f, 0.3f, 0.3f);
            
            actions.Add(pullBtn);
            actions.Add(removeBtn);
            headerRow.Add(actions);
            item.Add(headerRow);

            // Content Area (URL and Path)
            var contentArea = new VisualElement();
            contentArea.style.marginBottom = 4;

            var urlField = new PropertyField(property.FindPropertyRelative("googleDocUrlOrId"), "Doc URL / ID");
            urlField.tooltip = "Enter the full Google Doc URL or just the Document ID. Make sure it is shared as 'Anyone with the link can view'.";
            contentArea.Add(urlField);
            
            var pathField = new PropertyField(property.FindPropertyRelative("outputPath"), "Output Path");
            pathField.tooltip = "Where to save the markdown file relative to the project root (e.g. Assets/Documentation/Doc.md)";
            contentArea.Add(pathField);
            
            item.Add(contentArea);

            // Status Bar
            var statusBar = new VisualElement();
            statusBar.style.flexDirection = FlexDirection.Row;
            statusBar.style.marginTop = 6;
            statusBar.style.paddingTop = 4;
            statusBar.style.borderTopWidth = 1;
            statusBar.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            statusBar.style.justifyContent = Justify.SpaceBetween;

            // Time Label with Auto-update
            var timeLabel = new Label();
            timeLabel.style.fontSize = 10;
            timeLabel.style.opacity = 0.6f;
            var lastPulledProp = property.FindPropertyRelative("lastPulledUtcIso");
            void UpdateTime(SerializedProperty p) => timeLabel.text = string.IsNullOrEmpty(p.stringValue) ? "Never pulled" : $"Last sync: {p.stringValue}";
            UpdateTime(lastPulledProp);
            timeLabel.TrackPropertyValue(lastPulledProp, UpdateTime);
            statusBar.Add(timeLabel);

            // Error Label with Auto-update
            var errorLabel = new Label();
            errorLabel.style.color = new Color(1f, 0.4f, 0.4f);
            errorLabel.style.fontSize = 10;
            errorLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            var lastErrorProp = property.FindPropertyRelative("lastError");
            void UpdateError(SerializedProperty p) {
                errorLabel.text = string.IsNullOrEmpty(p.stringValue) ? "" : "⚠️ Error";
                errorLabel.tooltip = p.stringValue;
            }
            UpdateError(lastErrorProp);
            errorLabel.TrackPropertyValue(lastErrorProp, UpdateError);
            statusBar.Add(errorLabel);

            item.Add(statusBar);

            return item;
        }

        private void AddSource()
        {
            _serializedSettings.Update();
            var sourcesProp = _serializedSettings.FindProperty("sources");
            sourcesProp.InsertArrayElementAtIndex(sourcesProp.arraySize);
            
            var newSource = sourcesProp.GetArrayElementAtIndex(sourcesProp.arraySize - 1);
            newSource.FindPropertyRelative("name").stringValue = "New Document";
            newSource.FindPropertyRelative("outputPath").stringValue = "Assets/Documentation/Design.md";
            newSource.FindPropertyRelative("googleDocUrlOrId").stringValue = "";
            newSource.FindPropertyRelative("lastPulledUtcIso").stringValue = "";
            newSource.FindPropertyRelative("lastError").stringValue = "";
            
            _serializedSettings.ApplyModifiedProperties();
            RefreshSources();
        }

        private void RemoveSource(int index)
        {
            if (EditorUtility.DisplayDialog("Remove Source", "Are you sure you want to remove this document source configuration?", "Remove", "Cancel"))
            {
                _serializedSettings.Update();
                _serializedSettings.FindProperty("sources").DeleteArrayElementAtIndex(index);
                _serializedSettings.ApplyModifiedProperties();
                RefreshSources();
            }
        }
    }
}
