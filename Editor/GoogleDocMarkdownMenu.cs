using UnityEditor;

namespace Xrcadia.GoogleDocMarkdown.Editor
{
    internal static class GoogleDocMarkdownMenu
    {
        [MenuItem("xrcadia/Google Doc Markdown/Settings", priority = 0)]
        private static void ShowWizard()
        {
            GoogleDocMarkdownWizard.ShowWindow();
        }

        [MenuItem("xrcadia/Google Doc Markdown/Pull All Sources", priority = 2000)]
        private static void PullAllSources()
        {
            GoogleDocMarkdownPuller.PullAllSources(false);
        }
    }
}
