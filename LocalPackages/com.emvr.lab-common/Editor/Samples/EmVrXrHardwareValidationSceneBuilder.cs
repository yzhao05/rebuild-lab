using System.Collections.Generic;
using EMVR.Common.Assist;
using EMVR.Common.Editor.Validation;
using EMVR.Common.Interaction;
using EMVR.Common.Objects;
using EMVR.Common.Telemetry;
using EMVR.Common.UI;
using EMVR.Common.Validation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

namespace EMVR.Common.Editor.Samples
{
    /// <summary>
    /// Generates a manual, real-device validation scene using the same standard
    /// XRI Starter Assets rig and controller-ray structure as the rebuilt labs.
    /// </summary>
    public static class EmVrXrHardwareValidationSceneBuilder
    {
        public const string ScenePath =
            "Assets/Scenes/EMVR_Common_XrHardwareValidation.unity";

        const string BuildMenuPath =
            "Tools/EMVR/Common/Build XR Hardware Validation Scene";
        const string OpenMenuPath =
            "Tools/EMVR/Common/Open XR Hardware Validation Scene";
        const string GeneratedRoot = "Assets/EMVRCommonValidation";
        const string MaterialFolder = GeneratedRoot + "/Materials";
        const string XrSetupPrefabPath =
            "Assets/Samples/XR Interaction Toolkit/2.6.5/Starter Assets/Prefabs/XR Interaction Setup.prefab";

        [MenuItem(BuildMenuPath)]
        public static void BuildScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "EMVR Common",
                    "Exit Play Mode before building the XR hardware validation scene.",
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

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                && !EditorUtility.DisplayDialog(
                    "Rebuild XR hardware validation scene?",
                    ScenePath + " already exists. Rebuilding will replace the generated scene.",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            EnsureGeneratedFolders();
            Material groundMaterial = GetOrCreateMaterial(
                MaterialFolder + "/HardwareGround.mat",
                new Color(0.10f, 0.15f, 0.22f, 1f));
            Material cubeMaterial = GetOrCreateMaterial(
                MaterialFolder + "/HardwareGrabCube.mat",
                new Color(1f, 0.38f, 0.08f, 1f));
            Material markerMaterial = GetOrCreateMaterial(
                MaterialFolder + "/HardwareTargetMarker.mat",
                new Color(0.08f, 0.76f, 0.92f, 1f));

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            GameObject root = new GameObject("EMVR Common XR Hardware Validation");
            Camera xrCamera = CreateXrRig(root.transform, setupPrefab);
            if (xrCamera == null)
            {
                Debug.LogError("[EMVR Common] XRI setup contains no Camera.");
                return;
            }

            CreateLighting(root.transform);
            CreateEnvironment(root.transform, groundMaterial, markerMaterial);
            EmVrStatusDisplay statusDisplay = CreateStatusDisplay(root.transform, xrCamera);
            CreateInstructionText(root.transform, xrCamera);

            GameObject systemsObject = new GameObject("XR Hardware Validation Systems");
            systemsObject.transform.SetParent(root.transform, false);
            EmVrTelemetryEmitter telemetry =
                systemsObject.AddComponent<EmVrTelemetryEmitter>();
            telemetry.historyCapacity = 32;
            telemetry.logToConsole = false;

            EmVrAssistGroundingProvider grounding =
                systemsObject.AddComponent<EmVrAssistGroundingProvider>();
            grounding.sessionId = "xr-hardware-validation-session";
            grounding.userId = "local-hardware-validator";
            grounding.labId = EmVrXrHardwareValidationProbe.ExpectedLabId;
            grounding.stepId = "grab_with_physical_controller";
            grounding.mode = "physical_xr_validation";
            grounding.telemetryEmitter = telemetry;

            XRGrabInteractable grabInteractable;
            EmVrLabObject labObject;
            CreateHardwareGrabCube(
                root.transform,
                cubeMaterial,
                statusDisplay,
                telemetry,
                grounding,
                out grabInteractable,
                out labObject);
            grounding.explicitObjectSources = new MonoBehaviour[] { labObject };
            grounding.discoverObjectSourcesInChildren = false;

            EmVrXrRayRuntimeTuner rayTuner =
                systemsObject.AddComponent<EmVrXrRayRuntimeTuner>();
            rayTuner.sphereCastRadius = 0.06f;
            rayTuner.minimumRayDistance = 20f;
            rayTuner.controllerRaysOnly = true;
            rayTuner.enableMouseLeftDebugSelect = false;
            rayTuner.xrViewStateSource = null;

            EmVrXrHardwareValidationProbe probe =
                systemsObject.AddComponent<EmVrXrHardwareValidationProbe>();
            probe.grabInteractable = grabInteractable;
            probe.labObject = labObject;
            probe.statusDisplay = statusDisplay;
            probe.telemetryEmitter = telemetry;
            probe.assistGrounding = grounding;
            probe.requiredMovementDistance = 0.05f;
            probe.requireBothControllers = true;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError(
                    "[EMVR Common] Could not save XR hardware validation scene: "
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
                    "[EMVR Common] XR hardware validation scene built and "
                    + "edit-time validation passed: " + ScenePath);
            }
            else
            {
                Debug.LogWarning(
                    "[EMVR Common] XR hardware scene built with " + issues.Count
                    + " validation issue(s). Run Tools > EMVR > Common > Validate Active Scene.");
            }

            EditorUtility.DisplayDialog(
                "EMVR Common",
                "XR hardware validation scene created.\n\n"
                + "1. Connect and wake a real OpenXR headset and both controllers.\n"
                + "2. Enter Play Mode and put on the headset.\n"
                + "3. Point a controller ray at the orange cube.\n"
                + "4. Hold Trigger/Select, move the cube at least 5 cm, then release.\n"
                + "5. Confirm the panel and Console show [EMVR XR Hardware Validation] PASS.\n\n"
                + "XR Device Simulator input is shown as SIMULATED and cannot pass this test.",
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
                    "The XR hardware validation scene does not exist yet. Run the build menu first.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        static Camera CreateXrRig(Transform parent, GameObject setupPrefab)
        {
            GameObject setup = PrefabUtility.InstantiatePrefab(setupPrefab, parent) as GameObject;
            if (setup == null)
            {
                return null;
            }

            setup.name = "XR Hardware Rig (standard XRI controllers + rays)";
            Camera camera = setup.GetComponentInChildren<Camera>(true);
            if (camera != null)
            {
                camera.enabled = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 1f);
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 100f;
                AudioListener listener = camera.GetComponent<AudioListener>();
                if (listener != null)
                {
                    listener.enabled = true;
                }
            }

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
            ground.transform.position = new Vector3(0f, -0.15f, 2.2f);
            ground.transform.localScale = new Vector3(7f, 0.3f, 6f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Hardware Grab Target Marker";
            marker.transform.SetParent(parent, false);
            marker.transform.position = new Vector3(0f, 0.02f, 2.3f);
            marker.transform.localScale = new Vector3(1.1f, 0.025f, 1.1f);
            marker.GetComponent<Renderer>().sharedMaterial = markerMaterial;
            Object.DestroyImmediate(marker.GetComponent<Collider>());
        }

        static EmVrStatusDisplay CreateStatusDisplay(Transform parent, Camera camera)
        {
            TextMesh text = CreateWorldText(
                "Hardware Status Panel",
                parent,
                new Vector3(-2.9f, 2.8f, 3.1f),
                camera,
                TextAnchor.UpperLeft,
                TextAlignment.Left,
                new Color(0.3f, 1f, 0.72f, 1f),
                0.025f,
                46);
            text.text = "scene: waiting";

            EmVrStatusDisplay display = text.gameObject.AddComponent<EmVrStatusDisplay>();
            display.worldText = text;
            display.emptyMessage = "scene: waiting";
            return display;
        }

        static void CreateInstructionText(Transform parent, Camera camera)
        {
            TextMesh instruction = CreateWorldText(
                "Hardware Instructions",
                parent,
                new Vector3(0.2f, 2.8f, 3.1f),
                camera,
                TextAnchor.UpperLeft,
                TextAlignment.Left,
                Color.white,
                0.027f,
                48);
            instruction.text = "EMVR COMMON - REAL XR HARDWARE VALIDATION\n"
                + "1 CONNECT HMD + BOTH CONTROLLERS\n"
                + "2 RAY-GRAB ORANGE CUBE WITH TRIGGER\n"
                + "3 MOVE >= 5 CM AND RELEASE\n"
                + "Simulator devices cannot produce PASS";
        }

        static void CreateHardwareGrabCube(
            Transform parent,
            Material material,
            EmVrStatusDisplay statusDisplay,
            EmVrTelemetryEmitter telemetry,
            EmVrAssistGroundingProvider grounding,
            out XRGrabInteractable grabInteractable,
            out EmVrLabObject labObject)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "XR Hardware Grab Cube";
            cube.transform.SetParent(parent, false);
            cube.transform.position = new Vector3(0f, 0.8f, 2.3f);
            cube.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            cube.GetComponent<Renderer>().sharedMaterial = material;

            Rigidbody rigidbody = cube.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.drag = 1f;
            rigidbody.angularDrag = 1f;

            labObject = cube.AddComponent<EmVrLabObject>();
            labObject.ConfigureIdentity(
                EmVrXrHardwareValidationProbe.ExpectedObjectId,
                "XrHardwareValidationCube",
                "orange real-hardware grab cube");

            EmVrDragFeedbackBridge feedback = cube.AddComponent<EmVrDragFeedbackBridge>();
            feedback.labObject = labObject;
            feedback.statusDisplay = statusDisplay;
            feedback.telemetryEmitter = telemetry;
            feedback.assistGrounding = grounding;

            grabInteractable = cube.AddComponent<XRGrabInteractable>();
            grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            EmVrXrGrabFeedbackBridge xrFeedback =
                cube.AddComponent<EmVrXrGrabFeedbackBridge>();
            xrFeedback.feedbackBridge = feedback;
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
            Vector3 lookTarget = camera != null
                ? camera.transform.position + new Vector3(0f, 1.6f, 0f)
                : new Vector3(0f, 1.6f, 0f);
            textObject.transform.rotation = Quaternion.LookRotation(
                textObject.transform.position - lookTarget,
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
