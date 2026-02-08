using UnityEditor;
using UnityEngine.UIElements;

namespace Xrcadia.GoogleDocMarkdown.Editor
{
    [CustomEditor(typeof(GoogleDocMarkdownSettings))]
    public class GoogleDocMarkdownSettingsEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return new GoogleDocMarkdownSettingsView(serializedObject);
        }
    }
}
