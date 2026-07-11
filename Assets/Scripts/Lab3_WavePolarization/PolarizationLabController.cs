using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class PolarizationLabController : MonoBehaviour
    {
        public PolarizationWaveVisualizer waveVisualizer;
        public Transform presetElementRoot;
        public TextMesh statusLabel;

        void Update()
        {
            if (Pressed(KeyCode.Alpha1, KeyCode.Keypad1))
            {
                ApplyPreset(1);
            }
            if (Pressed(KeyCode.Alpha2, KeyCode.Keypad2))
            {
                ApplyPreset(2);
            }
            if (Pressed(KeyCode.Alpha3, KeyCode.Keypad3))
            {
                ApplyPreset(3);
            }
            if (Pressed(KeyCode.Alpha4, KeyCode.Keypad4))
            {
                ApplyPreset(4);
            }
            if (Pressed(KeyCode.Alpha5, KeyCode.Keypad5))
            {
                ApplyPreset(5);
            }
            if (ActionPressed(KeyCode.C))
            {
                ClearPresetElements();
            }
            if (ActionPressed(KeyCode.W))
            {
                ToggleWaveTrace();
            }
            if (ActionPressed(KeyCode.O))
            {
                ToggleOrbit();
            }
        }

        static bool Pressed(KeyCode mainKey, KeyCode keypadKey)
        {
            bool pressed = Input.GetKeyDown(mainKey) || Input.GetKeyDown(keypadKey);
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (mainKey == KeyCode.Alpha1)
                {
                    pressed = pressed || keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame;
                }
                else if (mainKey == KeyCode.Alpha2)
                {
                    pressed = pressed || keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame;
                }
                else if (mainKey == KeyCode.Alpha3)
                {
                    pressed = pressed || keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame;
                }
                else if (mainKey == KeyCode.Alpha4)
                {
                    pressed = pressed || keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame;
                }
                else if (mainKey == KeyCode.Alpha5)
                {
                    pressed = pressed || keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame;
                }
            }
#endif
            return pressed;
        }

        static bool ActionPressed(KeyCode key)
        {
            bool pressed = Input.GetKeyDown(key);
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (key == KeyCode.C)
                {
                    pressed = pressed || keyboard.cKey.wasPressedThisFrame;
                }
                else if (key == KeyCode.W)
                {
                    pressed = pressed || keyboard.wKey.wasPressedThisFrame;
                }
                else if (key == KeyCode.O)
                {
                    pressed = pressed || keyboard.oKey.wasPressedThisFrame;
                }
            }
#endif
            return pressed;
        }

        public void ApplyPreset(int presetId)
        {
            ClearPresetElements();
            EnsureRoot();

            if (presetId == 1)
            {
                CreateElement("Scene 1 linear polarizer", PolarizationElementType.LinearPolarizer, 45f, new Vector3(-0.9f, 3.25f, -0.45f));
                SetStatus("Scene 1: rotate one linear polarizer to find max/min amplitude.");
            }
            else if (presetId == 2)
            {
                CreateElement("Scene 2 quarter-wave plate", PolarizationElementType.QuarterWavePlate, 0f, new Vector3(-0.9f, 3.25f, -0.45f));
                SetStatus("Scene 2: first element is a QWP. Label fast and slow axes.");
            }
            else if (presetId == 3)
            {
                CreateElement("Scene 3 quarter-wave plate", PolarizationElementType.QuarterWavePlate, 0f, new Vector3(-1.4f, 3.25f, -0.45f));
                CreateElement("Scene 3 linear polarizer", PolarizationElementType.LinearPolarizer, 90f, new Vector3(1.15f, 3.25f, -0.45f));
                SetStatus("Scene 3: QWP followed by y-axis linear polarizer.");
            }
            else if (presetId == 4)
            {
                CreateElement("Scene 4 opposite circular candidate", PolarizationElementType.QuarterWavePlate, 90f, new Vector3(-0.9f, 3.25f, -0.45f));
                SetStatus("Scene 4: QWP at 90 deg gives the opposite circular handedness.");
            }
            else
            {
                CreateElement("Scene 5 linear polarizer", PolarizationElementType.LinearPolarizer, 0f, new Vector3(-1.55f, 3.25f, -0.45f));
                CreateElement("Scene 5 quarter-wave plate", PolarizationElementType.QuarterWavePlate, 30f, new Vector3(1.0f, 3.25f, -0.45f));
                SetStatus("Scene 5: adjust angles for RHEP with different eccentricity.");
            }

            RefreshWave();
        }

        public void ClearPresetElements()
        {
            if (presetElementRoot != null)
            {
                for (int i = presetElementRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(presetElementRoot.GetChild(i).gameObject);
                }
            }

            SetStatus("Free lab: drag elements into the stage and rotate continuously.");
            RefreshWave();
        }

        public void ToggleWaveTrace()
        {
            if (waveVisualizer == null)
            {
                return;
            }

            waveVisualizer.SetWaveTraceVisible(!waveVisualizer.showWaveTrace);
            SetStatus(waveVisualizer.showWaveTrace ? "Wave trace: on." : "Wave trace: off.");
        }

        public void ToggleOrbit()
        {
            if (waveVisualizer == null)
            {
                return;
            }

            waveVisualizer.SetOrbitVisible(!waveVisualizer.showPolarizationOrbit);
            SetStatus(waveVisualizer.showPolarizationOrbit ? "Output orbit: on." : "Output orbit: off.");
        }

        void EnsureRoot()
        {
            if (presetElementRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("Preset Polarization Elements");
            presetElementRoot = root.transform;
        }

        PolarizationElement CreateElement(string objectName, PolarizationElementType type, float angle, Vector3 position)
        {
            GameObject elementObject = PolarizationElementFactory.CreateRuntimeElement(objectName, type, position, angle);
            elementObject.transform.SetParent(presetElementRoot, true);
            return elementObject.GetComponent<PolarizationElement>();
        }

        void RefreshWave()
        {
            if (waveVisualizer != null)
            {
                waveVisualizer.ForceRefresh();
            }
        }

        void SetStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message;
            }
        }
    }
}
