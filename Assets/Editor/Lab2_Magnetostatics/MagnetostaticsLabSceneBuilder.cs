using System;
using System.IO;
using ElectrostaticsLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MagnetostaticsLabSceneBuilder
{
    const string ScenePath = "Assets/Scenes/Lab2_Magnetostatics.unity";
    const string MaterialFolder = "Assets/MagnetostaticsLab/Materials";
    const string ProgressLogRelativePath = "tmp/magnetostatics-lab-build-progress.log";
    const int BaseTextFontSize = 48;
    const int HighResolutionTextFontSize = 384;

    static bool buildInProgress;

    [MenuItem("Tools/Magnetostatics Lab/Build Lab 2 Scene")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            const string message = "Exit Play Mode before generating the magnetostatics lab scene.";
            LogStep(message);
            EditorUtility.DisplayDialog("Magnetostatics Lab", message, "OK");
            return;
        }

        if (buildInProgress)
        {
            LogStep("Build request ignored because another build is already running.");
            return;
        }

        buildInProgress = true;
        ResetProgressLog();

        try
        {
            LogStep("Build started.");
            EnsureAssetFolder("Assets", "Scenes");
            EnsureAssetFolder("Assets", "MagnetostaticsLab");
            EnsureAssetFolder("Assets/MagnetostaticsLab", "Materials");

            Material floorMaterial = CreateMaterial("Lab Floor", new Color(0.09f, 0.1f, 0.11f, 1f), false);
            Material wallMaterial = CreateMaterial("Glass Wall", new Color(0.42f, 0.52f, 0.58f, 0.34f), true);
            Material tableMaterial = CreateMaterial("Matte Table", new Color(0.17f, 0.18f, 0.2f, 1f), false);
            Material stageMaterial = CreateMaterial("Visualization Cube", new Color(0.45f, 0.78f, 1f, 0.16f), true);
            Material textPanelMaterial = CreateMaterial("Text Panel", new Color(0.04f, 0.05f, 0.06f, 0.86f), true);
            Material padMaterial = CreateMaterial("Preset Pad", new Color(0.94f, 0.8f, 0.24f, 1f), false);
            Material highlightMaterial = CreateMaterial("Highlighted Preset Pad", new Color(1f, 0.08f, 0.03f, 1f), false);
            Material neutralMaterial = CreateMaterial("Neutral Current Source", new Color(0.82f, 0.84f, 0.86f, 1f), false);
            Material roiMaterial = CreateMaterial("Helmholtz ROI", new Color(1f, 0.08f, 0.03f, 0.28f), true);
            LogStep("Created or updated materials.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Lab2_Magnetostatics";
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.36f, 0.38f, 1f);

            CreateLighting();
            Camera desktopCamera = CreateCamera();
            CreateTopDownCamera();
            TryAddXRInteractionSetup();
            EnsureXRInteractionManager();
            EnsureSingleAudioListener();
            LogStep("Created lighting, cameras, and optional XR setup.");

            CreateRoom(floorMaterial, wallMaterial, tableMaterial);
            GameObject stage = CreateCube("Magnetic Field Visualization Cube", new Vector3(0f, 3.3f, 0f), new Vector3(11.6f, 6f, 3.6f), stageMaterial);
            BoxCollider stageCollider = stage.GetComponent<BoxCollider>();
            stageCollider.enabled = false;
            MagneticFieldVisualizer visualizer = stage.AddComponent<MagneticFieldVisualizer>();

            GameObject roi = CreateCube("Scene 5 Red ROI", new Vector3(0f, 3.3f, -1.82f), new Vector3(4.8f, 1.05f, 0.04f), roiMaterial);
            UnityEngine.Object.DestroyImmediate(roi.GetComponent<Collider>());
            LogStep("Created visualization cube and Helmholtz ROI marker.");

            GameObject root = new GameObject("Preset Magnetic Sources");
            GameObject controllerObject = new GameObject("Lab 2 Controller");
            MagneticLabController controller = controllerObject.AddComponent<MagneticLabController>();
            controller.fieldVisualizer = visualizer;
            controller.presetSourceRoot = root.transform;
            MagneticScenePresentation presentation = controllerObject.AddComponent<MagneticScenePresentation>();
            controllerObject.AddComponent<ElectrostaticXrRayFallback>();
            presentation.desktopCamera = desktopCamera;
            presentation.forceFrontOrthographicCamera = true;
            presentation.hideDeviceSimulatorUi = true;
            presentation.cameraPosition = new Vector3(0f, 3.65f, -16.5f);
            presentation.orthographicSize = 6.7f;
            presentation.legibleOrthographicSize = 5.6f;

            controller.statusLabel = CreateText("Status", "Energize source, place it in the cube, then observe B-lines.", new Vector3(0f, 6.86f, -2.7f), 0.052f, Color.white, TextAnchor.MiddleCenter);

            CreateInstructionBoards(textPanelMaterial);
            CreateCurrentLoaders(textPanelMaterial);
            CreateFreeSources(neutralMaterial);
            CreatePresetPads(controller, padMaterial, highlightMaterial, textPanelMaterial);
            CreateControlButtons(controller, padMaterial);
            LogStep("Created boards, loaders, source rack, and controls.");

            controller.ApplyPreset(5);
            LogStep("Applied default Helmholtz-like preset.");

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LogStep("Saved scene and assets.");

            Debug.Log("Built magnetostatics lab scene at " + ScenePath);
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Magnetostatics Lab", "Lab 2 scene generated:\n" + ScenePath, "OK");
            }
        }
        catch (Exception exception)
        {
            LogStep("Build failed: " + exception);
            Debug.LogException(exception);
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Magnetostatics Lab Build Failed", exception.Message, "OK");
            }
            throw;
        }
        finally
        {
            buildInProgress = false;
        }
    }

    [MenuItem("Tools/Magnetostatics Lab/Build Lab 2 Scene", true)]
    public static bool CanBuildScene()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode && !buildInProgress;
    }

    static void ResetProgressLog()
    {
        string path = ProjectPath(ProgressLogRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, string.Empty);
    }

    static void LogStep(string message)
    {
        string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " | " + message;
        Debug.Log("[MagnetostaticsLabSceneBuilder] " + message);

        try
        {
            string path = ProjectPath(ProgressLogRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.AppendAllText(path, line + Environment.NewLine);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Could not write magnetostatics lab progress log: " + exception.Message);
        }
    }

    static string ProjectPath(string relativePath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    static void EnsureAssetFolder(string parent, string child)
    {
        string folder = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    static void CreateLighting()
    {
        GameObject sun = new GameObject("Directional Light");
        Light sunLight = sun.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.intensity = 1.15f;
        sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

        GameObject area = new GameObject("Soft Lab Area Light");
        Light areaLight = area.AddComponent<Light>();
        areaLight.type = LightType.Area;
        areaLight.intensity = 360f;
        areaLight.range = 8f;
        areaLight.transform.position = new Vector3(0f, 5.2f, -1.4f);
        areaLight.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    static Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Desktop Preview Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<ElectrostaticMouseVrPointer>();
        camera.orthographic = true;
        camera.orthographicSize = 6.7f;
        camera.depth = 10f;
        camera.clearFlags = CameraClearFlags.Skybox;
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 3.65f, -16.5f);
        cameraObject.transform.rotation = Quaternion.identity;
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    static void CreateTopDownCamera()
    {
        GameObject cameraObject = new GameObject("Top Down Screenshot Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6.1f;
        camera.depth = -5f;
        camera.enabled = false;
        cameraObject.transform.position = new Vector3(0f, 12.5f, 0f);
        cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    static void EnsureSingleAudioListener()
    {
        AudioListener[] listeners = UnityEngine.Object.FindObjectsOfType<AudioListener>(true);
        AudioListener preferred = null;
        for (int i = 0; i < listeners.Length; i++)
        {
            if (listeners[i] != null && listeners[i].GetComponent<Camera>() != null && listeners[i].gameObject.CompareTag("MainCamera"))
            {
                preferred = listeners[i];
                break;
            }
        }

        if (preferred == null && listeners.Length > 0)
        {
            preferred = listeners[0];
        }

        for (int i = 0; i < listeners.Length; i++)
        {
            if (listeners[i] != null)
            {
                listeners[i].enabled = listeners[i] == preferred;
            }
        }
    }

    static void TryAddXRInteractionSetup()
    {
        string prefabPath = "Assets/Samples/XR Interaction Toolkit/2.6.5/Starter Assets/Prefabs/XR Interaction Setup.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            return;
        }

        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        rig.name = "XR Interaction Setup";
        rig.transform.position = new Vector3(0f, 0f, -10.8f);
        HideExistingDeviceSimulatorUi(rig);
    }

    static void EnsureXRInteractionManager()
    {
        Type type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRInteractionManager, Unity.XR.Interaction.Toolkit");
        if (type == null || UnityEngine.Object.FindObjectOfType(type) != null)
        {
            return;
        }

        GameObject manager = new GameObject("XR Interaction Manager");
        manager.AddComponent(type);
    }

    static void HideExistingDeviceSimulatorUi(GameObject root)
    {
        Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && IsDeviceSimulatorObject(canvases[i].gameObject))
            {
                canvases[i].gameObject.SetActive(false);
            }
        }
    }

    static bool IsDeviceSimulatorObject(GameObject candidate)
    {
        Transform current = candidate.transform;
        while (current != null)
        {
            if (current.name.Contains("XR Device Simulator"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    static void CreateRoom(Material floorMaterial, Material wallMaterial, Material tableMaterial)
    {
        CreateCube("Floor", new Vector3(0f, -0.03f, 0f), new Vector3(19f, 0.06f, 12.4f), floorMaterial);
        CreateCube("Back Glass Wall", new Vector3(0f, 3.7f, 2.56f), new Vector3(19f, 7.4f, 0.08f), wallMaterial);
        CreateCube("Left Glass Wall", new Vector3(-9.5f, 3.7f, -1.6f), new Vector3(0.08f, 7.4f, 8.4f), wallMaterial);
        CreateCube("Right Glass Wall", new Vector3(9.5f, 3.7f, -1.6f), new Vector3(0.08f, 7.4f, 8.4f), wallMaterial);
        CreateCube("Workbench", new Vector3(0f, 0.52f, -3.5f), new Vector3(16.4f, 0.16f, 2.2f), tableMaterial);

        for (int i = -9; i <= 9; i += 2)
        {
            CreateCube("Wall Grid X " + i, new Vector3(i, 3.7f, 2.5f), new Vector3(0.02f, 7.4f, 0.04f), wallMaterial);
        }

        for (int j = 0; j <= 8; j++)
        {
            CreateCube("Wall Grid Y " + j, new Vector3(0f, 0.45f + j * 0.8f, 2.49f), new Vector3(18.8f, 0.018f, 0.04f), wallMaterial);
        }
    }

    static void CreateInstructionBoards(Material textPanelMaterial)
    {
        string workflow =
            "Workflow\n" +
            "1 Drag source\n" +
            "2 Set current\n" +
            "3 Place in cube";
        CreatePanel("Workflow Panel", new Vector3(-6.65f, 5.55f, -2.55f), new Vector2(4.0f, 1.45f), textPanelMaterial);
        CreateText("Workflow Board", workflow, new Vector3(-8.42f, 5.88f, -2.64f), 0.053f, Color.white, TextAnchor.MiddleLeft);

        string models =
            "Models\n" +
            "Wire: B curls\n" +
            "Coil: loop field\n" +
            "B_total: sum";
        CreatePanel("Models Panel", new Vector3(6.65f, 5.55f, -2.55f), new Vector2(4.0f, 1.45f), textPanelMaterial);
        CreateText("Models Board", models, new Vector3(4.9f, 5.88f, -2.64f), 0.053f, Color.white, TextAnchor.MiddleLeft);

        string report =
            "Report\n" +
            "Q1-Q4 sources\n" +
            "Q5 coil pair\n" +
            "Q6 Helmholtz";
        CreatePanel("Report Panel", new Vector3(0f, 5.55f, -2.55f), new Vector2(4.4f, 1.45f), textPanelMaterial);
        CreateText("Report Board", report, new Vector3(-1.95f, 5.88f, -2.64f), 0.051f, Color.white, TextAnchor.MiddleLeft);
        CreateText("Stage Label", "Magnetic field stage", new Vector3(0f, 6.22f, -0.98f), 0.056f, new Color(0.74f, 0.92f, 1f, 1f), TextAnchor.MiddleCenter);
    }

    static void CreateCurrentLoaders(Material textPanelMaterial)
    {
        CreatePanel("Current Loader Label Panel", new Vector3(-7.1f, 3.55f, -1.85f), new Vector2(2.1f, 0.42f), textPanelMaterial);
        CreateText("Current Loader Label", "Current loader", new Vector3(-8.05f, 3.56f, -1.93f), 0.052f, Color.white, TextAnchor.MiddleLeft);

        for (int i = 1; i <= 6; i++)
        {
            CreateLoaderBar(-8.65f + i * 0.36f, i, new Vector3(-7.2f, 2.9f + i * 0.08f, -0.2f));
            CreateLoaderBar(-6.55f + i * 0.36f, -i, new Vector3(-6.3f, 2.9f + i * 0.08f, -0.2f));
        }
    }

    static void CreateLoaderBar(float x, int level, Vector3 unused)
    {
        Color color = MagneticCurrentSource.CurrentColor(level);
        Material material = CreateMaterial("Current Loader " + level, color, false);
        float height = 0.72f + Mathf.Abs(level) * 0.12f;
        GameObject bar = CreateCube("Current Loader " + level, new Vector3(x, 1.2f + height * 0.5f, -4.65f), new Vector3(0.24f, height, 0.42f), material);
        MagneticCurrentEnergizer energizer = bar.AddComponent<MagneticCurrentEnergizer>();
        energizer.assignedLevel = level;
        energizer.effectiveRadius = 0.65f;
        bar.GetComponent<BoxCollider>().isTrigger = true;
        TextMesh label = CreateText("Current Loader Label " + level, (level > 0 ? "+" : string.Empty) + level, new Vector3(x, 0.74f, -4.96f), 0.046f, Color.white, TextAnchor.MiddleCenter);
        energizer.label = label;
        TextMesh hover = CreateText("Current Loader Hover " + level, "Set I = " + (level > 0 ? "+" : string.Empty) + level, new Vector3(x, 2.35f, -5.02f), 0.064f, Color.white, TextAnchor.MiddleCenter);
        AddHoverAnnotation(bar, hover, new Vector3(0f, 1.1f, -0.35f));
    }

    static void CreateFreeSources(Material neutralMaterial)
    {
        CreateFreeSource("Free Straight Wire A", MagneticSourceType.StraightWire, new Vector3(5.7f, 1.75f, -4.65f), Quaternion.identity, neutralMaterial, "Straight wire\nDrag into loader, then cube.");
        CreateFreeSource("Free Straight Wire B", MagneticSourceType.StraightWire, new Vector3(6.55f, 1.75f, -4.65f), Quaternion.identity, neutralMaterial, "Straight wire\nHold R while dragging to rotate.");
        CreateFreeSource("Free Solenoid A", MagneticSourceType.Solenoid, new Vector3(7.45f, 1.75f, -4.65f), Quaternion.identity, neutralMaterial, "Solenoid\nFinite coil field loops.");
        CreateFreeSource("Free Solenoid B", MagneticSourceType.Solenoid, new Vector3(8.35f, 1.75f, -4.65f), Quaternion.identity, neutralMaterial, "Solenoid\nUse pairs for Q5/Q6.");
    }

    static MagneticCurrentSource CreateFreeSource(string objectName, MagneticSourceType sourceType, Vector3 position, Quaternion rotation, Material neutralMaterial, string annotationText)
    {
        GameObject root = CreateSourceRoot(objectName, sourceType, position, rotation, neutralMaterial);
        Rigidbody rigidbody = root.AddComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.drag = 8f;
        rigidbody.angularDrag = 8f;
        root.AddComponent<MagneticDesktopDraggable>();
        AddXRGrabInteractable(root);

        MagneticCurrentSource source = root.AddComponent<MagneticCurrentSource>();
        source.sourceType = sourceType;
        source.displayName = sourceType == MagneticSourceType.StraightWire ? "Straight wire" : "Solenoid";
        source.currentLevel = 0;
        source.currentLabel = CreateText(objectName + " Label", annotationText, position + new Vector3(0f, 1.35f, -0.38f), 0.072f, Color.white, TextAnchor.MiddleCenter);
        source.currentLabel.transform.SetParent(root.transform, true);
        source.UpdateVisuals();
        AddHoverAnnotation(root, source.currentLabel, new Vector3(0f, 1.35f, -0.38f));
        return source;
    }

    static GameObject CreateSourceRoot(string objectName, MagneticSourceType sourceType, Vector3 position, Quaternion rotation, Material material)
    {
        GameObject root = new GameObject(objectName);
        root.transform.position = position;
        root.transform.rotation = rotation;

        if (sourceType == MagneticSourceType.StraightWire)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "Straight Wire Body";
            cylinder.transform.SetParent(root.transform, false);
            cylinder.transform.localScale = new Vector3(0.13f, 1.5f, 0.13f);
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.radius = 0.18f;
            collider.height = 3.15f;
            collider.direction = 1;
        }
        else
        {
            CreateHelix(root.transform);
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.radius = 0.72f;
            collider.height = 2.65f;
            collider.direction = 1;
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(1.45f, 2.65f, 1.1f);
        }

        return root;
    }

    static void CreateHelix(Transform parent)
    {
        GameObject lineObject = new GameObject("Solenoid Helix");
        lineObject.transform.SetParent(parent, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 120;
        line.startWidth = 0.055f;
        line.endWidth = 0.055f;
        line.numCapVertices = 4;
        Shader shader = Shader.Find("Sprites/Default");
        line.sharedMaterial = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Unlit"));

        for (int i = 0; i < line.positionCount; i++)
        {
            float t = i / (float)(line.positionCount - 1);
            float angle = t * Mathf.PI * 2f * 8f;
            float y = Mathf.Lerp(-1.1f, 1.1f, t);
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.46f, y, Mathf.Sin(angle) * 0.46f));
        }
    }

    static void CreatePresetPads(MagneticLabController controller, Material padMaterial, Material highlightMaterial, Material textPanelMaterial)
    {
        string[] labels =
        {
            "Scene 1\nTwo wires",
            "Scene 2\nWire + coil",
            "Scene 3\nThree wires",
            "Scene 4\nCoil pair",
            "Scene 5\nHelmholtz"
        };

        for (int i = 0; i < labels.Length; i++)
        {
            bool highlighted = i == 0 || i == 3 || i == 4;
            float x = -5.6f + i * 2.8f;
            GameObject pad = CreateCube("Magnetic Preset Pad " + (i + 1), new Vector3(x, 1.15f, -5.15f), highlighted ? new Vector3(1.78f, 0.2f, 0.78f) : new Vector3(1.5f, 0.16f, 0.66f), highlighted ? highlightMaterial : padMaterial);
            MagneticPresetPad presetPad = pad.AddComponent<MagneticPresetPad>();
            presetPad.controller = controller;
            presetPad.presetId = i + 1;
            AddXRSimpleInteractable(pad);
            CreateText("Magnetic Preset Number " + (i + 1), (i + 1).ToString(), new Vector3(x, 1.36f, -5.28f), highlighted ? 0.125f : 0.105f, highlighted ? Color.white : Color.black, TextAnchor.MiddleCenter);
            TextMesh hoverText = CreateText("Magnetic Preset Annotation " + (i + 1), labels[i], new Vector3(x, 1.72f, -5.5f), 0.066f, Color.white, TextAnchor.MiddleCenter);
            AddHoverAnnotation(pad, hoverText, new Vector3(0f, 0.5f, -0.35f));
        }

        CreatePanel("Magnetic Preset Help Panel", new Vector3(0f, -0.08f, -5.75f), new Vector2(7.4f, 0.34f), textPanelMaterial);
        CreateText("Magnetic Preset Help", "1-5 presets | C clear | F lines | V samples | X flip | +/- current", new Vector3(0f, -0.08f, -5.82f), 0.048f, Color.white, TextAnchor.MiddleCenter);
    }

    static void CreateControlButtons(MagneticLabController controller, Material padMaterial)
    {
        CreateLabButton("Clear Button", "C", "Clear presets", new Vector3(-4.5f, 0.36f, -6.15f), MagneticLabButton.LabButtonAction.ClearSources, controller, padMaterial);
        CreateLabButton("Field Lines Button", "F", "Field lines", new Vector3(-2.7f, 0.36f, -6.15f), MagneticLabButton.LabButtonAction.ToggleFieldLines, controller, padMaterial);
        CreateLabButton("Vectors Button", "V", "Direction samples", new Vector3(-0.9f, 0.36f, -6.15f), MagneticLabButton.LabButtonAction.ToggleVectors, controller, padMaterial);
        CreateLabButton("Flip Button", "X", "Flip currents", new Vector3(0.9f, 0.36f, -6.15f), MagneticLabButton.LabButtonAction.FlipCurrents, controller, padMaterial);
        CreateLabButton("Minus Button", "-", "Decrease current", new Vector3(2.7f, 0.36f, -6.15f), MagneticLabButton.LabButtonAction.DecreaseCurrent, controller, padMaterial);
        CreateLabButton("Plus Button", "+", "Increase current", new Vector3(4.5f, 0.36f, -6.15f), MagneticLabButton.LabButtonAction.IncreaseCurrent, controller, padMaterial);
    }

    static void CreateLabButton(string objectName, string faceText, string hoverText, Vector3 position, MagneticLabButton.LabButtonAction action, MagneticLabController controller, Material padMaterial)
    {
        GameObject button = CreateCube(objectName, position, new Vector3(1.12f, 0.14f, 0.58f), padMaterial);
        MagneticLabButton labButton = button.AddComponent<MagneticLabButton>();
        labButton.controller = controller;
        labButton.action = action;
        AddXRSimpleInteractable(button);

        CreateText(objectName + " Label", faceText, position + new Vector3(0f, 0.17f, -0.13f), 0.072f, Color.black, TextAnchor.MiddleCenter);
        TextMesh annotation = CreateText(objectName + " Annotation", hoverText, position + new Vector3(0f, 0.5f, -0.35f), 0.066f, Color.white, TextAnchor.MiddleCenter);
        AddHoverAnnotation(button, annotation, new Vector3(0f, 0.46f, -0.35f));
    }

    static GameObject CreateCube(string objectName, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.position = position;
        cube.transform.localScale = scale;
        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        return cube;
    }

    static GameObject CreatePanel(string objectName, Vector3 position, Vector2 size, Material material)
    {
        GameObject panel = CreateCube(objectName, position, new Vector3(size.x, size.y, 0.04f), material);
        Collider collider = panel.GetComponent<Collider>();
        if (collider != null)
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }

        return panel;
    }

    static void AddHoverAnnotation(GameObject target, TextMesh annotation, Vector3 worldOffset)
    {
        ElectrostaticHoverAnnotation hoverAnnotation = target.GetComponent<ElectrostaticHoverAnnotation>();
        if (hoverAnnotation == null)
        {
            hoverAnnotation = target.AddComponent<ElectrostaticHoverAnnotation>();
        }

        hoverAnnotation.annotation = annotation;
        hoverAnnotation.worldOffset = worldOffset;

        if (annotation != null)
        {
            annotation.gameObject.SetActive(false);
        }
    }

    static TextMesh CreateText(string objectName, string text, Vector3 position, float characterSize, Color color, TextAnchor anchor)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.position = position;
        textObject.transform.rotation = Quaternion.identity;

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.characterSize = characterSize * BaseTextFontSize / HighResolutionTextFontSize;
        textMesh.fontSize = HighResolutionTextFontSize;
        textMesh.anchor = anchor;
        textMesh.alignment = anchor == TextAnchor.MiddleLeft ? TextAlignment.Left : TextAlignment.Center;
        textMesh.color = color;
        if (textMesh.font != null && textMesh.font.material != null && textMesh.font.material.mainTexture != null)
        {
            textMesh.font.RequestCharactersInTexture(text, HighResolutionTextFontSize, FontStyle.Normal);
            textMesh.font.material.mainTexture.filterMode = FilterMode.Trilinear;
            textMesh.font.material.mainTexture.anisoLevel = 8;
        }

        return textMesh;
    }

    static Material CreateMaterial(string materialName, Color color, bool transparent)
    {
        string path = MaterialFolder + "/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;

        if (transparent)
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
        else
        {
            material.SetFloat("_Mode", 0f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    static void AddXRGrabInteractable(GameObject target)
    {
        Type type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
        if (type != null && target.GetComponent(type) == null)
        {
            target.AddComponent(type);
        }
    }

    static void AddXRSimpleInteractable(GameObject target)
    {
        Type type = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRSimpleInteractable, Unity.XR.Interaction.Toolkit");
        if (type != null && target.GetComponent(type) == null)
        {
            target.AddComponent(type);
        }
    }

    static void AddSceneToBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        for (int i = 0; i < current.Length; i++)
        {
            if (current[i].path == scenePath)
            {
                current[i].enabled = true;
                EditorBuildSettings.scenes = current;
                return;
            }
        }

        EditorBuildSettingsScene[] next = new EditorBuildSettingsScene[current.Length + 1];
        Array.Copy(current, next, current.Length);
        next[next.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = next;
    }
}
