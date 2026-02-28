using System.Collections.Generic;
using UnityEngine;

namespace Xrcadia.GoogleDocMarkdown.Editor
{
    /// <summary>
    /// Base class for Markdown Viewer color themes. To add a new theme, create a
    /// sealed subclass with all abstract properties and register it in <see cref="All"/>.
    /// </summary>
    public abstract class MarkdownTheme
    {
        public abstract string Name { get; }
        public abstract bool IsLight { get; }
        public abstract Color Background { get; }
        public abstract Color ToolbarBackground { get; }
        public abstract Color ToolbarBorder { get; }
        public abstract Color TextBody { get; }
        public abstract Color TextMuted { get; }
        public abstract Color Heading { get; }
        public abstract Color RuleBorder { get; }
        public abstract Color CodeBackground { get; }
        public abstract Color CodeText { get; }
        public abstract Color TableBackground { get; }
        public abstract Color TableHeaderBackground { get; }
        public abstract Color BlockquoteBorder { get; }
        public abstract Color BlockquoteText { get; }
        public abstract string InlineCodeColor { get; }

        public static IReadOnlyList<MarkdownTheme> All { get; } = new MarkdownTheme[]
        {
            // Dark
            new DarkTheme(),
            new SolarizedDarkTheme(),
            new NordTheme(),
            new DraculaTheme(),
            new MonokaiTheme(),
            new OneDarkTheme(),
            new GruvboxDarkTheme(),
            new GitHubDarkTheme(),
            // Light
            new PaperwhiteTheme(),
            new SolarizedLightTheme(),
            new GitHubTheme(),
        };

        public static MarkdownTheme Default => All[0];

        public static MarkdownTheme FindByName(string name)
        {
            foreach (var theme in All)
                if (theme.Name == name)
                    return theme;
            return Default;
        }

        public override string ToString() => Name;
    }

    // -----------------------------------------------------------------------
    //  Dark  –  the original dark theme
    // -----------------------------------------------------------------------

    sealed class DarkTheme : MarkdownTheme
    {
        public override string Name => "Dark";
        public override bool IsLight => false;
        public override Color Background => new(0.16f, 0.16f, 0.17f);
        public override Color ToolbarBackground => new(0.12f, 0.12f, 0.13f);
        public override Color ToolbarBorder => new(0.08f, 0.08f, 0.08f);
        public override Color TextBody => new(0.82f, 0.82f, 0.82f);
        public override Color TextMuted => new(0.60f, 0.60f, 0.60f);
        public override Color Heading => new(0.95f, 0.95f, 0.95f);
        public override Color RuleBorder => new(0.28f, 0.28f, 0.28f);
        public override Color CodeBackground => new(0.13f, 0.13f, 0.14f);
        public override Color CodeText => new(0.80f, 0.90f, 0.80f);
        public override Color TableBackground => new(0.20f, 0.20f, 0.20f);
        public override Color TableHeaderBackground => new(0.26f, 0.26f, 0.26f);
        public override Color BlockquoteBorder => new(0.40f, 0.40f, 0.40f);
        public override Color BlockquoteText => new(0.65f, 0.65f, 0.65f);
        public override string InlineCodeColor => "#9cdcfe";
    }

    // -----------------------------------------------------------------------
    //  Paperwhite  –  warm cream e-ink inspired theme
    // -----------------------------------------------------------------------

    sealed class PaperwhiteTheme : MarkdownTheme
    {
        public override string Name => "Paperwhite";
        public override bool IsLight => true;
        public override Color Background => new(0.96f, 0.94f, 0.90f);
        public override Color ToolbarBackground => new(0.91f, 0.89f, 0.84f);
        public override Color ToolbarBorder => new(0.82f, 0.79f, 0.74f);
        public override Color TextBody => new(0.22f, 0.20f, 0.17f);
        public override Color TextMuted => new(0.42f, 0.39f, 0.35f);
        public override Color Heading => new(0.14f, 0.12f, 0.10f);
        public override Color RuleBorder => new(0.78f, 0.75f, 0.70f);
        public override Color CodeBackground => new(0.92f, 0.90f, 0.86f);
        public override Color CodeText => new(0.30f, 0.40f, 0.30f);
        public override Color TableBackground => new(0.94f, 0.92f, 0.88f);
        public override Color TableHeaderBackground => new(0.90f, 0.87f, 0.82f);
        public override Color BlockquoteBorder => new(0.72f, 0.68f, 0.60f);
        public override Color BlockquoteText => new(0.36f, 0.33f, 0.28f);
        public override string InlineCodeColor => "#5b6e5b";
    }

    // -----------------------------------------------------------------------
    //  Solarized Dark  –  Ethan Schoonover's dark palette
    // -----------------------------------------------------------------------

    sealed class SolarizedDarkTheme : MarkdownTheme
    {
        public override string Name => "Solarized Dark";
        public override bool IsLight => false;
        public override Color Background => new(0.000f, 0.169f, 0.212f);
        public override Color ToolbarBackground => new(0.027f, 0.212f, 0.259f);
        public override Color ToolbarBorder => new(0.000f, 0.130f, 0.165f);
        public override Color TextBody => new(0.514f, 0.580f, 0.588f);
        public override Color TextMuted => new(0.345f, 0.431f, 0.459f);
        public override Color Heading => new(0.576f, 0.631f, 0.631f);
        public override Color RuleBorder => new(0.345f, 0.431f, 0.459f);
        public override Color CodeBackground => new(0.027f, 0.212f, 0.259f);
        public override Color CodeText => new(0.522f, 0.600f, 0.000f);
        public override Color TableBackground => new(0.020f, 0.200f, 0.240f);
        public override Color TableHeaderBackground => new(0.050f, 0.240f, 0.290f);
        public override Color BlockquoteBorder => new(0.165f, 0.631f, 0.596f);
        public override Color BlockquoteText => new(0.345f, 0.431f, 0.459f);
        public override string InlineCodeColor => "#268bd2";
    }

    // -----------------------------------------------------------------------
    //  Solarized Light  –  Ethan Schoonover's light palette
    // -----------------------------------------------------------------------

    sealed class SolarizedLightTheme : MarkdownTheme
    {
        public override string Name => "Solarized Light";
        public override bool IsLight => true;
        public override Color Background => new(0.992f, 0.965f, 0.890f);
        public override Color ToolbarBackground => new(0.933f, 0.910f, 0.835f);
        public override Color ToolbarBorder => new(0.890f, 0.870f, 0.800f);
        public override Color TextBody => new(0.396f, 0.482f, 0.514f);
        public override Color TextMuted => new(0.576f, 0.631f, 0.631f);
        public override Color Heading => new(0.345f, 0.431f, 0.459f);
        public override Color RuleBorder => new(0.576f, 0.631f, 0.631f);
        public override Color CodeBackground => new(0.933f, 0.910f, 0.835f);
        public override Color CodeText => new(0.522f, 0.600f, 0.000f);
        public override Color TableBackground => new(0.960f, 0.935f, 0.860f);
        public override Color TableHeaderBackground => new(0.933f, 0.910f, 0.835f);
        public override Color BlockquoteBorder => new(0.165f, 0.631f, 0.596f);
        public override Color BlockquoteText => new(0.576f, 0.631f, 0.631f);
        public override string InlineCodeColor => "#268bd2";
    }

    // -----------------------------------------------------------------------
    //  Nord  –  Arctic color palette by Arctic Ice Studio
    // -----------------------------------------------------------------------

    sealed class NordTheme : MarkdownTheme
    {
        public override string Name => "Nord";
        public override bool IsLight => false;
        public override Color Background => new(0.180f, 0.204f, 0.251f);
        public override Color ToolbarBackground => new(0.231f, 0.259f, 0.322f);
        public override Color ToolbarBorder => new(0.150f, 0.170f, 0.210f);
        public override Color TextBody => new(0.847f, 0.871f, 0.914f);
        public override Color TextMuted => new(0.500f, 0.530f, 0.580f);
        public override Color Heading => new(0.925f, 0.937f, 0.957f);
        public override Color RuleBorder => new(0.298f, 0.337f, 0.416f);
        public override Color CodeBackground => new(0.231f, 0.259f, 0.322f);
        public override Color CodeText => new(0.639f, 0.745f, 0.549f);
        public override Color TableBackground => new(0.231f, 0.259f, 0.322f);
        public override Color TableHeaderBackground => new(0.263f, 0.298f, 0.369f);
        public override Color BlockquoteBorder => new(0.506f, 0.631f, 0.757f);
        public override Color BlockquoteText => new(0.700f, 0.720f, 0.760f);
        public override string InlineCodeColor => "#88c0d0";
    }

    // -----------------------------------------------------------------------
    //  Dracula  –  dark theme with purple accents
    // -----------------------------------------------------------------------

    sealed class DraculaTheme : MarkdownTheme
    {
        public override string Name => "Dracula";
        public override bool IsLight => false;
        public override Color Background => new(0.157f, 0.165f, 0.212f);
        public override Color ToolbarBackground => new(0.120f, 0.125f, 0.170f);
        public override Color ToolbarBorder => new(0.090f, 0.095f, 0.135f);
        public override Color TextBody => new(0.973f, 0.973f, 0.949f);
        public override Color TextMuted => new(0.384f, 0.447f, 0.643f);
        public override Color Heading => new(0.973f, 0.973f, 0.949f);
        public override Color RuleBorder => new(0.384f, 0.447f, 0.643f);
        public override Color CodeBackground => new(0.267f, 0.278f, 0.353f);
        public override Color CodeText => new(0.314f, 0.980f, 0.482f);
        public override Color TableBackground => new(0.240f, 0.250f, 0.320f);
        public override Color TableHeaderBackground => new(0.267f, 0.278f, 0.353f);
        public override Color BlockquoteBorder => new(0.741f, 0.576f, 0.976f);
        public override Color BlockquoteText => new(0.800f, 0.800f, 0.780f);
        public override string InlineCodeColor => "#8be9fd";
    }

    // -----------------------------------------------------------------------
    //  Monokai  –  classic Sublime Text dark theme
    // -----------------------------------------------------------------------

    sealed class MonokaiTheme : MarkdownTheme
    {
        public override string Name => "Monokai";
        public override bool IsLight => false;
        public override Color Background => new(0.153f, 0.157f, 0.133f);
        public override Color ToolbarBackground => new(0.243f, 0.239f, 0.196f);
        public override Color ToolbarBorder => new(0.118f, 0.122f, 0.110f);
        public override Color TextBody => new(0.973f, 0.973f, 0.949f);
        public override Color TextMuted => new(0.459f, 0.443f, 0.369f);
        public override Color Heading => new(0.973f, 0.973f, 0.949f);
        public override Color RuleBorder => new(0.286f, 0.282f, 0.243f);
        public override Color CodeBackground => new(0.243f, 0.239f, 0.196f);
        public override Color CodeText => new(0.651f, 0.886f, 0.180f);
        public override Color TableBackground => new(0.243f, 0.239f, 0.196f);
        public override Color TableHeaderBackground => new(0.286f, 0.282f, 0.243f);
        public override Color BlockquoteBorder => new(0.976f, 0.149f, 0.447f);
        public override Color BlockquoteText => new(0.600f, 0.590f, 0.520f);
        public override string InlineCodeColor => "#66d9ef";
    }

    // -----------------------------------------------------------------------
    //  One Dark  –  Atom editor's iconic dark theme
    // -----------------------------------------------------------------------

    sealed class OneDarkTheme : MarkdownTheme
    {
        public override string Name => "One Dark";
        public override bool IsLight => false;
        public override Color Background => new(0.157f, 0.173f, 0.204f);
        public override Color ToolbarBackground => new(0.129f, 0.145f, 0.169f);
        public override Color ToolbarBorder => new(0.094f, 0.102f, 0.122f);
        public override Color TextBody => new(0.671f, 0.698f, 0.749f);
        public override Color TextMuted => new(0.361f, 0.388f, 0.439f);
        public override Color Heading => new(0.843f, 0.855f, 0.878f);
        public override Color RuleBorder => new(0.243f, 0.267f, 0.318f);
        public override Color CodeBackground => new(0.173f, 0.192f, 0.227f);
        public override Color CodeText => new(0.596f, 0.765f, 0.475f);
        public override Color TableBackground => new(0.173f, 0.192f, 0.227f);
        public override Color TableHeaderBackground => new(0.243f, 0.267f, 0.318f);
        public override Color BlockquoteBorder => new(0.380f, 0.686f, 0.937f);
        public override Color BlockquoteText => new(0.550f, 0.570f, 0.610f);
        public override string InlineCodeColor => "#56b6c2";
    }

    // -----------------------------------------------------------------------
    //  Gruvbox Dark  –  retro groove color scheme
    // -----------------------------------------------------------------------

    sealed class GruvboxDarkTheme : MarkdownTheme
    {
        public override string Name => "Gruvbox Dark";
        public override bool IsLight => false;
        public override Color Background => new(0.157f, 0.157f, 0.157f);
        public override Color ToolbarBackground => new(0.235f, 0.220f, 0.212f);
        public override Color ToolbarBorder => new(0.120f, 0.120f, 0.120f);
        public override Color TextBody => new(0.835f, 0.769f, 0.631f);
        public override Color TextMuted => new(0.573f, 0.514f, 0.455f);
        public override Color Heading => new(0.922f, 0.859f, 0.698f);
        public override Color RuleBorder => new(0.314f, 0.286f, 0.271f);
        public override Color CodeBackground => new(0.235f, 0.220f, 0.212f);
        public override Color CodeText => new(0.557f, 0.753f, 0.486f);
        public override Color TableBackground => new(0.235f, 0.220f, 0.212f);
        public override Color TableHeaderBackground => new(0.314f, 0.286f, 0.271f);
        public override Color BlockquoteBorder => new(0.996f, 0.502f, 0.098f);
        public override Color BlockquoteText => new(0.700f, 0.650f, 0.550f);
        public override string InlineCodeColor => "#83a598";
    }

    // -----------------------------------------------------------------------
    //  GitHub  –  clean white theme matching GitHub's markdown rendering
    // -----------------------------------------------------------------------

    sealed class GitHubTheme : MarkdownTheme
    {
        public override string Name => "GitHub";
        public override bool IsLight => true;
        public override Color Background => new(1.000f, 1.000f, 1.000f);
        public override Color ToolbarBackground => new(0.965f, 0.973f, 0.980f);
        public override Color ToolbarBorder => new(0.820f, 0.851f, 0.878f);
        public override Color TextBody => new(0.122f, 0.137f, 0.157f);
        public override Color TextMuted => new(0.396f, 0.427f, 0.463f);
        public override Color Heading => new(0.122f, 0.137f, 0.157f);
        public override Color RuleBorder => new(0.820f, 0.851f, 0.878f);
        public override Color CodeBackground => new(0.937f, 0.945f, 0.953f);
        public override Color CodeText => new(0.122f, 0.137f, 0.157f);
        public override Color TableBackground => new(1.000f, 1.000f, 1.000f);
        public override Color TableHeaderBackground => new(0.965f, 0.973f, 0.980f);
        public override Color BlockquoteBorder => new(0.820f, 0.851f, 0.878f);
        public override Color BlockquoteText => new(0.396f, 0.427f, 0.463f);
        public override string InlineCodeColor => "#0550ae";
    }

    // -----------------------------------------------------------------------
    //  GitHub Dark  –  GitHub's dark mode markdown rendering
    // -----------------------------------------------------------------------

    sealed class GitHubDarkTheme : MarkdownTheme
    {
        public override string Name => "GitHub Dark";
        public override bool IsLight => false;
        public override Color Background => new(0.051f, 0.067f, 0.090f);
        public override Color ToolbarBackground => new(0.086f, 0.106f, 0.133f);
        public override Color ToolbarBorder => new(0.188f, 0.212f, 0.239f);
        public override Color TextBody => new(0.902f, 0.929f, 0.953f);
        public override Color TextMuted => new(0.545f, 0.580f, 0.620f);
        public override Color Heading => new(0.902f, 0.929f, 0.953f);
        public override Color RuleBorder => new(0.188f, 0.212f, 0.239f);
        public override Color CodeBackground => new(0.086f, 0.106f, 0.133f);
        public override Color CodeText => new(0.902f, 0.929f, 0.953f);
        public override Color TableBackground => new(0.051f, 0.067f, 0.090f);
        public override Color TableHeaderBackground => new(0.086f, 0.106f, 0.133f);
        public override Color BlockquoteBorder => new(0.188f, 0.212f, 0.239f);
        public override Color BlockquoteText => new(0.545f, 0.580f, 0.620f);
        public override string InlineCodeColor => "#79c0ff";
    }
}
