using System.Collections.Generic;
using EMVR.Common.Assist;
using EMVR.Common.Editor.Validation;
using EMVR.Common.Objects;
using EMVR.Common.SceneFlow;
using EMVR.Common.Telemetry;
using EMVR.Common.UI;
using EMVR.Common.Validation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EMVR.Common.Editor.Samples
{
    /// <summary>
    /// Generates the third Common smoke-test scene. Runtime behavior is driven
    /// by the same contract runner intended for generated labs.
    /// </summary>
    public static class EmVrLabContractValidationSceneBuilder
    {
        public const string ScenePath =
            "Assets/Scenes/EMVR_Common_LabContractValidation.unity";

        const string BuildMenuPath =
            "Tools/EMVR/Common/Build Lab Contract Validation Scene";
        const string OpenMenuPath =
            "Tools/EMVR/Common/Open Lab Contract Validation Scene";
        const string GeneratedRoot = "Assets/EMVRCommonValidation";
        const string MaterialFolder = GeneratedRoot + "/Materials";

        [MenuItem(BuildMenuPath)]
        public static void BuildScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "EMVR Common",
                    "Exit Play Mode before building the lab-contract validation scene.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                && !EditorUtility.DisplayDialog(
                    "Rebuild lab-contract validation scene?",
                    ScenePath + " already exists. Rebuilding will replace the generated scene.",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            EnsureGeneratedFolders();
            Material groundMaterial = GetOrCreateMaterial(
                MaterialFolder + "/ContractGround.mat",
                new Color(0.10f, 0.15f, 0.22f, 1f));
            Material objectMaterial = GetOrCreateMaterial(
                MaterialFolder + "/ContractObject.mat",
                new Color(1f, 0.38f, 0.08f, 1f));
            Material firstStepMaterial = GetOrCreateMaterial(
                MaterialFolder + "/ContractStepOne.mat",
                new Color(0.08f, 0.72f, 0.92f, 1f));
            Material secondStepMaterial = GetOrCreateMaterial(
                MaterialFolder + "/ContractStepTwo.mat",
                new Color(0.18f, 0.86f, 0.48f, 1f));

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            GameObject root = new GameObject("EMVR Common Lab Contract Validation");
            Camera camera = CreateCamera(root.transform);
            CreateLighting(root.transform);
            CreateEnvironment(
                root.transform,
                groundMaterial,
                firstStepMaterial,
                secondStepMaterial);

            EmVrStatusDisplay statusDisplay = CreateStatusDisplay(root.transform, camera);
            CreateInstructionText(root.transform, camera);
            EmVrLabObject labObject = CreateContractObject(
                root.transform,
                objectMaterial);

            GameObject systemsObject = new GameObject("Lab Contract Validation Systems");
            systemsObject.transform.SetParent(root.transform, false);
            EmVrTelemetryEmitter telemetry =
                systemsObject.AddComponent<EmVrTelemetryEmitter>();
            telemetry.historyCapacity = 32;
            telemetry.logToConsole = false;

            EmVrAssistGroundingProvider grounding =
                systemsObject.AddComponent<EmVrAssistGroundingProvider>();
            grounding.explicitObjectSources = new MonoBehaviour[] { labObject };
            grounding.discoverObjectSourcesInChildren = false;
            grounding.telemetryEmitter = telemetry;
            grounding.diagnosticHintsJson = "{\"source\":\"contract_validation\"}";
            grounding.triggerReasonJson = "{\"reason\":\"automated_smoke_test\"}";
            grounding.stepRubricJson = "{\"requires_ordered_events\":true}";
            grounding.sessionMemoryJson = "{\"attempt\":1}";

            EmVrLabContractRunner runner =
                systemsObject.AddComponent<EmVrLabContractRunner>();
            runner.sessionId = "contract-validation-session";
            runner.userId = "local-validator";
            runner.labId = EmVrLabContractValidationProbe.ExpectedLabId;
            runner.mode = "automated_validation";
            runner.steps = new[]
            {
                new EmVrLabContractRunner.StepDefinition
                {
                    stepId = EmVrLabContractValidationProbe.FirstStepId,
                    displayName = "Select and drag the contract object",
                    requiredObjectId = EmVrLabContractValidationProbe.ExpectedObjectId,
                    requireSelection = true,
                    requireCompletedDrag = true
                },
                new EmVrLabContractRunner.StepDefinition
                {
                    stepId = EmVrLabContractValidationProbe.SecondStepId,
                    displayName = "Confirm the result",
                    requiredObjectId = string.Empty,
                    requireSelection = false,
                    requireCompletedDrag = false
                }
            };
            runner.knownObjects = new[] { labObject };
            runner.telemetryEmitter = telemetry;
            runner.assistGrounding = grounding;
            runner.statusDisplay = statusDisplay;
            runner.clearTelemetryOnStart = true;

            EmVrLabContractValidationProbe probe =
                systemsObject.AddComponent<EmVrLabContractValidationProbe>();
            probe.contractRunner = runner;
            probe.labObject = labObject;
            probe.telemetryEmitter = telemetry;
            probe.assistGrounding = grounding;
            probe.statusDisplay = statusDisplay;
            probe.stepDelaySeconds = 0.55f;
            probe.runOnStart = true;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError(
                    "[EMVR Common] Could not save lab-contract validation scene: "
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
                    "[EMVR Common] Lab-contract validation scene built and "
                    + "edit-time validation passed: " + ScenePath);
            }
            else
            {
                Debug.LogWarning(
                    "[EMVR Common] Lab-contract scene built with " + issues.Count
                    + " validation issue(s). Run Tools > EMVR > Common > Validate Active Scene.");
            }

            EditorUtility.DisplayDialog(
                "EMVR Common",
                "Lab-contract validation scene created.\n\n"
                + "1. Wait for compilation to finish.\n"
                + "2. Enter Play Mode.\n"
                + "3. Watch the object move through the two-step contract.\n"
                + "4. Confirm the status panel shows PASS and Console contains "
                + "[EMVR Lab Contract Validation] PASS.",
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
                    "The lab-contract validation scene does not exist yet. Run the build menu first.",
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
                new Vector3(0f, 1.15f, 1f) - cameraObject.transform.position,
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
            Material firstStepMaterial,
            Material secondStepMaterial)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(parent, false);
            ground.transform.position = new Vector3(0f, -0.15f, 1f);
            ground.transform.localScale = new Vector3(9f, 0.3f, 7f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            CreateStepPad(
                "Step 1 - Select and Drag",
                parent,
                new Vector3(-1.6f, 0.02f, 1f),
                firstStepMaterial);
            CreateStepPad(
                "Step 2 - Confirm Result",
                parent,
                new Vector3(1.7f, 0.02f, 1.6f),
                secondStepMaterial);
        }

        static void CreateStepPad(
            string name,
            Transform parent,
            Vector3 position,
            Material material)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            marker.transform.localScale = new Vector3(1.15f, 0.025f, 1.15f);
            marker.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(marker.GetComponent<Collider>());
        }

        static EmVrStatusDisplay CreateStatusDisplay(Transform parent, Camera camera)
        {
            TextMesh text = CreateWorldText(
                "Status Panel",
                parent,
                new Vector3(-4.2f, 3.8f, 1.35f),
                camera,
                TextAnchor.UpperLeft,
                TextAlignment.Left,
                new Color(0.3f, 1f, 0.72f, 1f),
                0.034f,
                50);
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
                new Vector3(-0.1f, 3.8f, 1.5f),
                camera,
                TextAnchor.UpperLeft,
                TextAlignment.Left,
                Color.white,
                0.038f,
                54);
            instruction.text = "EMVR COMMON - LAB CONTRACT VALIDATION\n"
                + "Automatic: reject missing ID -> start lab -> step 1 -> step 2 -> complete\n"
                + "Checks canonical telemetry order and step-synchronized assist grounding";
        }

        static EmVrLabObject CreateContractObject(Transform parent, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Contract Validation Object";
            cube.transform.SetParent(parent, false);
            cube.transform.position = new Vector3(-1.6f, 0.72f, 1f);
            cube.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
            cube.GetComponent<Renderer>().sharedMaterial = material;

            EmVrLabObject labObject = cube.AddComponent<EmVrLabObject>();
            labObject.ConfigureIdentity(
                EmVrLabContractValidationProbe.ExpectedObjectId,
                "ContractValidationObject",
                "orange contract validation object");
            return labObject;
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
