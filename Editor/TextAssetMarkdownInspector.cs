using UnityEditor;
using UnityEngine;

namespace Xrcadia.GoogleDocMarkdown.Editor
{
    [CustomEditor(typeof(TextAsset))]
    public class TextAssetMarkdownInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var path = AssetDatabase.GetAssetPath(target);
            if (path.EndsWith(".md", System.StringComparison.OrdinalIgnoreCase))
            {
                GUI.enabled = true;
                EditorGUILayout.Space();
                if (GUILayout.Button("View as Markdown"))
                    GoogleDocMarkdownViewer.ShowWindow(path);
            }
        }
    }
}
