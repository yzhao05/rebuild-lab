using System.Collections.Generic;
using EMVR.Common.Editor.Validation;
using EMVR.Common.Objects;
using EMVR.Common.UI;
using EMVR.Common.Validation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EMVR.Common.Editor.Samples
{
    /// <summary>
    /// Generates the second Common smoke-test scene without editing serialized
    /// Unity scene YAML. The runtime probe performs the full test automatically.
    /// </summary>
    public static class EmVrObjectStateValidationSceneBuilder
    {
        public const string ScenePath =
            "Assets/Scenes/EMVR_Common_ObjectStateValidation.unity";

        const string BuildMenuPath =
            "Tools/EMVR/Common/Build Object State Validation Scene";
        const string OpenMenuPath =
            "Tools/EMVR/Common/Open Object State Validation Scene";
        const string GeneratedRoot = "Assets/EMVRCommonValidation";
        const string MaterialFolder = GeneratedRoot + "/Materials";

        [MenuItem(BuildMenuPath)]
        public static void BuildScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "EMVR Common",
                    "Exit Play Mode before building the object-state validation scene.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                && !EditorUtility.DisplayDialog(
                    "Rebuild object-state validation scene?",
                    ScenePath + " already exists. Rebuilding will replace the generated scene.",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            EnsureGeneratedFolders();
            Material groundMaterial = GetOrCreateMaterial(
                MaterialFolder + "/ObjectStateGround.mat",
                new Color(0.10f, 0.15f, 0.22f, 1f));
            Material transformMaterial = GetOrCreateMaterial(
                MaterialFolder + "/StateTransformCube.mat",
                new Color(1f, 0.38f, 0.08f, 1f));
            Material parameterMaterial = GetOrCreateMaterial(
                MaterialFolder + "/StateParameterCylinder.mat",
                new Color(0.12f, 0.82f, 0.48f, 1f));
            Material markerMaterial = GetOrCreateMaterial(
                MaterialFolder + "/StateOriginMarker.mat",
                new Color(0.10f, 0.65f, 0.92f, 1f));

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            GameObject root = new GameObject("EMVR Common Object State Validation");
            Camera camera = CreateCamera(root.transform);
            CreateLighting(root.transform);
            CreateEnvironment(root.transform, groundMaterial, markerMaterial);

            EmVrStatusDisplay statusDisplay = CreateStatusDisplay(root.transform, camera);
            CreateInstructionText(root.transform, camera);

            EmVrLabObject transformObject = CreateTransformObject(
                root.transform,
                transformMaterial);
            EmVrParameterizedLabObject parameterObject = CreateParameterizedObject(
                root.transform,
                parameterMaterial);
            CreateObjectLabels(root.transform, camera);

            GameObject systemsObject = new GameObject("Object State Validation Systems");
            systemsObject.transform.SetParent(root.transform, false);
            EmVrObjectStateSnapshotStore snapshotStore =
                systemsObject.AddComponent<EmVrObjectStateSnapshotStore>();
            snapshotStore.explicitObjects = new[] { transformObject, parameterObject };
            snapshotStore.autoDiscoverWhenEmpty = false;

            EmVrObjectStateValidationProbe probe =
                systemsObject.AddComponent<EmVrObjectStateValidationProbe>();
            probe.snapshotStore = snapshotStore;
            probe.transformObject = transformObject;
            probe.parameterizedObject = parameterObject;
            probe.statusDisplay = statusDisplay;
            probe.stepDelaySeconds = 0.65f;
            probe.runOnStart = true;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError(
                    "[EMVR Common] Could not save object-state validation scene: "
                    + ScenePath);
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = root;

            List<EmVrSceneValidator.ValidationIssue> issues =
                EmVrSceneValidator.ValidateActiveScene();
            if (issues.Count == 0)
            {
                Debug.Log(
                    "[EMVR Common] Object-state validation scene built and "
                    + "edit-time validation passed: " + ScenePath);
            }
            else
            {
                Debug.LogWarning(
                    "[EMVR Common] Object-state scene built with " + issues.Count
                    + " validation issue(s). Run Tools > EMVR > Common > Validate Active Scene.");
            }

            EditorUtility.DisplayDialog(
                "EMVR Common",
                "Object-state validation scene created.\n\n"
                + "1. Wait for compilation to finish.\n"
                + "2. Enter Play Mode.\n"
                + "3. Watch the two objects mutate, restore, mutate again, and reset.\n"
                + "4. Confirm the status panel shows PASS and Console contains "
                + "[EMVR Object State Validation] PASS.",
                "OK");
        }

        [MenuItem(OpenMenuPath)]
        public static void OpenScene()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                EditorUtility.DisplayDialog(
                    "EMVR Common",
                    "The object-state validation scene does not exist yet. Run the build menu first.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        static Camera CreateCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 4.8f, -9f);
            cameraObject.transform.rotation = Quaternion.LookRotation(
                new Vector3(0f, 1.25f, 1f) - cameraObject.transform.position,
                Vector3.up);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 1f);
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        static void CreateLighting(Transform parent)
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f, 1f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
        }

        static void CreateEnvironment(
            Transform parent,
            Material groundMaterial,
            Material markerMaterial)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(parent, false);
            ground.transform.position = new Vector3(0f, -0.15f, 1f);
            ground.transform.localScale = new Vector3(9f, 0.3f, 7f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            CreateOriginMarker(parent, new Vector3(-1.7f, 0.02f, 1f), markerMaterial);
            CreateOriginMarker(parent, new Vector3(1.7f, 0.02f, 1f), markerMaterial);
        }

        static void CreateOriginMarker(Transform parent, Vector3 position, Material material)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Authored State Marker";
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            marker.transform.localScale = new Vector3(0.8f, 0.025f, 0.8f);
            marker.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(marker.GetComponent<Collider>());
        }

        static EmVrStatusDisplay CreateStatusDisplay(Transform parent, Camera camera)
        {
            TextMesh text = CreateWorldText(
                "Status Panel",
                parent,
                new Vector3(-4.15f, 3.65f, 1.4f),
                camera,
                TextAnchor.UpperLeft,
                TextAlignment.Left,
                new Color(0.3f, 1f, 0.72f, 1f),
                0.04f,
                56);
            text.text = "result: waiting";

            EmVrStatusDisplay display = text.gameObject.AddComponent<EmVrStatusDisplay>();
            display.worldText = text;
            display.emptyMessage = "result: waiting";
            return display;
        }

        static void CreateInstructionText(Transform parent, Camera camera)
        {
            TextMesh instruction = CreateWorldText(
                "Instructions",
                parent,
                new Vector3(0.0f, 3.75f, 1.5f),
                camera,
                TextAnchor.UpperLeft,
                TextAlignment.Left,
                Color.white,
                0.04f,
                56);
            instruction.text = "EMVR COMMON - OBJECT STATE VALIDATION\n"
                + "Automatic: CAPTURE -> MUTATE -> RESTORE -> MUTATE -> RESET\n"
                + "Checks two types, unique IDs, transforms, parameter, and JSON round trip";
        }

        static EmVrLabObject CreateTransformObject(Transform parent, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Transform State Cube";
            cube.transform.SetParent(parent, false);
            cube.transform.position = new Vector3(-1.7f, 0.7f, 1f);
            cube.transform.rotation = Quaternion.Euler(0f, 12f, 0f);
            cube.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
            cube.GetComponent<Renderer>().sharedMaterial = material;

            EmVrLabObject labObject = cube.AddComponent<EmVrLabObject>();
            labObject.ConfigureIdentity(
                EmVrObjectStateValidationProbe.ExpectedTransformObjectId,
                "ValidationTransformObject",
                "orange transform-state cube");
            return labObject;
        }

        static EmVrParameterizedLabObject CreateParameterizedObject(
            Transform parent,
            Material material)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "Parameterized State Cylinder";
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.position = new Vector3(1.7f, 0.75f, 1f);
            cylinder.transform.rotation = Quaternion.Euler(0f, -15f, 0f);
            cylinder.transform.localScale = new Vector3(0.8f, 0.75f, 0.8f);
            cylinder.GetComponent<Renderer>().sharedMaterial = material;

            EmVrParameterizedLabObject labObject =
                cylinder.AddComponent<EmVrParameterizedLabObject>();
            labObject.ConfigureIdentity(
                EmVrObjectStateValidationProbe.ExpectedParameterizedObjectId,
                "ValidationParameterizedObject",
                "green parameter-state cylinder");
            labObject.ConfigureParameter("field_strength", 2.5f, 0f, 10f);
            return labObject;
        }

        static void CreateObjectLabels(Transform parent, Camera camera)
        {
            TextMesh transformLabel = CreateWorldText(
                "Transform Object Label",
                parent,
                new Vector3(-2.75f, 2.05f, 1.1f),
                camera,
                TextAnchor.MiddleCenter,
                TextAlignment.Center,
                new Color(1f, 0.62f, 0.25f, 1f),
                0.035f,
                52);
            transformLabel.text = "TYPE A\nposition + rotation + scale";

            TextMesh parameterLabel = CreateWorldText(
                "Parameterized Object Label",
                parent,
                new Vector3(2.4f, 2.05f, 1.1f),
                camera,
                TextAnchor.MiddleCenter,
                TextAlignment.Center,
                new Color(0.35f, 1f, 0.65f, 1f),
                0.035f,
                52);
            parameterLabel.text = "TYPE B\ntransform + field_strength";
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
