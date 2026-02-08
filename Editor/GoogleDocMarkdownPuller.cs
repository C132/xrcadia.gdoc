using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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
            var sources = FindSources();
            if (sources.Count == 0)
            {
                return;
            }

            var nowUtc = DateTime.UtcNow;
            var toPull = new List<GoogleDocMarkdownSource>();
            foreach (var source in sources)
            {
                if (source == null)
                {
                    continue;
                }

                if (!onlyAutoPullEnabled || ShouldAutoPull(source, nowUtc))
                {
                    toPull.Add(source);
                }
            }

            if (toPull.Count == 0)
            {
                return;
            }

            QueueJobs(toPull);
        }

        public static void PullSource(GoogleDocMarkdownSource source)
        {
            if (source == null)
            {
                return;
            }

            QueueJobs(new List<GoogleDocMarkdownSource> { source });
        }

        private static void QueueJobs(IReadOnlyList<GoogleDocMarkdownSource> sources)
        {
            var total = sources.Count;
            for (int i = 0; i < total; i++)
            {
                var labelSuffix = total > 1 ? $" ({i + 1}/{total})" : string.Empty;
                PendingJobs.Enqueue(new PullJob(sources[i], labelSuffix));
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
                if (job.Source == null)
                {
                    continue;
                }

                SetStatus(job.Source, string.Empty, false);
                if (!job.TryStart(out var error))
                {
                    SetStatus(job.Source, error, false);
                    Debug.LogError(error, job.Source);
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

            if (activeJob.Source == null)
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

            EditorUtility.DisplayProgressBar(
                ProgressTitle,
                $"Downloading {activeJob.Source.name}{activeJob.LabelSuffix}",
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
                if (job.Source == null)
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
                var directory = Path.GetDirectoryName(job.FullPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                EditorUtility.DisplayProgressBar(
                    ProgressTitle,
                    $"Saving {job.Source.name}{job.LabelSuffix}",
                    1f);

                File.WriteAllText(job.FullPath, markdown, new UTF8Encoding(false));
                AssetDatabase.Refresh();

                SetStatus(job.Source, string.Empty, true);
                Debug.Log($"Google Doc Markdown pulled to {job.Source.outputPath}", job.Source);
            }
            catch (Exception ex)
            {
                HandleFailure(job, $"Pull failed: {ex.Message}");
            }
        }

        private static void HandleFailure(PullJob job, string message)
        {
            if (job.Source != null)
            {
                SetStatus(job.Source, message, false);
                Debug.LogError(message, job.Source);
            }
        }

        private static void FinalizeJob(PullJob job)
        {
            job.Dispose();
            activeJob = null;
            EditorUtility.ClearProgressBar();
            StartNextJob();
        }

        private static void SetStatus(GoogleDocMarkdownSource source, string error, bool success)
        {
            source.lastError = error ?? string.Empty;
            if (success)
            {
                source.lastPulledUtcIso = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            }

            EditorUtility.SetDirty(source);
            AssetDatabase.SaveAssets();
        }

        private static bool ShouldAutoPull(GoogleDocMarkdownSource source, DateTime nowUtc)
        {
            if (!source.autoPullOnEditorStartup)
            {
                return false;
            }

            var minMinutes = Math.Max(0, source.minimumMinutesBetweenAutoPulls);
            if (minMinutes == 0 || string.IsNullOrWhiteSpace(source.lastPulledUtcIso))
            {
                return true;
            }

            if (!DateTime.TryParse(
                    source.lastPulledUtcIso,
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

        private static List<GoogleDocMarkdownSource> FindSources()
        {
            var results = new List<GoogleDocMarkdownSource>();
            var guids = AssetDatabase.FindAssets("t:GoogleDocMarkdownSource");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var source = AssetDatabase.LoadAssetAtPath<GoogleDocMarkdownSource>(path);
                if (source != null)
                {
                    results.Add(source);
                }
            }

            return results;
        }

        private sealed class PullJob
        {
            public PullJob(GoogleDocMarkdownSource source, string labelSuffix)
            {
                Source = source;
                LabelSuffix = labelSuffix;
            }

            public GoogleDocMarkdownSource Source { get; }
            public string LabelSuffix { get; }
            public UnityWebRequest Request { get; private set; }
            public UnityWebRequestAsyncOperation Operation { get; private set; }
            public string FullPath { get; private set; }

            public bool TryStart(out string error)
            {
                error = string.Empty;

                if (Source == null)
                {
                    error = "Source asset is missing.";
                    return false;
                }

                if (!TryGetDocumentId(Source.googleDocUrlOrId, out var docId, out error))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(Source.outputPath))
                {
                    error = "Output path is empty. Set a relative path like Docs/Design.md.";
                    return false;
                }

                if (Path.IsPathRooted(Source.outputPath))
                {
                    error = "Output path must be relative to the Unity project root.";
                    return false;
                }

                FullPath = Path.GetFullPath(Path.Combine(GetProjectRoot(), Source.outputPath));
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
