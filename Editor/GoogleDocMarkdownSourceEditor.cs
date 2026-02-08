using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Xrcadia.GoogleDocMarkdown.Editor
{
    [CustomEditor(typeof(GoogleDocMarkdownSource))]
    public class GoogleDocMarkdownSourceEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var helpBox = new HelpBox(
                "Google Docs must be public to export Markdown unless OAuth is added.",
                HelpBoxMessageType.Info);
            root.Add(helpBox);

            var googleDocProp = serializedObject.FindProperty("googleDocUrlOrId");
            var outputPathProp = serializedObject.FindProperty("outputPath");
            var autoPullProp = serializedObject.FindProperty("autoPullOnEditorStartup");
            var minMinutesProp = serializedObject.FindProperty("minimumMinutesBetweenAutoPulls");
            var lastPulledProp = serializedObject.FindProperty("lastPulledUtcIso");
            var lastErrorProp = serializedObject.FindProperty("lastError");

            root.Add(new PropertyField(googleDocProp, "Google Doc URL or ID"));
            root.Add(new PropertyField(outputPathProp, "Output Path"));
            root.Add(new PropertyField(autoPullProp, "Auto Pull On Editor Startup"));
            root.Add(new PropertyField(minMinutesProp, "Min Minutes Between Auto Pulls"));

            var pullButton = new Button(() =>
            {
                var source = (GoogleDocMarkdownSource)target;
                GoogleDocMarkdownPuller.PullSource(source);
            })
            {
                text = "Pull Now"
            };
            root.Add(pullButton);

            var lastPulledField = new PropertyField(lastPulledProp, "Last Pulled (UTC)");
            lastPulledField.SetEnabled(false);
            root.Add(lastPulledField);

            var lastErrorField = new PropertyField(lastErrorProp, "Last Error");
            lastErrorField.SetEnabled(false);
            root.Add(lastErrorField);

            root.Bind(serializedObject);
            return root;
        }
    }
}
