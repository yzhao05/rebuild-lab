using System;
using System.IO;
using ElectrostaticsLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ElectrostaticsLabSceneBuilder
{
    const string ScenePath = "Assets/Scenes/Lab1_Electrostatics.unity";
    const string MaterialFolder = "Assets/ElectrostaticsLab/Materials";
    const string ProgressLogRelativePath = "tmp/electrostatics-lab-build-progress.log";
    const int BaseTextFontSize = 48;
    const int HighResolutionTextFontSize = 384;

    static bool buildInProgress;

    [MenuItem("Tools/Electrostatics Lab/Build Lab 1 Scene")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            const string message = "Exit Play Mode before generating the lab scene.";
            LogStep(message);
            EditorUtility.DisplayDialog("Electrostatics Lab", message, "OK");
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
            EnsureAssetFolder("Assets", "ElectrostaticsLab");
            EnsureAssetFolder("Assets/ElectrostaticsLab", "Materials");
            LogStep("Ensured output folders.");

            Material floorMaterial = CreateMaterial("Lab Floor", new Color(0.09f, 0.1f, 0.11f, 1f), false);
            Material wallMaterial = CreateMaterial("Glass Wall", new Color(0.42f, 0.52f, 0.58f, 0.34f), true);
            Material tableMaterial = CreateMaterial("Matte Table", new Color(0.18f, 0.18f, 0.2f, 1f), false);
            Material stageMaterial = CreateMaterial("Visualization Cube", new Color(0.45f, 0.78f, 1f, 0.18f), true);
            Material positiveMaterial = CreateMaterial("Positive Red", new Color(1f, 0.08f, 0.03f, 1f), false);
            Material negativeMaterial = CreateMaterial("Negative Blue", new Color(0.04f, 0.24f, 1f, 1f), false);
            Material neutralMaterial = CreateMaterial("Neutral Charge", new Color(0.82f, 0.84f, 0.86f, 1f), false);
            Material padMaterial = CreateMaterial("Preset Pad", new Color(0.95f, 0.82f, 0.25f, 1f), false);
            Material highlightedPadMaterial = CreateMaterial("Highlighted Preset Pad", new Color(1f, 0.08f, 0.03f, 1f), false);
            Material textPanelMaterial = CreateMaterial("Text Panel", new Color(0.04f, 0.05f, 0.06f, 0.86f), true);
            LogStep("Created or updated materials.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Lab1_Electrostatics";
            LogStep("Created empty scene.");

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.36f, 0.38f, 1f);

            CreateLighting();
            Camera desktopCamera = CreateCamera();
            TryAddXRInteractionSetup();
            EnsureXRInteractionManager();
            EnsureSingleAudioListener();
            LogStep("Created lighting, camera, and optional XR setup.");

            CreateRoom(floorMaterial, wallMaterial, tableMaterial);
            LogStep("Created lab room.");

            GameObject stage = CreateCube("Field Visualization Cube", new Vector3(0f, 3.4f, 0f), new Vector3(11.6f, 6f, 3.6f), stageMaterial);
            BoxCollider stageCollider = stage.GetComponent<BoxCollider>();
            stageCollider.isTrigger = true;
            ElectrostaticFieldVisualizer visualizer = stage.AddComponent<ElectrostaticFieldVisualizer>();
            LogStep("Created field visualization cube.");

            GameObject root = new GameObject("Preset Charges");
            GameObject controllerObject = new GameObject("Lab Controller");
            ElectrostaticLabController controller = controllerObject.AddComponent<ElectrostaticLabController>();
            controller.fieldVisualizer = visualizer;
            controller.presetChargeRoot = root.transform;
            ElectrostaticScenePresentation presentation = controllerObject.AddComponent<ElectrostaticScenePresentation>();
            controllerObject.AddComponent<ElectrostaticXrRayFallback>();
            presentation.desktopCamera = desktopCamera;
            presentation.forceFrontOrthographicCamera = true;
            presentation.hideDeviceSimulatorUi = true;
            presentation.cameraPosition = new Vector3(0f, 3.45f, -15.8f);
            presentation.orthographicSize = 6.5f;

            CreateChargers(positiveMaterial, negativeMaterial);
            CreateFreeCharge("Point Charge", ChargeDistribution.Point, new Vector3(-5.6f, 1.05f, 0f), neutralMaterial, "Point charge\nGrab, charge, then place in cube.");
            CreateFreeCharge("Line Charge", ChargeDistribution.Line, new Vector3(-4.2f, 1.35f, 0f), neutralMaterial, "Line charge\nProduces a radial field around its axis.");
            CreateFreeCharge("Surface Charge", ChargeDistribution.Surface, new Vector3(-2.8f, 1.35f, 0f), neutralMaterial, "Surface charge\nApproximates a charged plate.");
            CreateFreeCharge("Extra Point Charge", ChargeDistribution.Point, new Vector3(2.8f, 1.05f, 0f), neutralMaterial, "Point charge\nUse with another object to compare superposition.");
            CreateFreeCharge("Extra Line Charge", ChargeDistribution.Line, new Vector3(4.2f, 1.35f, 0f), neutralMaterial, "Line charge\nUse positive or negative charging.");
            CreateFreeCharge("Extra Surface Charge", ChargeDistribution.Surface, new Vector3(5.6f, 1.35f, 0f), neutralMaterial, "Surface charge\nPair plates to create a near-uniform field.");
            LogStep("Created chargers and free charge objects.");

            controller.statusLabel = CreateText(
                "Status",
                "Charge -> Field Cube -> Observe",
                new Vector3(0f, 6.95f, -2.7f),
                0.052f,
                Color.white,
                TextAnchor.MiddleCenter);

            CreateInstructionBoards(textPanelMaterial);
            CreatePresetPads(controller, padMaterial, highlightedPadMaterial, textPanelMaterial);
            CreateVrControlButtons(controller, padMaterial);
            CreateReportHints(textPanelMaterial);
            LogStep("Created instruction boards, preset pads, and report hints.");

            controller.ApplyPreset(4);
            LogStep("Applied default challenge preset.");

            EditorSceneManager.SaveScene(scene, ScenePath);
            LogStep("Saved scene to " + ScenePath + ".");

            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            LogStep("Saved assets and updated build settings.");

            if (!Application.isBatchMode)
            {
                AssetDatabase.Refresh();
                LogStep("AssetDatabase refresh completed.");
            }
            else
            {
                LogStep("Skipped final AssetDatabase refresh in batchmode.");
            }

            Debug.Log("Built electrostatics lab scene at " + ScenePath);
            LogStep("Build completed successfully.");
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Electrostatics Lab", "Lab 1 scene generated:\n" + ScenePath, "OK");
            }
        }
        catch (Exception exception)
        {
            LogStep("Build failed: " + exception);
            Debug.LogException(exception);
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Electrostatics Lab Build Failed", exception.Message, "OK");
            }
            throw;
        }
        finally
        {
            buildInProgress = false;
        }
    }

    [MenuItem("Tools/Electrostatics Lab/Build Lab 1 Scene", true)]
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
        Debug.Log("[ElectrostaticsLabSceneBuilder] " + message);

        try
        {
            string path = ProjectPath(ProgressLogRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.AppendAllText(path, line + Environment.NewLine);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Could not write electrostatics lab progress log: " + exception.Message);
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
        camera.orthographicSize = 6.5f;
        camera.fieldOfView = 50f;
        camera.depth = 10f;
        camera.clearFlags = CameraClearFlags.Skybox;
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 3.45f, -15.8f);
        cameraObject.transform.rotation = Quaternion.identity;

        AudioListener listener = cameraObject.AddComponent<AudioListener>();
        listener.enabled = true;
        return camera;
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

        LogStep("Audio listeners found: " + listeners.Length + ". Enabled exactly one.");
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

    static void CreateChargers(Material positiveMaterial, Material negativeMaterial)
    {
        GameObject positive = CreateCube("Positive Charger", new Vector3(-8f, 1.05f, 0f), new Vector3(0.9f, 0.9f, 0.9f), positiveMaterial);
        positive.GetComponent<BoxCollider>().isTrigger = true;
        ElectrostaticCharger positiveCharger = positive.AddComponent<ElectrostaticCharger>();
        positiveCharger.polarity = 1;
        TextMesh positiveLabel = CreateText("Positive Charger Label", "Positive charger\nAdds + charge.", new Vector3(-8f, 1.9f, -0.55f), 0.07f, Color.white, TextAnchor.MiddleCenter);
        AddHoverAnnotation(positive, positiveLabel, new Vector3(0f, 0.76f, -0.45f));

        GameObject negative = CreateCube("Negative Charger", new Vector3(8f, 1.05f, 0f), new Vector3(0.9f, 0.9f, 0.9f), negativeMaterial);
        negative.GetComponent<BoxCollider>().isTrigger = true;
        ElectrostaticCharger negativeCharger = negative.AddComponent<ElectrostaticCharger>();
        negativeCharger.polarity = -1;
        TextMesh negativeLabel = CreateText("Negative Charger Label", "Negative charger\nAdds - charge.", new Vector3(8f, 1.9f, -0.55f), 0.07f, Color.white, TextAnchor.MiddleCenter);
        AddHoverAnnotation(negative, negativeLabel, new Vector3(0f, 0.76f, -0.45f));
    }

    static ElectrostaticCharge CreateFreeCharge(string objectName, ChargeDistribution distribution, Vector3 position, Material material, string annotationText)
    {
        GameObject body = CreateChargeBody(objectName, distribution, position, material);
        Rigidbody rigidbody = body.AddComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.drag = 8f;
        rigidbody.angularDrag = 8f;
        body.AddComponent<ElectrostaticDesktopDraggable>();
        AddXRGrabInteractable(body);

        ElectrostaticCharge charge = body.AddComponent<ElectrostaticCharge>();
        charge.distribution = distribution;
        charge.displayName = DistributionName(distribution);
        charge.chargeUnits = 0;
        charge.chargeLabel = CreateChargeLabel(body.transform, annotationText);
        charge.UpdateVisuals();
        AddHoverAnnotation(body, charge.chargeLabel, new Vector3(0f, 1.25f, -0.45f));
        return charge;
    }

    static GameObject CreateChargeBody(string objectName, ChargeDistribution distribution, Vector3 position, Material material)
    {
        PrimitiveType primitive = PrimitiveType.Sphere;
        Vector3 scale = Vector3.one * 0.42f;

        if (distribution == ChargeDistribution.Line)
        {
            primitive = PrimitiveType.Cylinder;
            scale = new Vector3(0.16f, 1.25f, 0.16f);
        }
        else if (distribution == ChargeDistribution.Surface)
        {
            primitive = PrimitiveType.Cube;
            scale = new Vector3(0.14f, 2.15f, 1.05f);
        }

        GameObject body = GameObject.CreatePrimitive(primitive);
        body.name = objectName;
        body.transform.position = position;
        body.transform.localScale = scale;
        body.GetComponent<Renderer>().sharedMaterial = material;
        return body;
    }

    static TextMesh CreateChargeLabel(Transform parent, string annotationText)
    {
        TextMesh label = CreateText("Charge Label", annotationText, parent.position + new Vector3(0f, 1.25f, -0.45f), 0.075f, Color.white, TextAnchor.MiddleCenter);
        label.transform.SetParent(parent, true);
        return label;
    }

    static void CreatePresetPads(ElectrostaticLabController controller, Material padMaterial, Material highlightedPadMaterial, Material textPanelMaterial)
    {
        string[] labels =
        {
            "Scene 1\nUnequal lines",
            "Scene 2\nPoint + line",
            "Scene 3\nLine + plate",
            "Scene 4\nOpposite plates",
            "Scene 5\nPoint rows"
        };

        for (int i = 0; i < labels.Length; i++)
        {
            bool highlighted = i == 0 || i == 1 || i == 4;
            float x = -5.6f + i * 2.8f;
            Vector3 padScale = highlighted ? new Vector3(1.85f, 0.22f, 0.9f) : new Vector3(1.55f, 0.16f, 0.72f);
            Material material = highlighted ? highlightedPadMaterial : padMaterial;
            GameObject pad = CreateCube("Preset Pad " + (i + 1), new Vector3(x, 0.64f, -5.15f), padScale, material);
            ElectrostaticPresetPad presetPad = pad.AddComponent<ElectrostaticPresetPad>();
            presetPad.controller = controller;
            presetPad.presetId = i + 1;
            AddXRSimpleInteractable(pad);
            CreateText("Preset Pad Number " + (i + 1), (i + 1).ToString(), new Vector3(x, 0.88f, -5.28f), highlighted ? 0.145f : 0.12f, highlighted ? Color.white : Color.black, TextAnchor.MiddleCenter);
            TextMesh hoverText = CreateText("Preset Pad Annotation " + (i + 1), labels[i], new Vector3(x, 1.12f, -5.5f), 0.078f, Color.white, TextAnchor.MiddleCenter);
            AddHoverAnnotation(pad, hoverText, new Vector3(0f, 0.5f, -0.35f));
        }

        CreatePanel("Preset Help Panel", new Vector3(0f, 0.29f, -5.75f), new Vector2(6.4f, 0.34f), textPanelMaterial);
        CreateText("Preset Help", "VR: point ray or touch, then select", new Vector3(0f, 0.29f, -5.82f), 0.052f, Color.white, TextAnchor.MiddleCenter);
    }

    static void CreateVrControlButtons(ElectrostaticLabController controller, Material padMaterial)
    {
        CreateLabButton("Clear Button", "C", "Clear\nremove presets", new Vector3(-2.8f, 0.64f, -6.15f), ElectrostaticLabButton.LabButtonAction.ClearCharges, controller, padMaterial);
        CreateLabButton("Field Lines Button", "F", "Field lines\nshow / hide", new Vector3(0f, 0.64f, -6.15f), ElectrostaticLabButton.LabButtonAction.ToggleFieldLines, controller, padMaterial);
        CreateLabButton("Vectors Button", "V", "Vectors\nshow / hide", new Vector3(2.8f, 0.64f, -6.15f), ElectrostaticLabButton.LabButtonAction.ToggleVectors, controller, padMaterial);
    }

    static void CreateLabButton(string objectName, string faceText, string hoverText, Vector3 position, ElectrostaticLabButton.LabButtonAction action, ElectrostaticLabController controller, Material padMaterial)
    {
        GameObject button = CreateCube(objectName, position, new Vector3(1.45f, 0.18f, 0.72f), padMaterial);
        ElectrostaticLabButton labButton = button.AddComponent<ElectrostaticLabButton>();
        labButton.controller = controller;
        labButton.action = action;
        AddXRSimpleInteractable(button);

        CreateText(objectName + " Label", faceText, position + new Vector3(0f, 0.24f, -0.13f), 0.12f, Color.black, TextAnchor.MiddleCenter);
        TextMesh annotation = CreateText(objectName + " Annotation", hoverText, position + new Vector3(0f, 0.6f, -0.35f), 0.078f, Color.white, TextAnchor.MiddleCenter);
        AddHoverAnnotation(button, annotation, new Vector3(0f, 0.52f, -0.35f));
    }

    static void CreateInstructionBoards(Material textPanelMaterial)
    {
        string tutorial =
            "Workflow\n" +
            "1. Grip a charge\n" +
            "2. Hold in red/blue\n" +
            "3. Place in cube\n" +
            "4. Select buttons";

        CreatePanel("Tutorial Panel", new Vector3(-6.2f, 5.55f, -2.55f), new Vector2(4.2f, 1.35f), textPanelMaterial);
        CreateText("Tutorial Board", tutorial, new Vector3(-8.05f, 5.86f, -2.64f), 0.053f, Color.white, TextAnchor.MiddleLeft);

        string theory =
            "Field models\n" +
            "Point: E ~ q/r^2\n" +
            "Line: E ~ lambda/r\n" +
            "Plate: E ~ sigma/2eps\n" +
            "Total: vector sum";

        CreatePanel("Theory Panel", new Vector3(6.2f, 5.55f, -2.55f), new Vector2(4.2f, 1.35f), textPanelMaterial);
        CreateText("Theory Board", theory, new Vector3(4.35f, 5.86f, -2.64f), 0.053f, Color.white, TextAnchor.MiddleLeft);
        CreateText("Stage Label", "Field visualization stage", new Vector3(0f, 6.28f, -0.98f), 0.056f, new Color(0.74f, 0.92f, 1f, 1f), TextAnchor.MiddleCenter);
    }

    static void CreateReportHints(Material textPanelMaterial)
    {
        string hints =
            "Report\n" +
            "Q1-Q2 line charges\n" +
            "Q3-Q4 superposition\n" +
            "Q5 top/front views\n" +
            "Q6 plate ROI\n" +
            "Q7 point rows";

        CreatePanel("Report Hints Panel", new Vector3(0f, 5.55f, -2.55f), new Vector2(4.4f, 1.35f), textPanelMaterial);
        CreateText("Report Hints", hints, new Vector3(-1.95f, 5.86f, -2.64f), 0.051f, Color.white, TextAnchor.MiddleLeft);
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
            textMesh.font.material.mainTexture.filterMode = FilterMode.Bilinear;
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

    static string DistributionName(ChargeDistribution distribution)
    {
        if (distribution == ChargeDistribution.Point)
        {
            return "Point";
        }

        if (distribution == ChargeDistribution.Line)
        {
            return "Line";
        }

        return "Surface";
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
