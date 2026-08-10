using System.Collections.Generic;
using EMVR.Common.Editor.Validation;
using EMVR.Common.Objects;
using EMVR.Common.SceneFlow;
using EMVR.Common.UI;
using EMVR.Common.Validation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace EMVR.Common.Editor.Samples
{
    /// <summary>
    /// Generates a Start/Lab pair and enables both in Build Settings so the
    /// runtime probe can exercise actual single-scene loads and reloads.
    /// </summary>
    public static class EmVrSceneLifecycleValidationSceneBuilder
    {
        public const string StartScenePath =
            "Assets/Scenes/EMVR_Common_SceneLifecycle_Start.unity";
        public const string LabScenePath =
            "Assets/Scenes/EMVR_Common_SceneLifecycle_Lab.unity";

        const string StartSceneName = "EMVR_Common_SceneLifecycle_Start";
        const string LabSceneName = "EMVR_Common_SceneLifecycle_Lab";
        const string BuildMenuPath =
            "Tools/EMVR/Common/Build Scene Lifecycle Validation Scenes";
        const string OpenMenuPath =
            "Tools/EMVR/Common/Open Scene Lifecycle Validation Start";
        const string GeneratedRoot = "Assets/EMVRCommonValidation";
        const string MaterialFolder = GeneratedRoot + "/Materials";
        const string XrSetupPrefabPath =
            "Assets/Samples/XR Interaction Toolkit/2.6.5/Starter Assets/Prefabs/XR Interaction Setup.prefab";

        [MenuItem(BuildMenuPath)]
        public static void BuildScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "EMVR Common",
                    "Exit Play Mode before building the lifecycle validation scenes.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            GameObject setupPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(XrSetupPrefabPath);
            if (setupPrefab == null)
            {
                EditorUtility.DisplayDialog(
                    "EMVR Common",
                    "XRI Starter Assets prefab was not found at:\n" + XrSetupPrefabPath,
                    "OK");
                return;
            }

            bool scenesExist = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScenePath) != null
                || AssetDatabase.LoadAssetAtPath<SceneAsset>(LabScenePath) != null;
            if (scenesExist
                && !EditorUtility.DisplayDialog(
                    "Rebuild scene-lifecycle validation scenes?",
                    "The generated Start or Lab scene already exists. Rebuilding replaces both.",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            EnsureGeneratedFolders();
            Material startGroundMaterial = GetOrCreateMaterial(
                MaterialFolder + "/LifecycleStartGround.mat",
                new Color(0.10f, 0.18f, 0.30f, 1f));
            Material labGroundMaterial = GetOrCreateMaterial(
                MaterialFolder + "/LifecycleLabGround.mat",
                new Color(0.12f, 0.24f, 0.18f, 1f));
            Material resetCubeMaterial = GetOrCreateMaterial(
                MaterialFolder + "/LifecycleResetCube.mat",
                new Color(1f, 0.38f, 0.08f, 1f));

            BuildLabScene(labGroundMaterial, resetCubeMaterial);
            GameObject startRoot = BuildStartScene(setupPrefab, startGroundMaterial);
            EnsureScenesInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = startRoot;

            List<EmVrSceneValidator.ValidationIssue> issues =
                EmVrSceneValidator.ValidateActiveScene();
            if (issues.Count == 0)
            {
                Debug.Log(
                    "[EMVR Common] Scene-lifecycle validation scenes built, enabled in "
                    + "Build Settings, and edit-time validation passed.");
            }
            else
            {
                Debug.LogWarning(
                    "[EMVR Common] Lifecycle Start scene built with " + issues.Count
                    + " validation issue(s). Run Tools > EMVR > Common > Validate Active Scene.");
            }

            EditorUtility.DisplayDialog(
                "EMVR Common",
                "Scene-lifecycle validation scenes created and enabled in Build Settings.\n\n"
                + "1. The Start scene is open now.\n"
                + "2. Wait for compilation, then enter Play Mode.\n"
                + "3. The test automatically runs Start -> Lab -> Reset -> Back three times.\n"
                + "4. Confirm the persistent status panel and Console show "
                + "[EMVR Scene Lifecycle Validation] PASS.",
                "OK");
        }

        [MenuItem(OpenMenuPath)]
        public static void OpenStartScene()
        {
            SceneAsset sceneAsset =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScenePath);
            if (sceneAsset == null)
            {
                EditorUtility.DisplayDialog(
                    "EMVR Common",
                    "The lifecycle Start scene does not exist yet. Run the build menu first.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);
        }

        static void BuildLabScene(Material groundMaterial, Material cubeMaterial)
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            GameObject root = new GameObject("EMVR Lifecycle Lab Content");
            CreateGround(root.transform, groundMaterial);
            CreateSceneTitle(
                root.transform,
                "LAB SCENE\ncontent is re-created by Reset",
                new Color(0.40f, 1f, 0.68f, 1f));

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Lifecycle Reset Cube";
            cube.transform.SetParent(root.transform, false);
            cube.transform.position = new Vector3(0f, 0.72f, 1.6f);
            cube.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
            cube.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            cube.GetComponent<Renderer>().sharedMaterial = cubeMaterial;
            EmVrLabObject labObject = cube.AddComponent<EmVrLabObject>();
            labObject.ConfigureIdentity(
                EmVrSceneLifecycleValidationProbe.ExpectedResetObjectId,
                "LifecycleResetObject",
                "orange lifecycle reset cube");

            EmVrSceneLifecycleMarker marker =
                root.AddComponent<EmVrSceneLifecycleMarker>();
            marker.sceneRole = EmVrSceneLifecycleValidationProbe.LabRole;
            marker.resetValidationObject = labObject;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, LabScenePath))
            {
                throw new System.InvalidOperationException(
                    "Could not save lifecycle Lab scene: " + LabScenePath);
            }
        }

        static GameObject BuildStartScene(GameObject setupPrefab, Material groundMaterial)
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            GameObject contentRoot = new GameObject("EMVR Lifecycle Start Content");
            CreateGround(contentRoot.transform, groundMaterial);
            CreateSceneTitle(
                contentRoot.transform,
                "START SCENE\nbootstrap duplicate is removed on Back",
                new Color(0.38f, 0.76f, 1f, 1f));
            EmVrSceneLifecycleMarker marker =
                contentRoot.AddComponent<EmVrSceneLifecycleMarker>();
            marker.sceneRole = EmVrSceneLifecycleValidationProbe.StartRole;
            marker.resetValidationObject = null;

            GameObject persistentRootObject =
                new GameObject("EMVR Persistent Application Services");
            EmVrPersistentServiceRoot persistentRoot =
                persistentRootObject.AddComponent<EmVrPersistentServiceRoot>();
            persistentRoot.serviceId = "lifecycle-validation-services";

            GameObject setup =
                PrefabUtility.InstantiatePrefab(setupPrefab, persistentRootObject.transform)
                    as GameObject;
            if (setup == null)
            {
                throw new System.InvalidOperationException(
                    "Could not instantiate XRI Starter Assets setup.");
            }

            setup.name = "Persistent XR Interaction Setup";
            DisableXrRendering(setup);
            EventSystem eventSystem = setup.GetComponentInChildren<EventSystem>(true);
            if (eventSystem == null)
            {
                throw new System.InvalidOperationException(
                    "The XRI Starter Assets setup contains no EventSystem.");
            }

            Camera desktopCamera = CreateDesktopCamera(persistentRootObject.transform);
            CreatePersistentLight(persistentRootObject.transform);
            EmVrStatusDisplay statusDisplay = CreatePersistentStatus(
                persistentRootObject.transform,
                desktopCamera);
            GameObject inputTarget = new GameObject("Persistent Input Route Target");
            inputTarget.transform.SetParent(persistentRootObject.transform, false);

            EmVrSceneLoader loader =
                persistentRootObject.AddComponent<EmVrSceneLoader>();
            loader.loadMode = LoadSceneMode.Single;
            loader.loadAsynchronously = true;
            loader.preventRepeatedRequests = true;

            EmVrSceneLifecycleValidationProbe probe =
                persistentRootObject.AddComponent<EmVrSceneLifecycleValidationProbe>();
            probe.persistentRoot = persistentRoot;
            probe.sceneLoader = loader;
            probe.statusDisplay = statusDisplay;
            probe.inputProbeTarget = inputTarget;
            probe.startSceneName = StartSceneName;
            probe.labSceneName = LabSceneName;
            probe.repetitionCount = 3;
            probe.stageDelaySeconds = 0.2f;
            probe.sceneLoadTimeoutSeconds = 8f;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, StartScenePath))
            {
                throw new System.InvalidOperationException(
                    "Could not save lifecycle Start scene: " + StartScenePath);
            }

            return persistentRootObject;
        }

        static Camera CreateDesktopCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Persistent Desktop Camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 4.8f, -9f);
            cameraObject.transform.rotation = Quaternion.LookRotation(
                new Vector3(0f, 1.1f, 1.5f) - cameraObject.transform.position,
                Vector3.up);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 1f);
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        static void DisableXrRendering(GameObject setup)
        {
            Camera[] cameras = setup.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                cameras[i].enabled = false;
            }

            AudioListener[] listeners = setup.GetComponentsInChildren<AudioListener>(true);
            for (int i = 0; i < listeners.Length; i++)
            {
                listeners[i].enabled = false;
            }
        }

        static void CreatePersistentLight(Transform parent)
        {
            GameObject lightObject = new GameObject("Persistent Directional Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f, 1f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
        }

        static EmVrStatusDisplay CreatePersistentStatus(Transform parent, Camera camera)
        {
            TextMesh text = CreateWorldText(
                "Persistent Lifecycle Status",
                parent,
                new Vector3(-4.15f, 3.75f, 1.3f),
                camera,
                TextAnchor.UpperLeft,
                TextAlignment.Left,
                new Color(0.3f, 1f, 0.72f, 1f),
                0.032f,
                50);
            text.text = "result: waiting";
            EmVrStatusDisplay display = text.gameObject.AddComponent<EmVrStatusDisplay>();
            display.worldText = text;
            display.emptyMessage = "result: waiting";
            return display;
        }

        static void CreateGround(Transform parent, Material material)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Scene Ground";
            ground.transform.SetParent(parent, false);
            ground.transform.position = new Vector3(0f, -0.15f, 1.5f);
            ground.transform.localScale = new Vector3(9f, 0.3f, 7f);
            ground.GetComponent<Renderer>().sharedMaterial = material;
        }

        static void CreateSceneTitle(Transform parent, string value, Color color)
        {
            GameObject textObject = new GameObject("Scene Role Title");
            textObject.transform.SetParent(parent, false);
            textObject.transform.position = new Vector3(0.2f, 3.5f, 1.5f);
            Vector3 cameraPosition = new Vector3(0f, 4.8f, -9f);
            textObject.transform.rotation = Quaternion.LookRotation(
                textObject.transform.position - cameraPosition,
                Vector3.up);
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.UpperLeft;
            text.alignment = TextAlignment.Left;
            text.color = color;
            text.characterSize = 0.045f;
            text.fontSize = 58;
            text.richText = false;
            text.text = value;
        }

        static TextMesh CreateWorldText(
            string name,
            Transform parent,
            Vector3 position,
            Camera camera,
            TextAnchor anchor,
            TextAlignment alignment,
            Color color,
            float characterSize,
            int fontSize)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.transform.position = position;
            textObject.transform.rotation = Quaternion.LookRotation(
                textObject.transform.position - camera.transform.position,
                Vector3.up);
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.anchor = anchor;
            text.alignment = alignment;
            text.color = color;
            text.characterSize = characterSize;
            text.fontSize = fontSize;
            text.richText = false;
            return text;
        }

        static void EnsureScenesInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes =
                new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            EnsureBuildScene(scenes, StartScenePath);
            EnsureBuildScene(scenes, LabScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void EnsureBuildScene(
            List<EditorBuildSettingsScene> scenes,
            string scenePath)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                if (!string.Equals(
                    scenes[i].path,
                    scenePath,
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                scenes[i].enabled = true;
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        }

        static Material GetOrCreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Lit");
                }

                if (shader == null)
                {
                    throw new System.InvalidOperationException(
                        "No supported lit shader is available.");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        static void EnsureGeneratedFolders()
        {
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets", "EMVRCommonValidation");
            EnsureFolder(GeneratedRoot, "Materials");
        }

        static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
