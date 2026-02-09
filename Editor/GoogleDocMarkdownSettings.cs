using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor;

namespace Xrcadia.GoogleDocMarkdown.Editor
{
    public class GoogleDocMarkdownSettings : ScriptableObject
    {
        [Serializable]
        public class SourceConfig
        {
            public string name;
            public string googleDocUrlOrId;
            public string lastPulledUtcIso;
            public string lastError;
        }

        public bool autoPullOnEditorStartup = true;
        public int minimumMinutesBetweenAutoPulls = 60;
        public string outputPath = "Assets/Documentation";
        public List<SourceConfig> sources = new List<SourceConfig>();

        public static GoogleDocMarkdownSettings GetOrCreateSettings()
        {
            var guids = AssetDatabase.FindAssets("t:GoogleDocMarkdownSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<GoogleDocMarkdownSettings>(path);
            }

            // Default location
            var defaultPath = "Assets/GoogleDocMarkdownSettings.asset";
            var settings = CreateInstance<GoogleDocMarkdownSettings>();
            AssetDatabase.CreateAsset(settings, defaultPath);
            AssetDatabase.SaveAssets();
            return settings;
        }
    }
}
