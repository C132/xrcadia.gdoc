using UnityEditor;

namespace Xrcadia.GoogleDocMarkdown.Editor
{
    [InitializeOnLoad]
    internal static class GoogleDocMarkdownAutoPull
    {
        static GoogleDocMarkdownAutoPull()
        {
            EditorApplication.delayCall += RunAutoPull;
        }

        private static void RunAutoPull()
        {
            GoogleDocMarkdownPuller.PullAllSources(true);
        }
    }
}
