using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Xrcadia.GoogleDocMarkdown.Editor
{
    public class GoogleDocMarkdownWizard : EditorWindow
    {
        public static void ShowWindow()
        {
            var window = GetWindow<GoogleDocMarkdownWizard>("Google Doc Markdown");
            window.titleContent = new GUIContent("GDoc Settings", EditorGUIUtility.IconContent("Settings").image);
            window.minSize = new Vector2(500, 600);
        }

        public void CreateGUI()
        {
            var settings = GoogleDocMarkdownSettings.GetOrCreateSettings();
            var serializedSettings = new SerializedObject(settings);
            
            var view = new GoogleDocMarkdownSettingsView(serializedSettings);
            rootVisualElement.Add(view);
        }
    }
}
