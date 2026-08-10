# Scene Builder Template

Use this as a starting point, not as a copy-paste requirement.

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExampleLabSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/ExampleLab.unity";

    [MenuItem("Tools/Example Lab/Build Scene")]
    public static void BuildScene()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Example Lab Build Blocked",
                "Exit Play Mode before building the scene.",
                "OK");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "ExampleLab";

        CreateCamera();
        CreateLighting();
        CreateStage();
        CreateControls();
        CreateStatusPanel();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.6f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.tag = "MainCamera";
    }

    private static void CreateLighting()
    {
        var lightObject = new GameObject("Directional Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
    }

    private static void CreateStage()
    {
        // Create visualization stage, display objects, and non-blocking transparent regions.
    }

    private static void CreateControls()
    {
        // Create buttons, presets, draggable objects, and colliders.
    }

    private static void CreateStatusPanel()
    {
        // Add short runtime status feedback.
    }
}
```

Template reminders:

- Keep one active `AudioListener`.
- Disable transparent colliders unless needed.
- Use clear object names.
- Add colliders that match user-visible interaction targets.
- Save under `Assets/Scenes/`.
