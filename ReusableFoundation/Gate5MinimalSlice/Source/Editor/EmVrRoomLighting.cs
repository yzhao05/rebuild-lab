using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EMVR.ReusableFoundation.Gate5.Editor
{
    // Protected shared implementation. Lab builders provide identity and paths only.
    public sealed class EmVrRoomLighting
    {
        const float DefaultRoomLightIntensity = 5f;
        const float DefaultAmbientIntensity = 1f;
        public string SourceLightingPath { get; private set; }
        public string ExecutionPath { get; private set; }
        public string OutputScenePath { get; private set; }

        public static EmVrRoomLighting ForLab(string labId, string scenePath)
        {
            if (labId == null || !Regex.IsMatch(labId, @"\A[a-z][a-z0-9_]*\z"))
                throw new ArgumentException("Use a validated Lab ID.", nameof(labId));
            // Match the workflow's accepted scene names, including Unicode/spaces,
            // while keeping persisted paths POSIX and rejecting traversal/quoting.
            if (scenePath == null || !scenePath.StartsWith("Assets/Scenes/", StringComparison.Ordinal)
                || !scenePath.EndsWith(".unity", StringComparison.Ordinal)
                || scenePath.IndexOfAny(new[] { '\\', '\"', '\'', '\r', '\n', '\0' }) >= 0
                || Array.Exists(scenePath.Split('/'), part => part == ".." || part == "." || part.Length == 0))
                throw new ArgumentException("Use a repository-relative POSIX scene path under Assets/Scenes.", nameof(scenePath));
            return new EmVrRoomLighting {
                SourceLightingPath = "Assets/Generated/" + labId + "/ApprovedDependencies/EMVRRoom/Lighting/RoomLighting.lighting",
                ExecutionPath = "Assets/Scenes/" + labId + "/RoomLighting_Execution.lighting",
                OutputScenePath = scenePath
            };
        }

        EmVrRoomLighting() { }

        public static string[] FilterSavePaths(string[] paths)
        {
            var allowed = new List<string>();
            foreach (string path in paths)
                if (!Regex.IsMatch(path.Replace('\\', '/'),
                    @"\AAssets/Generated/[a-z][a-z0-9_]*/ApprovedDependencies/EMVRRoom/Lighting/RoomLighting\.lighting(?:\.meta)?\z", RegexOptions.IgnoreCase))
                    allowed.Add(path);
            return allowed.ToArray();
        }

        [InitializeOnLoadMethod]
        static void RetireOriginalCacheAfterReload()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
                    || EditorApplication.isUpdating || Lightmapping.isRunning) return;
                string path = AssetDatabase.GetAssetPath(Lightmapping.lightingSettings);
                Match match = Regex.Match(path, @"\AAssets/Scenes/([a-z][a-z0-9_]*)/RoomLighting_Execution\.lighting\z");
                if (!match.Success) return;
                string scene = SceneManager.GetActiveScene().path;
                if (string.IsNullOrEmpty(scene)) return;
                var lighting = ForLab(match.Groups[1].Value, scene);
                lighting.VerifyOriginalLightingAfterSave();
                lighting.RetireOriginalLightingCache();
            };
        }

        [Serializable]
        sealed class LightingExecutionProvenance
        {
            public string source_path = "ApprovedAssets/EMVRRoom/Lighting/RoomLighting.lighting";
            public string source_sha256 = "A49EB3AFA0B879432FFCAAF4150C79618BC118D43D08EBA4F02AAFB52E3FE27B";
            public string source_guid = "28f1169e0b81e7e4ca1f34e58759ebca";
            public string execution_guid;
        }

        public LightingSettings RequireRoomLightingSettings()
        {
            string lightingPath = SourceLightingPath;
            // A valid file on disk and Unity's loaded import are separate checks.
            // Only reimport after confirming exact approved bytes; never rewrite defaults.
            RequireLightingBytes(lightingPath, "A49EB3AFA0B879432FFCAAF4150C79618BC118D43D08EBA4F02AAFB52E3FE27B");
            RequireLightingBytes(lightingPath + ".meta", "94C11E510CFF135AFB7CD42CEC59C1DC98000B0951D551E44220E8F14710F1DB");
            if (AssetDatabase.LoadAssetAtPath<LightingSettings>(ExecutionPath) != null)
            {
                // Validate against frozen file bytes, not a freshly loaded mutable source object.
                LightingSettings existing = RequireExecutionLighting(null, lightingPath);
                RetireOriginalLightingCache();
                return existing;
            }
            LightingSettings source;
            try
            {
                // Keep a valid loaded object; unnecessary reimports can retrigger GI precompute.
                source = LoadVerifiedRoomLighting(lightingPath);
            }
            catch (InvalidOperationException)
            {
                AssetDatabase.ImportAsset(lightingPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                RequireLightingBytes(lightingPath, "A49EB3AFA0B879432FFCAAF4150C79618BC118D43D08EBA4F02AAFB52E3FE27B");
                RequireLightingBytes(lightingPath + ".meta", "94C11E510CFF135AFB7CD42CEC59C1DC98000B0951D551E44220E8F14710F1DB");
                source = LoadVerifiedRoomLighting(lightingPath);
            }
            LightingSettings execution = RequireExecutionLighting(source, lightingPath);
            RetireOriginalLightingCache();
            return execution;
        }

        static LightingSettings LoadVerifiedRoomLighting(string lightingPath)
        {
            string guid = AssetDatabase.AssetPathToGUID(lightingPath);
            if (guid != "28f1169e0b81e7e4ca1f34e58759ebca")
                throw new InvalidOperationException("RoomLighting GUID mismatch after import: expected 28f1169e0b81e7e4ca1f34e58759ebca, actual " + guid);
            LightingSettings settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(lightingPath);
            if (settings == null)
                throw new InvalidOperationException("RoomLighting bytes/GUID are correct, but Unity did not load a LightingSettings object after reimport: " + lightingPath);
            using (SerializedObject serialized = new SerializedObject(settings))
            {
                serialized.Update();
                SerializedProperty realtime = serialized.FindProperty("m_EnableRealtimeLightmaps");
                SerializedProperty baked = serialized.FindProperty("m_EnableBakedLightmaps");
                SerializedProperty workflow = serialized.FindProperty("m_GIWorkflowMode");
                if (realtime == null || baked == null || workflow == null)
                    throw new InvalidOperationException("Unity's imported LightingSettings schema is missing required GI properties.");
                // Unity can load the frozen source with Auto Generate enabled even though
                // its YAML says OnDemand. Never modify this imported source to compensate.
                if (!realtime.boolValue || baked.boolValue || (workflow.intValue != 0 && workflow.intValue != 1)
                    || !settings.realtimeGI || settings.bakedGI || workflow.intValue != (settings.autoGenerate ? 0 : 1))
                    throw new InvalidOperationException("RoomLighting source has incompatible loaded GI values. "
                        + "Serialized realtime=" + realtime.boolValue + ", baked=" + baked.boolValue + ", workflow=" + workflow.intValue
                        + "; API realtime=" + settings.realtimeGI + ", baked=" + settings.bakedGI + ", autoGenerate=" + settings.autoGenerate
                        + ". Expected realtime=true, baked=false and a consistent workflow enum. Source bytes remain read-only.");
            }
            return settings;
        }

        LightingSettings RequireExecutionLighting(LightingSettings source, string sourcePath)
        {
            string executionPath = ExecutionPath;
            LightingSettings execution = AssetDatabase.LoadAssetAtPath<LightingSettings>(executionPath);
            if ((source != null && execution == source) || AssetDatabase.AssetPathToGUID(executionPath) == "28f1169e0b81e7e4ca1f34e58759ebca")
                throw new InvalidOperationException("Execution settings must have their own identity; refusing to write the imported source.");
            if (execution == null)
            {
                if (source == null)
                    throw new InvalidOperationException("Execution settings disappeared during validation; retry after asset import finishes.");
                if (System.IO.File.Exists(executionPath))
                    throw new InvalidOperationException("Unity cannot load the existing execution LightingSettings: " + executionPath);
                EnsureAssetFolder(executionPath);
                execution = UnityEngine.Object.Instantiate(source);
                execution.name = "RoomLighting_Execution";
                execution.autoGenerate = false; // Only the current-scene execution object is writable here.
                AssetDatabase.CreateAsset(execution, executionPath);
            }
            else
            {
                // Reject authored drift before restoring a known native loaded-mode mismatch.
                ValidateExecutionLightingFields(sourcePath, executionPath);
                if (execution.autoGenerate)
                {
                    execution.autoGenerate = false;
                    EditorUtility.SetDirty(execution);
                }
            }
            AssetDatabase.SaveAssetIfDirty(execution);
            if (execution.autoGenerate || !execution.realtimeGI || execution.bakedGI)
                throw new InvalidOperationException("Unity did not retain OnDemand on the current-scene execution settings.");
            ValidateExecutionLightingFields(sourcePath, executionPath);
            var provenance = new LightingExecutionProvenance { execution_guid = AssetDatabase.AssetPathToGUID(executionPath) };
            string provenancePath = executionPath + ".provenance.json";
            string json = JsonUtility.ToJson(provenance, true) + "\n";
            if (!System.IO.File.Exists(provenancePath) || System.IO.File.ReadAllText(provenancePath) != json)
                System.IO.File.WriteAllText(provenancePath, json, new System.Text.UTF8Encoding(false));
            // Saving the generated execution object must never serialize the imported original.
            RequireLightingBytes(sourcePath, provenance.source_sha256);
            RequireLightingBytes(sourcePath + ".meta", "94C11E510CFF135AFB7CD42CEC59C1DC98000B0951D551E44220E8F14710F1DB");
            return execution;
        }

        static void ValidateExecutionLightingFields(string sourcePath, string executionPath)
        {
            ValidateLightingFields(System.IO.File.ReadAllText(sourcePath), System.IO.File.ReadAllText(executionPath));
        }

        public static void ValidateLightingFields(string sourceYaml, string executionYaml)
        {
            var expected = LightingFields(sourceYaml);
            var actual = LightingFields(executionYaml);
            if (expected.Count == 0 || actual.Count == 0)
                throw new InvalidOperationException("LightingSettings fields are missing.");
            foreach (var item in expected)
            {
                if (item.Key == "m_Name" || item.Key == "serializedVersion") continue; // Native identity/schema, not light values.
                string value;
                if (!actual.TryGetValue(item.Key, out value) || !SameLightingValue(item.Value, value))
                    throw new InvalidOperationException("Execution LightingSettings differs from the frozen source field " + item.Key
                        + "; expected " + item.Value + ", actual " + (value ?? "<missing>") + ". No room brightness/default changes are allowed.");
            }
        }

        static Dictionary<string, string> LightingFields(string yaml)
        {
            yaml = System.Text.RegularExpressions.Regex.Replace(yaml, @"\r?\n {4,}(?=\S)", " ");
            var result = new Dictionary<string, string>();
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(yaml,
                @"^  (\w+): ([^\r\n]*)", System.Text.RegularExpressions.RegexOptions.Multiline))
                result.Add(match.Groups[1].Value, match.Groups[2].Value);
            return result;
        }

        static bool SameLightingValue(string expected, string actual)
        {
            if (System.Text.RegularExpressions.Regex.Replace(expected, @"\s", "") == System.Text.RegularExpressions.Regex.Replace(actual, @"\s", "")) return true;
            double left, right;
            return double.TryParse(expected, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out left)
                && double.TryParse(actual, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out right)
                && !double.IsNaN(left) && !double.IsNaN(right) && Math.Abs(left - right) <= 0.000001;
        }

        static void RequireLightingBytes(string path, string expectedHash)
        {
            if (!System.IO.File.Exists(path))
                throw new InvalidOperationException("Required RoomLighting file is missing: " + path);
            using (var hash = System.Security.Cryptography.SHA256.Create())
            using (var stream = System.IO.File.OpenRead(path))
            {
                string actual = BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "");
                if (actual != expectedHash)
                    throw new InvalidOperationException("RoomLighting byte-integrity mismatch: " + path
                        + "; expected " + expectedHash + ", actual " + actual + ". Stop and ask the workflow maintainer to repair the approved import.");
            }
        }

        public void RetireOriginalLightingCache()
        {
            foreach (LightingSettings original in Resources.FindObjectsOfTypeAll<LightingSettings>())
            {
                if (AssetDatabase.GetAssetPath(original) != SourceLightingPath) continue;
                bool referenced = false;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    if (Lightmapping.GetLightingSettingsForScene(SceneManager.GetSceneAt(i)) == original)
                        referenced = true;
                // The file is immutable. Discard only its unsaved native cache changes.
                EditorUtility.ClearDirty(original);
                if (!referenced) Resources.UnloadAsset(original);
            }
        }

        public void VerifyOriginalLightingAfterSave()
        {
            RequireLightingBytes(SourceLightingPath, "A49EB3AFA0B879432FFCAAF4150C79618BC118D43D08EBA4F02AAFB52E3FE27B");
            RequireLightingBytes(SourceLightingPath + ".meta", "94C11E510CFF135AFB7CD42CEC59C1DC98000B0951D551E44220E8F14710F1DB");
        }

        public void RequireExecutionBinding(string actualPath, bool realtimeGI, bool bakedGI, bool autoGenerate)
        {
            if (actualPath != ExecutionPath || !realtimeGI || bakedGI || autoGenerate)
                throw new InvalidOperationException("Assign only this Lab's verified OnDemand execution settings; imported originals and other Lab settings are read-only.");
        }

        public void ApplyRoomLightingDefaults(Transform room, LightingSettings settings)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            RequireExecutionBinding(AssetDatabase.GetAssetPath(settings), settings.realtimeGI, settings.bakedGI, settings.autoGenerate);
            VerifyOriginalLightingAfterSave();
            ValidateExecutionLightingFields(SourceLightingPath, ExecutionPath);
            Light[] lights = room.GetComponentsInChildren<Light>(true);
            if (lights.Length != 4)
                throw new InvalidOperationException("The complete room requires four original point lights.");
            foreach (Light light in lights)
                if (!light.isActiveAndEnabled || light.type != LightType.Point
                    || !Mathf.Approximately(light.intensity, DefaultRoomLightIntensity))
                    throw new InvalidOperationException("Room lights must retain the frozen instance intensity 5.");

            // Assign only the validated native execution asset; source imports stay immutable.
            Lightmapping.lightingSettings = settings;
            if (Lightmapping.lightingSettings != settings)
                throw new InvalidOperationException("Unity did not retain the automatic RoomLighting reference; scene build stopped.");
            RetireOriginalLightingCache();
            // Renderer IDs change on rebuild. Unity must generate data for this new scene.
            Lightmapping.lightingDataAsset = null;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            // User-approved scene default: original Flat ambient RGB x 1.5; source assets remain unchanged.
            RenderSettings.ambientSkyColor = new Color(0.13116321f, 0.15650724f, 0.212264115f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.1400303f, 0.21646784f, 0.27205884f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.048010383f, 0.112683475f, 0.17647058f, 1f);
            RenderSettings.ambientIntensity = DefaultAmbientIntensity;
            RenderSettings.skybox = null;
            RenderSettings.sun = null;
            RenderSettings.fog = false;
            RenderSettings.fogColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.01f;
            RenderSettings.fogStartDistance = 0f;
            RenderSettings.fogEndDistance = 300f;
            RenderSettings.subtractiveShadowColor = new Color(0.42f, 0.478f, 0.627f, 1f);
            RenderSettings.haloStrength = 0.5f;
            RenderSettings.flareStrength = 1f;
            RenderSettings.flareFadeSpeed = 3f;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.defaultReflectionResolution = 128;
            RenderSettings.reflectionBounces = 1;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.reflectionIntensity = 1f;
        }

        static void EnsureAssetFolder(string assetPath)
        {
            string folder = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
            string current = "Assets";
            string[] parts = folder.Split('/');
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }

    sealed class EmVrProtectedLightingSaveGuard : AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths) => EmVrRoomLighting.FilterSavePaths(paths);
    }
}
