using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Xrcadia.GoogleDocMarkdown.Editor
{
    internal static class GoogleDocMarkdownPuller
    {
        private const string ProgressTitle = "Google Doc Markdown";
        private static readonly Queue<PullJob> PendingJobs = new Queue<PullJob>();
        private static PullJob activeJob;

        public static void PullAllSources(bool onlyAutoPullEnabled)
        {
            var settings = GoogleDocMarkdownSettings.GetOrCreateSettings();
            var sources = settings.sources;
            if (sources == null || sources.Count == 0)
            {
                return;
            }

            var nowUtc = DateTime.UtcNow;
            var toPull = new List<GoogleDocMarkdownSettings.SourceConfig>();
            foreach (var source in sources)
            {
                if (source == null)
                {
                    continue;
                }

                if (!onlyAutoPullEnabled || ShouldAutoPull(source, nowUtc, settings))
                {
                    toPull.Add(source);
                }
            }

            if (toPull.Count == 0)
            {
                return;
            }

            QueueJobs(toPull, settings);
        }

        public static void PullSource(GoogleDocMarkdownSettings.SourceConfig config)
        {
            if (config == null)
            {
                return;
            }

            var settings = GoogleDocMarkdownSettings.GetOrCreateSettings();
            QueueJobs(new List<GoogleDocMarkdownSettings.SourceConfig> { config }, settings);
        }

        private static void QueueJobs(IReadOnlyList<GoogleDocMarkdownSettings.SourceConfig> configs, GoogleDocMarkdownSettings settings)
        {
            var total = configs.Count;
            for (int i = 0; i < total; i++)
            {
                var labelSuffix = total > 1 ? $" ({i + 1}/{total})" : string.Empty;
                PendingJobs.Enqueue(new PullJob(configs[i], settings, labelSuffix));
            }

            StartQueue();
        }

        private static void StartQueue()
        {
            if (activeJob != null)
            {
                return;
            }

            StartNextJob();
        }

        private static void StartNextJob()
        {
            while (PendingJobs.Count > 0)
            {
                var job = PendingJobs.Dequeue();
                if (job.Config == null)
                {
                    continue;
                }

                SetStatus(job.Config, job.Settings, string.Empty, false);
                if (!job.TryStart(out var error))
                {
                    SetStatus(job.Config, job.Settings, error, false);
                    Debug.LogError(error, job.Settings);
                    continue;
                }

                activeJob = job;
                EditorApplication.update -= Update;
                EditorApplication.update += Update;
                return;
            }

            activeJob = null;
            EditorApplication.update -= Update;
            EditorUtility.ClearProgressBar();
        }

        private static void Update()
        {
            if (activeJob == null)
            {
                EditorApplication.update -= Update;
                EditorUtility.ClearProgressBar();
                return;
            }

            if (activeJob.Config == null)
            {
                FinalizeJob(activeJob);
                return;
            }

            if (activeJob.Operation == null)
            {
                HandleFailure(activeJob, "Download operation could not be started.");
                FinalizeJob(activeJob);
                return;
            }

            var label = string.IsNullOrEmpty(activeJob.Config.name) ? "Unnamed Source" : activeJob.Config.name;
            EditorUtility.DisplayProgressBar(
                ProgressTitle,
                $"Downloading {label}{activeJob.LabelSuffix}",
                activeJob.Operation.progress);

            if (!activeJob.Operation.isDone)
            {
                return;
            }

            CompleteJob(activeJob);
            FinalizeJob(activeJob);
        }

        private static void CompleteJob(PullJob job)
        {
            try
            {
                if (job.Config == null)
                {
                    return;
                }

                if (job.Request.result != UnityWebRequest.Result.Success)
                {
                    var message = $"Download failed ({job.Request.responseCode}): {job.Request.error}";
                    HandleFailure(job, message);
                    return;
                }

                var markdown = NormalizeMarkdown(job.Request.downloadHandler.text);

                // Attempt to get filename from headers if we are using the default name
                var headerFilename = GetFilenameFromHeaders(job.Request);
                if (!string.IsNullOrEmpty(headerFilename))
                {
                    if (!headerFilename.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    {
                        headerFilename += ".md";
                    }

                    var currentPath = job.Config.outputPath.Replace('\\', '/');
                    var isDefault = currentPath == "Docs/Design.md" || 
                                   currentPath == "Assets/Documentation/Design.md" || 
                                   currentPath.EndsWith("/");

                    if (isDefault)
                    {
                        var dir = Path.GetDirectoryName(currentPath);
                        var newOutputPath = Path.Combine(dir ?? string.Empty, headerFilename).Replace('\\', '/');
                        
                        if (job.Config.outputPath != newOutputPath)
                        {
                            job.Config.outputPath = newOutputPath;
                            job.FullPath = Path.GetFullPath(Path.Combine(GetProjectRoot(), newOutputPath));
                        }
                    }
                }

                markdown = ProcessImages(markdown, job.FullPath);
                
                var directory = Path.GetDirectoryName(job.FullPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var label = string.IsNullOrEmpty(job.Config.name) ? "Unnamed Source" : job.Config.name;
                EditorUtility.DisplayProgressBar(
                    ProgressTitle,
                    $"Saving {label}{job.LabelSuffix}",
                    1f);

                File.WriteAllText(job.FullPath, markdown, new UTF8Encoding(false));
                AssetDatabase.Refresh();

                SetStatus(job.Config, job.Settings, string.Empty, true);
                Debug.Log($"Google Doc Markdown pulled to {job.Config.outputPath}", job.Settings);
            }
            catch (Exception ex)
            {
                HandleFailure(job, $"Pull failed: {ex.Message}");
            }
        }

        private static void HandleFailure(PullJob job, string message)
        {
            if (job.Config != null)
            {
                SetStatus(job.Config, job.Settings, message, false);
                Debug.LogError(message, job.Settings);
            }
        }

        private static void FinalizeJob(PullJob job)
        {
            job.Dispose();
            activeJob = null;
            EditorUtility.ClearProgressBar();
            StartNextJob();
        }

        private static void SetStatus(GoogleDocMarkdownSettings.SourceConfig config, GoogleDocMarkdownSettings settings, string error, bool success)
        {
            config.lastError = error ?? string.Empty;
            if (success)
            {
                config.lastPulledUtcIso = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static bool ShouldAutoPull(GoogleDocMarkdownSettings.SourceConfig config, DateTime nowUtc, GoogleDocMarkdownSettings settings)
        {
            if (!settings.autoPullOnEditorStartup)
            {
                return false;
            }

            var minMinutes = Math.Max(0, settings.minimumMinutesBetweenAutoPulls);
            if (minMinutes == 0 || string.IsNullOrWhiteSpace(config.lastPulledUtcIso))
            {
                return true;
            }

            if (!DateTime.TryParse(
                    config.lastPulledUtcIso,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var lastPulledUtc))
            {
                return true;
            }

            return (nowUtc - lastPulledUtc).TotalMinutes >= minMinutes;
        }

        private static bool TryGetDocumentId(string input, out string docId, out string error)
        {
            docId = string.Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                error = "Google Doc URL or ID is empty.";
                return false;
            }

            var trimmed = input.Trim();
            var docHostIndex = trimmed.IndexOf("docs.google.com", StringComparison.OrdinalIgnoreCase);
            var markerIndex = trimmed.IndexOf("/d/", StringComparison.OrdinalIgnoreCase);

            if (docHostIndex >= 0)
            {
                if (markerIndex < 0)
                {
                    error = "Could not locate /d/<id> in the Google Doc URL.";
                    return false;
                }

                var start = markerIndex + 3;
                var end = trimmed.IndexOf('/', start);
                if (end < 0)
                {
                    end = trimmed.IndexOf('?', start);
                }

                if (end < 0)
                {
                    end = trimmed.Length;
                }

                if (end > start)
                {
                    docId = trimmed.Substring(start, end - start);
                }
            }
            else
            {
                docId = trimmed;
            }

            if (string.IsNullOrWhiteSpace(docId))
            {
                error = "Document ID is empty.";
                return false;
            }

            return true;
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string ProcessImages(string markdown, string markdownFilePath)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return markdown;
            }

            string fileName = Path.GetFileNameWithoutExtension(markdownFilePath);
            string directory = Path.GetDirectoryName(markdownFilePath);
            if (string.IsNullOrEmpty(directory))
            {
                return markdown;
            }

            string imagesFolderRelative = fileName + "_images";
            string imagesFolderPath = Path.Combine(directory, imagesFolderRelative);

            // Match reference-style base64 images: [image1]: <data:image/png;base64,...>
            // We use multiline to match ^ at the start of each line (allowing optional indentation).
            var regex = new Regex(@"^\s*\[([^\]]+)\]:\s*<?data:image\/(png|jpeg|gif|webp|svg\+xml);base64,([^>\s]+)>?",
                RegexOptions.Multiline);

            var matches = regex.Matches(markdown);
            if (matches.Count == 0)
            {
                return markdown;
            }

            try
            {
                if (!Directory.Exists(imagesFolderPath))
                {
                    Directory.CreateDirectory(imagesFolderPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create images directory '{imagesFolderPath}': {ex.Message}");
                return markdown;
            }

            return regex.Replace(markdown, m =>
            {
                string imageId = m.Groups[1].Value;
                string extension = m.Groups[2].Value;
                string base64Data = m.Groups[3].Value;

                // Map svg+xml to svg
                if (extension == "svg+xml")
                {
                    extension = "svg";
                }

                string imageFileName = $"{imageId}.{extension}";
                string imagePath = Path.Combine(imagesFolderPath, imageFileName);
                // Use forward slashes for Markdown paths regardless of OS
                string relativeImagePath = $"{imagesFolderRelative}/{imageFileName}";

                try
                {
                    byte[] imageBytes = Convert.FromBase64String(base64Data);
                    File.WriteAllBytes(imagePath, imageBytes);
                    return $"[{imageId}]: {relativeImagePath}";
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to process image '{imageId}': {ex.Message}");
                    return m.Value; // Keep original if failed
                }
            });
        }

        private static string NormalizeMarkdown(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            var normalized = input.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = normalized.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].TrimEnd();
            }

            return string.Join("\n", lines);
        }

        private static string GetFilenameFromHeaders(UnityWebRequest request)
        {
            var cd = request.GetResponseHeader("Content-Disposition");
            if (string.IsNullOrEmpty(cd)) return null;

            // Try to match filename*=UTF-8''... (RFC 5987)
            var matchUtf8 = Regex.Match(cd, @"filename\*=UTF-8''([^;\n]+)", RegexOptions.IgnoreCase);
            if (matchUtf8.Success)
            {
                var fileName = UnityWebRequest.UnEscapeURL(matchUtf8.Groups[1].Value);
                return SanitizeFilename(fileName);
            }

            // Try to match filename="..."
            var match = Regex.Match(cd, @"filename=[""']?([^;""'\n]+)[""']?", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return SanitizeFilename(match.Groups[1].Value);
            }

            return null;
        }

        private static string SanitizeFilename(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return fileName;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }


        private sealed class PullJob
        {
            public PullJob(GoogleDocMarkdownSettings.SourceConfig config, GoogleDocMarkdownSettings settings, string labelSuffix)
            {
                Config = config;
                Settings = settings;
                LabelSuffix = labelSuffix;
            }

            public GoogleDocMarkdownSettings.SourceConfig Config { get; }
            public GoogleDocMarkdownSettings Settings { get; }
            public string LabelSuffix { get; }
            public UnityWebRequest Request { get; private set; }
            public UnityWebRequestAsyncOperation Operation { get; private set; }
            public string FullPath { get; set; }

            public bool TryStart(out string error)
            {
                error = string.Empty;

                if (Config == null)
                {
                    error = "Source configuration is missing.";
                    return false;
                }

                if (!TryGetDocumentId(Config.googleDocUrlOrId, out var docId, out error))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(Config.outputPath))
                {
                    error = "Output path is empty. Set a relative path like Docs/Design.md.";
                    return false;
                }

                if (Path.IsPathRooted(Config.outputPath))
                {
                    error = "Output path must be relative to the Unity project root.";
                    return false;
                }

                FullPath = Path.GetFullPath(Path.Combine(GetProjectRoot(), Config.outputPath));
                var url = $"https://docs.google.com/document/d/{docId}/export?format=md";
                Request = UnityWebRequest.Get(url);
                Request.downloadHandler = new DownloadHandlerBuffer();
                Operation = Request.SendWebRequest();
                return true;
            }

            public void Dispose()
            {
                if (Request != null)
                {
                    Request.Dispose();
                    Request = null;
                }
            }
        }
    }
}
