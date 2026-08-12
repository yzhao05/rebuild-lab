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
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

namespace EMVR.Common.Editor.Samples
{
    /// <summary>
    /// Creates a small, inspectable scene that verifies package installation and
    /// the pointer -> drag -> status -> telemetry -> assist grounding path.
    /// </summary>
    public static class EmVrMinimalValidationSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/EMVR_Common_MinimalValidation.unity";

        const string BuildMenuPath = "Tools/EMVR/Common/Build Minimal Validation Scene";
        const string OpenMenuPath = "Tools/EMVR/Common/Open Minimal Validation Scene";
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
                    "Exit Play Mode before building the minimal validation scene.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                && !EditorUtility.DisplayDialog(
                    "Rebuild minimal validation scene?",
                    ScenePath + " already exists. Rebuilding will replace the generated scene.",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            EnsureGeneratedFolders();
            Material groundMaterial = GetOrCreateMaterial(
                MaterialFolder + "/Ground.mat",
                new Color(0.13f, 0.18f, 0.24f, 1f));
            Material cubeMaterial = GetOrCreateMaterial(
                MaterialFolder + "/DraggableCube.mat",
                new Color(1f, 0.42f, 0.08f, 1f));
            Material markerMaterial = GetOrCreateMaterial(
                MaterialFolder + "/PlaneMarker.mat",
                new Color(0.12f, 0.72f, 0.78f, 0.45f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("EMVR Common Minimal Validation");

            Camera camera = CreateCamera(root.transform);
            CreateLighting(root.transform);

            Camera xrCamera;
            AudioListener xrAudioListener;
            bool hasXrRig = CreateXrValidationRig(
                root.transform,
                out xrCamera,
                out xrAudioListener);
            if (!hasXrRig)
            {
                CreateEventSystem(root.transform);
            }

            Transform dragPlaneAnchor = CreateEnvironment(
                root.transform,
                groundMaterial,
                markerMaterial);

            EmVrStatusDisplay statusDisplay = CreateStatusDisplay(root.transform, camera);
            CreateInstructionText(root.transform, camera);

            GameObject systemsObject = new GameObject("Validation Systems");
            systemsObject.transform.SetParent(root.transform, false);
            EmVrTelemetryEmitter telemetry = systemsObject.AddComponent<EmVrTelemetryEmitter>();
            telemetry.logToConsole = false;
            EmVrAssistGroundingProvider grounding = systemsObject.AddComponent<EmVrAssistGroundingProvider>();
            grounding.sessionId = "minimal-validation-session";
            grounding.userId = "local-validator";
            grounding.labId = EmVrMinimalValidationProbe.ExpectedLabId;
            grounding.stepId = "drag_cube";
            grounding.mode = "manual_validation";
            grounding.telemetryEmitter = telemetry;

            EmVrMousePointer pointer = camera.gameObject.AddComponent<EmVrMousePointer>();
            pointer.fallbackPlaneNormal = Vector3.up;
            pointer.fallbackPlaneAnchor = dragPlaneAnchor;
            pointer.maxDistance = 30f;
            pointer.pointerRadius = 0.08f;
            pointer.rayStartOffset = 0.05f;

            EmVrPlanarMouseDraggable draggable;
            XRGrabInteractable xrGrabInteractable;
            EmVrLabObject labObject;
            CreateDraggableCube(
                root.transform,
                dragPlaneAnchor,
                cubeMaterial,
                statusDisplay,
                telemetry,
                grounding,
                out draggable,
                out xrGrabInteractable,
                out labObject);

            grounding.explicitObjectSources = new MonoBehaviour[] { labObject };
            grounding.selectedObjectSource = labObject;

            EmVrMinimalValidationProbe probe = systemsObject.AddComponent<EmVrMinimalValidationProbe>();
            probe.mousePointer = pointer;
            probe.draggable = draggable;
            probe.xrGrabInteractable = xrGrabInteractable;
            probe.labObject = labObject;
            probe.statusDisplay = statusDisplay;
            probe.telemetryEmitter = telemetry;
            probe.assistGrounding = grounding;

            EmVrMinimalValidationViewMode viewMode =
                systemsObject.AddComponent<EmVrMinimalValidationViewMode>();
            viewMode.desktopCamera = camera;
            viewMode.desktopPointer = pointer;
            viewMode.desktopDraggable = draggable;
            viewMode.desktopAudioListener = camera.GetComponent<AudioListener>();
            viewMode.xrCamera = xrCamera;
            viewMode.xrAudioListener = xrAudioListener;
            viewMode.statusDisplay = statusDisplay;
            viewMode.startInDesktopView = true;

            EmVrXrValidationDiagnostics xrDiagnostics =
                systemsObject.AddComponent<EmVrXrValidationDiagnostics>();
            xrDiagnostics.viewMode = viewMode;
            xrDiagnostics.statusDisplay = statusDisplay;

            EmVrXrRayRuntimeTuner xrRayTuner =
                systemsObject.AddComponent<EmVrXrRayRuntimeTuner>();
            xrRayTuner.sphereCastRadius = 0.06f;
            xrRayTuner.minimumRayDistance = 20f;
            xrRayTuner.controllerRaysOnly = true;
            xrRayTuner.enableMouseLeftDebugSelect = true;
            xrRayTuner.xrViewStateSource = viewMode;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("[EMVR Common] Could not save minimal validation scene: " + ScenePath);
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = root;

            List<EmVrSceneValidator.ValidationIssue> issues = EmVrSceneValidator.ValidateActiveScene();
            if (issues.Count == 0)
            {
                Debug.Log(
                    "[EMVR Common] Minimal validation scene built and edit-time validation passed: "
                    + ScenePath);
            }
            else
            {
                Debug.LogWarning(
                    "[EMVR Common] Scene built with " + issues.Count
                    + " validation issue(s). Run Tools > EMVR > Common > Validate Active Scene.");
            }

            EditorUtility.DisplayDialog(
                "EMVR Common",
                "Minimal validation scene created.\n\n"
                + "1. Wait for compilation to finish.\n"
                + "2. Enter Play Mode.\n"
                + "3. F1 is the monoscopic desktop view; drag the orange cube with the left mouse button.\n"
                + "4. F2 switches to the XR headset view; controller models and cyan rays remain active.\n"
                + "5. Confirm the status panel shows PASS and Console contains "
                + "[EMVR Minimal Validation] PASS.",
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
                    "The validation scene does not exist yet. Run the Build Minimal Validation Scene menu first.",
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
            cameraObject.transform.position = new Vector3(0f, 5f, -9f);
            cameraObject.transform.rotation = Quaternion.LookRotation(
                new Vector3(0f, 1.2f, 1f) - cameraObject.transform.position,
                Vector3.up);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.055f, 1f);
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        static bool CreateXrValidationRig(
            Transform parent,
            out Camera xrCamera,
            out AudioListener xrAudioListener)
        {
            xrCamera = null;
            xrAudioListener = null;

            GameObject setupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrSetupPrefabPath);
            if (setupPrefab == null)
            {
                Debug.LogWarning(
                    "[EMVR Common] XRI Starter Assets were not found at " + XrSetupPrefabPath
                    + ". The scene will keep desktop validation but cannot show XR controllers.");
                return false;
            }

            GameObject setup = PrefabUtility.InstantiatePrefab(setupPrefab, parent) as GameObject;
            if (setup == null)
            {
                Debug.LogWarning("[EMVR Common] Could not instantiate the XRI validation setup.");
                return false;
            }

            setup.name = "XR Validation Rig (active; F2 headset view)";
            xrCamera = setup.GetComponentInChildren<Camera>(true);
            if (xrCamera != null)
            {
                xrCamera.enabled = false;
                xrAudioListener = xrCamera.GetComponent<AudioListener>();
                if (xrAudioListener != null)
                {
                    xrAudioListener.enabled = false;
                }
            }

            return true;
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

        static void CreateEventSystem(Transform parent)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.transform.SetParent(parent, false);
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        static Transform CreateEnvironment(
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

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Drag Plane Marker";
            marker.transform.SetParent(parent, false);
            marker.transform.position = new Vector3(0f, 0.015f, 1f);
            marker.transform.localScale = new Vector3(2.4f, 0.025f, 2.4f);
            marker.GetComponent<Renderer>().sharedMaterial = markerMaterial;
            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Object.DestroyImmediate(markerCollider);
            }

            GameObject anchorObject = new GameObject("Drag Plane Anchor");
            anchorObject.transform.SetParent(parent, false);
            anchorObject.transform.position = new Vector3(0f, 0.7f, 1f);
            return anchorObject.transform;
        }

        static EmVrStatusDisplay CreateStatusDisplay(Transform parent, Camera camera)
        {
            TextMesh statusText = CreateWorldText(
                "Status Panel",
                parent,
                new Vector3(-4.1f, 3.5f, 1.4f),
                camera,
                TextAnchor.UpperLeft,
                TextAlignment.Left,
                new Color(0.3f, 1f, 0.72f, 1f),
                0.045f,
                58);
            statusText.text = "scene: waiting";

            EmVrStatusDisplay display = statusText.gameObject.AddComponent<EmVrStatusDisplay>();
            display.worldText = statusText;
            display.emptyMessage = "scene: waiting";
            return display;
        }

        static void CreateInstructionText(Transform parent, Camera camera)
        {
            TextMesh instruction = CreateWorldText(
                "Instructions",
                parent,
                new Vector3(0.2f, 3.65f, 1.5f),
                camera,
                TextAnchor.UpperLeft,
                TextAlignment.Left,
                Color.white,
                0.045f,
                58);
            instruction.text = "EMVR COMMON - MINIMAL VALIDATION\n"
                + "F1 DESKTOP: hold LEFT MOUSE and drag the ORANGE CUBE\n"
                + "F2 XR: Y right hand, Q/E or UP/DOWN height, LEFT MOUSE grab";
        }

        static void CreateDraggableCube(
            Transform parent,
            Transform dragPlaneAnchor,
            Material cubeMaterial,
            EmVrStatusDisplay statusDisplay,
            EmVrTelemetryEmitter telemetry,
            EmVrAssistGroundingProvider grounding,
            out EmVrPlanarMouseDraggable draggable,
            out XRGrabInteractable xrGrabInteractable,
            out EmVrLabObject labObject)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Validation Draggable Cube";
            cube.transform.SetParent(parent, false);
            cube.transform.position = dragPlaneAnchor.position;
            cube.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
            cube.GetComponent<Renderer>().sharedMaterial = cubeMaterial;

            Rigidbody rigidbody = cube.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            labObject = cube.AddComponent<EmVrLabObject>();
            labObject.ConfigureIdentity(
                EmVrMinimalValidationProbe.ExpectedObjectId,
                "ValidationCube",
                "orange validation cube");

            draggable = cube.AddComponent<EmVrPlanarMouseDraggable>();
            draggable.planeNormal = Vector3.up;
            draggable.planeAnchor = dragPlaneAnchor;
            draggable.handleUnityMouseMessages = true;

            EmVrDragFeedbackBridge feedback = cube.AddComponent<EmVrDragFeedbackBridge>();
            feedback.labObject = labObject;
            feedback.statusDisplay = statusDisplay;
            feedback.telemetryEmitter = telemetry;
            feedback.assistGrounding = grounding;

            xrGrabInteractable = cube.AddComponent<XRGrabInteractable>();
            xrGrabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            EmVrXrGrabFeedbackBridge xrFeedback = cube.AddComponent<EmVrXrGrabFeedbackBridge>();
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
                    throw new System.InvalidOperationException("No supported lit shader is available.");
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
