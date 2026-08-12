# Lab Controller Template

Use this as a controller structure reference.

```csharp
using UnityEngine;

public sealed class ExampleLabController : MonoBehaviour
{
    [SerializeField] private ExampleVisualizer visualizer;
    [SerializeField] private TextMesh statusText;

    private int activePreset = 1;

    private void Awake()
    {
        if (visualizer == null)
        {
            visualizer = FindObjectOfType<ExampleVisualizer>();
        }
    }

    private void Start()
    {
        ApplyPreset(1);
    }

    private void Update()
    {
        HandleKeyboard();
    }

    public void ApplyPreset(int preset)
    {
        activePreset = Mathf.Clamp(preset, 1, 5);

        // Configure lab-specific state here.

        RefreshVisualization();
    }

    public void NotifySelection(Object selected)
    {
        SetStatus(selected == null ? "selected: none" : "selected: " + selected.name);
    }

    private void RefreshVisualization()
    {
        if (visualizer != null)
        {
            visualizer.Refresh();
        }

        SetStatus("preset: " + activePreset);
    }

    private void HandleKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ApplyPreset(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ApplyPreset(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ApplyPreset(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ApplyPreset(4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ApplyPreset(5);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
```

Template reminders:

- Keep shortcut handling centralized.
- Presets should create obvious visual differences.
- Status text should show recognized state, not just instructions.
- Keep display refresh separate from input handling.
