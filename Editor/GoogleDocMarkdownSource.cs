using UnityEngine;

namespace Xrcadia.GoogleDocMarkdown.Editor
{
    [CreateAssetMenu(fileName = "GoogleDocMarkdownSource", menuName = "Google Doc Markdown/Source", order = 0)]
    public class GoogleDocMarkdownSource : ScriptableObject
    {
        [Tooltip("Google Doc URL or raw document ID.")]
        public string googleDocUrlOrId;

        [Tooltip("Relative to the Unity project root, e.g. Docs/Design.md or Assets/Docs/Design.md.")]
        public string outputPath = "Docs/Design.md";

        [Tooltip("When enabled, the editor will pull on startup if the minimum interval has elapsed.")]
        public bool autoPullOnEditorStartup = true;

        [Min(0)]
        [Tooltip("Minimum minutes between automatic pulls on editor startup.")]
        public int minimumMinutesBetweenAutoPulls = 60;

        [Tooltip("Last successful pull time in UTC (ISO 8601).")]
        public string lastPulledUtcIso;

        [Tooltip("Last error message, if any.")]
        public string lastError;
    }
}
