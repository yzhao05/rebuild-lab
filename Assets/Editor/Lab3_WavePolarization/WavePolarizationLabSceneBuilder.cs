using System;
using System.IO;
using ElectrostaticsLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WavePolarizationLabSceneBuilder
{
    const string ScenePath = "Assets/Scenes/Lab3_WavePolarization.unity";
    const string MaterialFolder = "Assets/WavePolarizationLab/Materials";
    const string ProgressLogRelativePath = "tmp/wave-polarization-lab-build-progress.log";
    const int BaseTextFontSize = 48;
    const int HighResolutionTextFontSize = 384;

    static bool buildInProgress;

    [MenuItem("Tools/Wave Polarization Lab/Build Lab 3 Scene")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            const string message = "Exit Play Mode before generating the wave polarization lab scene.";
            LogStep(message);
            EditorUtility.DisplayDialog("Wave Polarization Lab", message, "OK");
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
            EnsureAssetFolder("Assets", "WavePolarizationLab");
            EnsureAssetFolder("Assets/WavePolarizationLab", "Materials");

            Material floorMaterial = CreateMaterial("Lab Floor", new Color(0.08f, 0.09f, 0.1f, 1f), false);
            Material wallMaterial = CreateMaterial("Glass Wall", new Color(0.42f, 0.52f, 0.58f, 0.34f), true);
            Material tableMaterial = CreateMaterial("Matte Table", new Color(0.16f, 0.17f, 0.19f, 1f), false);
            Material stageMaterial = CreateMaterial("Observation Stage", new Color(0.36f, 0.78f, 1f, 0.13f), true);
            Material panelMaterial = CreateMaterial("Text Panel", new Color(0.04f, 0.05f, 0.06f, 0.86f), true);
            Material padMaterial = CreateMaterial("Preset Pad", new Color(0.94f, 0.8f, 0.24f, 1f), false);
            Material controlMaterial = CreateMaterial("Control Pad", new Color(0.7f, 0.84f, 0.9f, 1f), false);
            Material sourceMaterial = CreateMaterial("Source Arrow", new Color(0.45f, 0.92f, 1f, 1f), false);
            LogStep("Created or updated materials.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Lab3_WavePolarization";
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.36f, 0.38f, 1f);

            CreateLighting();
            Camera desktopCamera = CreateCamera();
            CreateTopDownCamera();
            TryAddXRInteractionSetup();
            EnsureXRInteractionManager();
            EnsureSingleAudioListener();

            CreateRoom(floorMaterial, wallMaterial, tableMaterial);
            CreateInstructionBoards(panelMaterial);
            CreateSourceAndAxes(sourceMaterial, panelMaterial);

            GameObject stage = CreateCube("Polarization Observation Stage", new Vector3(0f, 3.25f, 0f), new Vector3(8.6f, 2.9f, 1.9f), stageMaterial);
            Collider stageCollider = stage.GetComponent<Collider>();
            if (stageCollider != null)
            {
                stageCollider.enabled = false;
            }

            PolarizationWaveVisualizer visualizer = stage.AddComponent<PolarizationWaveVisualizer>();
            LogStep("Created observation stage and visualizer.");

            GameObject root = new GameObject("Preset Polarization Elements");
            GameObject controllerObject = new GameObject("Lab 3 Controller");
            PolarizationLabController controller = controllerObject.AddComponent<PolarizationLabController>();
            controller.waveVisualizer = visualizer;
            controller.presetElementRoot = root.transform;
            PolarizationScenePresentation presentation = controllerObject.AddComponent<PolarizationScenePresentation>();
            controllerObject.AddComponent<ElectrostaticXrRayFallback>();
            presentation.desktopCamera = desktopCamera;
            presentation.cameraPosition = new Vector3(0f, 3.65f, -16.5f);
            presentation.orthographicSize = 6.7f;
            presentation.legibleOrthographicSize = 5.8f;

            TextMesh status = CreateText("Status", "Drag films into the stage. Hold R while dragging to rotate continuously.", new Vector3(0f, 6.17f, -2.7f), 0.052f, Color.white, TextAnchor.MiddleCenter);
            TextMesh result = CreateText("Polarization Result", "linear\nI = 1.00\nphase y-x = 0 deg", new Vector3(6.55f, 4.85f, -0.65f), 0.058f, Color.white, TextAnchor.MiddleCenter);
            controller.statusLabel = status;
            visualizer.statusLabel = status;
            visualizer.resultLabel = result;

            CreateFreeElementRack();
            CreatePresetPads(controller, padMaterial, panelMaterial);
            CreateControlButtons(controller, controlMaterial);
            controller.ApplyPreset(1);
            LogStep("Created free element rack, presets, and controls.");

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LogStep("Saved scene and assets.");

            Debug.Log("Built wave polarization lab scene at " + ScenePath);
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Wave Polarization Lab", "Lab 3 scene generated:\n" + ScenePath, "OK");
            }
        }
        catch (Exception exception)
        {
            LogStep("Build failed: " + exception);
            Debug.LogException(exception);
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Wave Polarization Lab Build Failed", exception.Message, "OK");
            }
            throw;
        }
        finally
        {
            buildInProgress = false;
        }
    }

    [MenuItem("Tools/Wave Polarization Lab/Build Lab 3 Scene", true)]
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
        Debug.Log("[WavePolarizationLabSceneBuilder] " + message);

        try
        {
            string path = ProjectPath(ProgressLogRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.AppendAllText(path, line + Environment.NewLine);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Could not write wave polarization lab progress log: " + exception.Message);
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

    static void CreateRoom(Material floorMaterial, Material wallMaterial, Material tableMaterial)
    {
        CreateCube("Floor", new Vector3(0f, -0.03f, 0f), new Vector3(19f, 0.06f, 12.4f), floorMaterial);
        CreateCube("Back Glass Wall", new Vector3(0f, 3.7f, 2.56f), new Vector3(19f, 7.4f, 0.08f), wallMaterial);
        CreateCube("Left Glass Wall", new Vector3(-9.5f, 3.7f, -1.6f), new Vector3(0.08f, 7.4f, 8.4f), wallMaterial);
        CreateCube("Right Glass Wall", new Vector3(9.5f, 3.7f, -1.6f), new Vector3(0.08f, 7.4f, 8.4f), wallMaterial);
        CreateCube("Workbench", new Vector3(0f, 0.52f, -3.5f), new Vector3(16.4f, 0.16f, 2.2f), tableMaterial);
    }

    static void CreateInstructionBoards(Material panelMaterial)
    {
        CreatePanel("Workflow Panel", new Vector3(-6.75f, 7.05f, -2.55f), new Vector2(3.7f, 1.18f), panelMaterial);
        CreateText("Workflow Board", "Workflow\n1 Drag film\n2 Hold R rotate\n3 Observe output", new Vector3(-8.38f, 7.18f, -2.64f), 0.048f, Color.white, TextAnchor.MiddleLeft);

        CreatePanel("Report Panel", new Vector3(0f, 7.05f, -2.55f), new Vector2(4.2f, 1.18f), panelMaterial);
        CreateText("Report Board", "Report\nQ1 amplitude\nQ2 axes\nQ4/Q5 handedness", new Vector3(-1.9f, 7.18f, -2.64f), 0.047f, Color.white, TextAnchor.MiddleLeft);

        CreatePanel("Models Panel", new Vector3(6.75f, 7.05f, -2.55f), new Vector2(3.7f, 1.18f), panelMaterial);
        CreateText("Models Board", "Models\nJones vector\nLP: q q\nQWP: i f f + s s", new Vector3(5.15f, 7.18f, -2.64f), 0.046f, Color.white, TextAnchor.MiddleLeft);
        CreateText("Stage Label", "Drop films here", new Vector3(0f, 5.35f, -0.98f), 0.052f, new Color(0.74f, 0.92f, 1f, 1f), TextAnchor.MiddleCenter);
    }

    static void CreateSourceAndAxes(Material sourceMaterial, Material panelMaterial)
    {
        CreatePanel("Incident Wave Label Panel", new Vector3(-5.85f, 4.9f, -0.95f), new Vector2(2.65f, 0.42f), panelMaterial);
        CreateText("Incident Wave Board", "Incident E: 45 deg linear", new Vector3(-7.08f, 4.9f, -1.04f), 0.044f, Color.white, TextAnchor.MiddleLeft);
        CreateText("Input Wave Label", "input wave display", new Vector3(-2.45f, 4.52f, -0.82f), 0.044f, new Color(0.65f, 0.96f, 1f, 1f), TextAnchor.MiddleCenter);
        CreateText("Output Wave Label", "output wave display", new Vector3(2.25f, 2.08f, -0.82f), 0.044f, new Color(1f, 0.95f, 0.35f, 1f), TextAnchor.MiddleCenter);

        GameObject arrow = CreateCube("Incident Wave Direction", new Vector3(-4.95f, 3.25f, -0.64f), new Vector3(1.5f, 0.065f, 0.07f), sourceMaterial);
        UnityEngine.Object.DestroyImmediate(arrow.GetComponent<Collider>());
        GameObject arrowHead = CreateCube("Incident Wave Arrow Head", new Vector3(-4.12f, 3.25f, -0.64f), new Vector3(0.22f, 0.22f, 0.07f), sourceMaterial);
        arrowHead.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        UnityEngine.Object.DestroyImmediate(arrowHead.GetComponent<Collider>());

        CreateText("Axis X Label", "x", new Vector3(-4.2f, 2.45f, -0.7f), 0.05f, Color.white, TextAnchor.MiddleCenter);
        CreateText("Axis Y Label", "y", new Vector3(-4.95f, 3.95f, -0.7f), 0.05f, Color.white, TextAnchor.MiddleCenter);
        CreateCube("Axis X", new Vector3(-4.72f, 2.6f, -0.68f), new Vector3(1.0f, 0.035f, 0.04f), sourceMaterial);
        CreateCube("Axis Y", new Vector3(-4.95f, 3.1f, -0.68f), new Vector3(0.035f, 1.0f, 0.04f), sourceMaterial);
    }

    static void CreateFreeElementRack()
    {
        CreateRackElement("Free Linear Polarizer A", PolarizationElementType.LinearPolarizer, new Vector3(-7.7f, 0.78f, -4.65f), 0f, "Linear polarizer");
        CreateRackElement("Free Linear Polarizer B", PolarizationElementType.LinearPolarizer, new Vector3(-6.35f, 0.78f, -4.65f), 45f, "Linear polarizer");
        CreateRackElement("Free QWP A", PolarizationElementType.QuarterWavePlate, new Vector3(6.35f, 0.78f, -4.65f), 0f, "Quarter-wave plate");
        CreateRackElement("Free QWP B", PolarizationElementType.QuarterWavePlate, new Vector3(7.7f, 0.78f, -4.65f), 45f, "Quarter-wave plate");
        CreateText("Left Rack Label", "drag blue LP films", new Vector3(-7.05f, 1.95f, -4.9f), 0.048f, Color.white, TextAnchor.MiddleCenter);
        CreateText("Right Rack Label", "drag orange QWP films", new Vector3(7.05f, 1.95f, -4.9f), 0.048f, Color.white, TextAnchor.MiddleCenter);
    }

    static void CreateRackElement(string objectName, PolarizationElementType type, Vector3 position, float angle, string hoverText)
    {
        GameObject element = PolarizationElementFactory.CreateRuntimeElement(objectName, type, position, angle);
        AddXRGrabInteractable(element);
        TextMesh annotation = CreateText(objectName + " Annotation", hoverText, position + new Vector3(0f, 1.0f, -0.4f), 0.058f, Color.white, TextAnchor.MiddleCenter);
        AddHoverAnnotation(element, annotation, new Vector3(0f, 1.1f, -0.35f));
    }

    static void CreatePresetPads(PolarizationLabController controller, Material padMaterial, Material panelMaterial)
    {
        string[] labels =
        {
            "Q1\nLP sweep",
            "Q2\nQWP first",
            "Q3\nLP y output",
            "Q4\nRHCP/LHCP",
            "Q5\nRHEP"
        };

        for (int i = 0; i < labels.Length; i++)
        {
            float x = -5.6f + i * 2.8f;
            GameObject pad = CreateCube("Polarization Preset Pad " + (i + 1), new Vector3(x, -0.12f, -5.15f), new Vector3(1.6f, 0.18f, 0.7f), padMaterial);
            PolarizationLabButton button = pad.AddComponent<PolarizationLabButton>();
            button.controller = controller;
            button.action = PolarizationLabButton.LabButtonAction.ApplyPreset;
            button.presetId = i + 1;
            AddXRSimpleInteractable(pad);
            CreateText("Polarization Preset Number " + (i + 1), (i + 1).ToString(), new Vector3(x, 0.09f, -5.28f), 0.105f, Color.black, TextAnchor.MiddleCenter);
            TextMesh hover = CreateText("Polarization Preset Annotation " + (i + 1), labels[i], new Vector3(x, 0.45f, -5.5f), 0.058f, Color.white, TextAnchor.MiddleCenter);
            AddHoverAnnotation(pad, hover, new Vector3(0f, 0.5f, -0.35f));
        }

        CreatePanel("Polarization Preset Help Panel", new Vector3(0f, -1.32f, -5.75f), new Vector2(8.8f, 0.32f), panelMaterial);
        CreateText("Polarization Preset Help", "1-5 presets | C clear | W wave | O orbit | hold R rotate | Q/E selected angle", new Vector3(0f, -1.32f, -5.82f), 0.04f, Color.white, TextAnchor.MiddleCenter);
    }

    static void CreateControlButtons(PolarizationLabController controller, Material controlMaterial)
    {
        CreateLabButton("Clear Button", "C", "Clear elements", new Vector3(-2.7f, -0.76f, -6.15f), PolarizationLabButton.LabButtonAction.ClearElements, controller, controlMaterial);
        CreateLabButton("Wave Trace Button", "W", "Wave trace", new Vector3(0f, -0.76f, -6.15f), PolarizationLabButton.LabButtonAction.ToggleWaveTrace, controller, controlMaterial);
        CreateLabButton("Orbit Button", "O", "Output orbit", new Vector3(2.7f, -0.76f, -6.15f), PolarizationLabButton.LabButtonAction.ToggleOrbit, controller, controlMaterial);
    }

    static void CreateLabButton(string objectName, string faceText, string hoverText, Vector3 position, PolarizationLabButton.LabButtonAction action, PolarizationLabController controller, Material material)
    {
        GameObject button = CreateCube(objectName, position, new Vector3(1.3f, 0.14f, 0.58f), material);
        PolarizationLabButton labButton = button.AddComponent<PolarizationLabButton>();
        labButton.controller = controller;
        labButton.action = action;
        AddXRSimpleInteractable(button);
        CreateText(objectName + " Label", faceText, position + new Vector3(0f, 0.17f, -0.13f), 0.068f, Color.black, TextAnchor.MiddleCenter);
        TextMesh annotation = CreateText(objectName + " Annotation", hoverText, position + new Vector3(0f, 0.5f, -0.35f), 0.058f, Color.white, TextAnchor.MiddleCenter);
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
