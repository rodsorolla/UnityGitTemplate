using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Sorolla.PersistentData.Editor
{
    /// <summary>
    /// Warns about editor-modified save files before building.
    /// </summary>
    public class BuildPreprocessor : IPreprocessBuildWithReport
    {
        private const string MetadataFileName = "editor_metadata.json";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var savesBasePath = Path.Combine(Application.persistentDataPath, "saves");
            var metadataPath = Path.Combine(savesBasePath, MetadataFileName);

            if (!File.Exists(metadataPath))
                return;

            try
            {
                var json = File.ReadAllText(metadataPath);
                var modifiedFiles = JsonConvert.DeserializeObject<List<string>>(json);

                if (modifiedFiles == null || modifiedFiles.Count == 0)
                    return;

                // Check which files still exist
                var existingModified = new List<string>();
                foreach (var file in modifiedFiles)
                {
                    if (File.Exists(file))
                        existingModified.Add(Path.GetFileName(file));
                }

                if (existingModified.Count == 0)
                    return;

                var fileList = string.Join("\n• ", existingModified);
                var message = $"The following save files were modified in the Editor:\n\n• {fileList}\n\n" +
                              "These modifications may affect your build. Consider using 'Clean All Editor Saves' " +
                              "in the Save Data Editor window before building.\n\n" +
                              "Continue with build?";

                if (!EditorUtility.DisplayDialog("Editor-Modified Saves Detected", message, "Continue", "Cancel Build"))
                {
                    throw new BuildFailedException("Build cancelled: Editor-modified save files detected.");
                }
            }
            catch (BuildFailedException)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Failed to check for editor-modified saves: {ex.Message}");
            }
        }
    }
}
