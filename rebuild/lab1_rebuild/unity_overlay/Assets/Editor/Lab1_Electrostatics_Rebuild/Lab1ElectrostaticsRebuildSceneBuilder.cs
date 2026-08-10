using System.Collections.Generic;
using EMVR.Lab1Rebuild;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChargedObjects;
using ChargePlane = ChargedObjects.Plane;

public static class Lab1ElectrostaticsRebuildSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Lab1_Electrostatics_Rebuild.unity";
    private const int IgnoreRaycastLayer = 2;

    [MenuItem("Tools/Electrostatics Lab/Rebuild Lab 1 Scene")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Lab 1 Rebuild", "Exit Play Mode before rebuilding the Lab 1 scene.", "OK");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Lab1_Electrostatics_Rebuild";

        Material benchMaterial = CreateMaterial("Lab1 Summer26 Bench", new Color(0.13f, 0.15f, 0.16f, 1f));
        Material stageMaterial = CreateTransparentMaterial("Lab1 Summer26 Observation", new Color(0.12f, 0.68f, 1f, 0.18f));
        Material positiveMaterial = CreateMaterial("Lab1 Summer26 Positive Charger", new Color(0.95f, 0.12f, 0.08f, 1f));
        Material negativeMaterial = CreateMaterial("Lab1 Summer26 Negative Charger", new Color(0.08f, 0.28f, 0.95f, 1f));
        Material sourceMaterial = CreateMaterial("Lab1 Summer26 Neutral Source", Color.white);
        Material padMaterial = CreateMaterial("Lab1 Summer26 Preset Pad", new Color(1f, 0.76f, 0.18f, 1f));
        Material highlightPadMaterial = CreateMaterial("Lab1 Summer26 Highlight Pad", new Color(1f, 0.26f, 0.18f, 1f));

        CreateCamera();
        CreateLight();
        CreateBench(benchMaterial);
        CreateInstructionLayout();

        Lab1RebuildStatus status = new GameObject("Lab1 Rebuild Status").AddComponent<Lab1RebuildStatus>();
        status.statusText = CreateText("Status Text", "status: ready", new Vector3(-6.7f, -4.0f, -0.25f), 0.04f, TextAnchor.UpperLeft);

        ElectricField electricField = CreateObservationStage(stageMaterial);
        CreateCharger("Positive Charger", new Vector3(-5.7f, 0.75f, 0f), positiveMaterial, 1, "red charger\n+q");
        CreateCharger("Negative Charger", new Vector3(-5.7f, -0.75f, 0f), negativeMaterial, -1, "blue charger\n-q");

        Lab1Summer26SourceRuntime[] sources =
        {
            CreatePointCharge("Point Charge A", new Vector3(5.65f, 1.5f, 0f), sourceMaterial),
            CreatePointCharge("Point Charge B", new Vector3(6.25f, 1.5f, 0f), sourceMaterial),
            CreateLineCharge("Line Charge A", new Vector3(5.65f, 0.25f, 0f), sourceMaterial),
            CreateLineCharge("Line Charge B", new Vector3(6.25f, 0.25f, 0f), sourceMaterial),
            CreatePlaneCharge("Plane Charge A", new Vector3(5.65f, -1.15f, 0f), sourceMaterial),
            CreatePlaneCharge("Plane Charge B", new Vector3(6.25f, -1.15f, 0f), sourceMaterial)
        };

        GameObject controllerObject = new GameObject("Lab1 Summer26 Preset Controller");
        Lab1Summer26PresetController controller = controllerObject.AddComponent<Lab1Summer26PresetController>();
        controller.sources = sources;

        for (int i = 1; i <= 5; i++)
        {
            bool highlighted = i == 1 || i == 2 || i == 5;
            CreatePresetPad(i, new Vector3(-2.0f + (i - 1) * 1.0f, -2.65f, 0f), highlighted ? highlightPadMaterial : padMaterial, controller);
        }

        Selection.activeObject = electricField.gameObject;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorUtility.DisplayDialog("Lab 1 Rebuild", "Generated " + ScenePath + " using Summer26 Sphere/Wire/Plane/ChargingBox/ElectricField scripts.", "OK");
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Desktop Preview Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.55f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.18f, 0.2f, 0.22f, 1f);
        camera.stereoTargetEye = StereoTargetEyeMask.None;
        camera.targetDisplay = 0;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.transform.rotation = Quaternion.identity;
        cameraObject.tag = "MainCamera";
        cameraObject.AddComponent<AudioListener>();
    }

    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Key Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.05f;
        lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
    }

    private static void CreateBench(Material material)
    {
        CreateCube("Lab Bench", new Vector3(0f, 0f, 0.55f), new Vector3(13.8f, 8.4f, 0.08f), material);
    }

    private static void CreateInstructionLayout()
    {
        CreateText("Workflow Panel", "Workflow\n1 drag source\n2 charge red/blue\n3 move into stage\n4 press 1-5 presets", new Vector3(-6.7f, 3.95f, -0.25f), 0.04f, TextAnchor.UpperLeft);
        CreateText("Models Panel", "Summer26 model\nSphere.cs point q/r^2\nWire.cs line lambda/r\nPlane.cs surface field\nElectricField.cs draws lines", new Vector3(3.55f, 3.95f, -0.25f), 0.04f, TextAnchor.UpperLeft);
        CreateText("Report Panel", "Report focus\nQ1-Q2 point pairs\nQ3-Q4 line/point\nQ5 view changes\nQ6-Q7 alternatives", new Vector3(3.55f, -1.65f, -0.25f), 0.04f, TextAnchor.UpperLeft);
        CreateText("Stage Label", "Observation stage\nSummer26 ElectricField trigger", new Vector3(-2.2f, 1.95f, -0.25f), 0.035f, TextAnchor.UpperLeft);
        CreateText("Preset Hint", "1  2  3  4  5      C clear", new Vector3(-2.28f, -2.25f, -0.25f), 0.04f, TextAnchor.UpperLeft);
    }

    private static ElectricField CreateObservationStage(Material material)
    {
        GameObject stage = CreateCube("Observation Zone - Summer26 ElectricField", new Vector3(0f, 0.1f, 0.2f), new Vector3(4.6f, 3.1f, 1.0f), material);
        stage.layer = IgnoreRaycastLayer;
        BoxCollider collider = stage.GetComponent<BoxCollider>();
        collider.isTrigger = true;

        ElectricField field = stage.AddComponent<ElectricField>();
        field.Space = stage;
        field.ChargedWires = new List<Wire>();
        field.ChargedPlanes = new List<ChargePlane>();
        field.ChargedSpheres = new List<Sphere>();
        field.Ele_field_lines = new List<LineRenderer>();
        field.Lines = new List<GameObject>();
        field.Wire_density = 10;
        field.Plane_density = 10;
        field.Field_Line_Length = 220;
        field.stepsize = 0.035f;
        return field;
    }

    private static Lab1Summer26SourceRuntime CreatePointCharge(string name, Vector3 position, Material material)
    {
        GameObject source = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        source.name = name;
        source.transform.position = position;
        source.transform.localScale = Vector3.one * 0.34f;
        SetMaterial(source, material);

        Sphere sphere = source.AddComponent<Sphere>();
        sphere.sphere = sphere;
        sphere.one = CreateMarker(source.transform, "field seed +x", new Vector3(0.22f, 0f, 0f));
        sphere.two = CreateMarker(source.transform, "field seed -x", new Vector3(-0.22f, 0f, 0f));
        sphere.three = CreateMarker(source.transform, "field seed +y", new Vector3(0f, 0.22f, 0f));
        sphere.four = CreateMarker(source.transform, "field seed -y", new Vector3(0f, -0.22f, 0f));
        sphere.five = CreateMarker(source.transform, "field seed +z", new Vector3(0f, 0f, 0.22f));
        sphere.six = CreateMarker(source.transform, "field seed -z", new Vector3(0f, 0f, -0.22f));

        return AddSourceRuntime(source, "point");
    }

    private static Lab1Summer26SourceRuntime CreateLineCharge(string name, Vector3 position, Material material)
    {
        GameObject source = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        source.name = name;
        source.transform.position = position;
        source.transform.localScale = new Vector3(0.08f, 0.72f, 0.08f);
        SetMaterial(source, material);

        Wire wire = source.AddComponent<Wire>();
        wire.wire = wire;
        wire.head = CreateMarker(source.transform, "head", new Vector3(0f, 0.72f, 0f));
        wire.tail = CreateMarker(source.transform, "tail", new Vector3(0f, -0.72f, 0f));

        return AddSourceRuntime(source, "line");
    }

    private static Lab1Summer26SourceRuntime CreatePlaneCharge(string name, Vector3 position, Material material)
    {
        GameObject source = GameObject.CreatePrimitive(PrimitiveType.Cube);
        source.name = name;
        source.transform.position = position;
        source.transform.localScale = new Vector3(0.10f, 1.25f, 0.45f);
        SetMaterial(source, material);

        ChargePlane plane = source.AddComponent<ChargePlane>();
        plane.plane = plane;
        plane.up = CreateMarker(source.transform, "up", new Vector3(0f, 0.72f, 0f));
        plane.down = CreateMarker(source.transform, "down", new Vector3(0f, -0.72f, 0f));
        plane.left = CreateMarker(source.transform, "left", new Vector3(0f, 0f, -0.32f));
        plane.right = CreateMarker(source.transform, "right", new Vector3(0f, 0f, 0.32f));

        return AddSourceRuntime(source, "plane");
    }

    private static Lab1Summer26SourceRuntime AddSourceRuntime(GameObject source, string displayName)
    {
        Rigidbody body = source.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;

        Lab1Summer26SourceRuntime runtime = source.AddComponent<Lab1Summer26SourceRuntime>();
        runtime.displayName = displayName;
        runtime.label = CreateChildText(source.transform, "Charge Label", displayName + "\nq = 0", new Vector3(-0.34f, 0.48f, -0.22f), 0.03f);
        return runtime;
    }

    private static void CreateCharger(string name, Vector3 position, Material material, int sign, string label)
    {
        GameObject charger = CreateCube(name, position, new Vector3(0.8f, 0.8f, 0.38f), material);
        BoxCollider collider = charger.GetComponent<BoxCollider>();
        collider.isTrigger = true;

        ChargingBox chargingBox = charger.AddComponent<ChargingBox>();
        chargingBox.sign = sign;

        Lab1Summer26ChargerFallback fallback = charger.AddComponent<Lab1Summer26ChargerFallback>();
        fallback.sign = sign;
        CreateChildText(charger.transform, "Charger Label", label, new Vector3(-0.28f, 0.62f, -0.24f), 0.035f);
    }

    private static void CreatePresetPad(int index, Vector3 position, Material material, Lab1Summer26PresetController controller)
    {
        GameObject pad = CreateCube("Preset " + index, position, new Vector3(0.58f, 0.34f, 0.18f), material);
        Lab1Summer26PresetPad presetPad = pad.AddComponent<Lab1Summer26PresetPad>();
        presetPad.presetIndex = index;
        presetPad.controller = controller;
        CreateChildText(pad.transform, "Preset Label", index.ToString(), new Vector3(-0.045f, 0.095f, -0.18f), 0.045f);
    }

    private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.localScale = scale;
        SetMaterial(cube, material);
        return cube;
    }

    private static GameObject CreateMarker(Transform parent, string name, Vector3 localPosition)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = localPosition;
        return marker;
    }

    private static TextMesh CreateText(string name, string text, Vector3 position, float characterSize, TextAnchor anchor)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.position = position;
        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.anchor = anchor;
        textMesh.alignment = TextAlignment.Left;
        textMesh.fontSize = 64;
        textMesh.characterSize = characterSize;
        textMesh.lineSpacing = 0.9f;
        textMesh.color = Color.white;
        return textMesh;
    }

    private static TextMesh CreateChildText(Transform parent, string name, string text, Vector3 localPosition, float characterSize)
    {
        TextMesh textMesh = CreateText(name, text, Vector3.zero, characterSize, TextAnchor.UpperLeft);
        textMesh.transform.SetParent(parent, false);
        textMesh.transform.localPosition = localPosition;
        return textMesh;
    }

    private static void SetMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.name = name;
        material.color = color;
        return material;
    }

    private static Material CreateTransparentMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.name = name;
        material.color = color;
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        return material;
    }
}
