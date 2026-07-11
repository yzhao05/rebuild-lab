using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class MagneticLabController : MonoBehaviour
    {
        const int BaseTextFontSize = 48;
        const int HighResolutionTextFontSize = 384;

        public MagneticFieldVisualizer fieldVisualizer;
        public Transform presetSourceRoot;
        public TextMesh statusLabel;
        public int presetMaxCurrent = 6;

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
                ClearPresetSources();
            }
            if (ActionPressed(KeyCode.F))
            {
                ToggleFieldLines();
            }
            if (ActionPressed(KeyCode.V))
            {
                ToggleVectors();
            }
            if (ActionPressed(KeyCode.X))
            {
                FlipAllSourcesInStage();
            }
            if (ActionPressed(KeyCode.Equals) || ActionPressed(KeyCode.KeypadPlus))
            {
                AddCurrentToSourcesInStage(1);
            }
            if (ActionPressed(KeyCode.Minus) || ActionPressed(KeyCode.KeypadMinus))
            {
                AddCurrentToSourcesInStage(-1);
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
                else if (key == KeyCode.F)
                {
                    pressed = pressed || keyboard.fKey.wasPressedThisFrame;
                }
                else if (key == KeyCode.V)
                {
                    pressed = pressed || keyboard.vKey.wasPressedThisFrame;
                }
                else if (key == KeyCode.X)
                {
                    pressed = pressed || keyboard.xKey.wasPressedThisFrame;
                }
                else if (key == KeyCode.Equals || key == KeyCode.KeypadPlus)
                {
                    pressed = pressed || keyboard.equalsKey.wasPressedThisFrame || keyboard.numpadPlusKey.wasPressedThisFrame;
                }
                else if (key == KeyCode.Minus || key == KeyCode.KeypadMinus)
                {
                    pressed = pressed || keyboard.minusKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame;
                }
            }
#endif
            return pressed;
        }

        public void ApplyPreset(int presetId)
        {
            ClearPresetSources();
            EnsureRoot();

            if (presetId == 1)
            {
                CreateSource("Scene 1 wire A", MagneticSourceType.StraightWire, 4, new Vector3(-2.6f, 3.65f, 0f), Quaternion.Euler(0f, 0f, -18f));
                CreateSource("Scene 1 wire B", MagneticSourceType.StraightWire, -4, new Vector3(2.7f, 2.45f, 0f), Quaternion.Euler(0f, 0f, -18f));
                SetStatus("Scene 1: two straight wires, opposite directions.");
            }
            else if (presetId == 2)
            {
                CreateSource("Scene 2a lower wire", MagneticSourceType.StraightWire, 3, new Vector3(-3.8f, 2.3f, 0f), Quaternion.Euler(0f, 0f, 90f));
                CreateSource("Scene 2a upper coil", MagneticSourceType.Solenoid, -3, new Vector3(3.4f, 3.8f, 0f), Quaternion.Euler(0f, 0f, 90f));
                SetStatus("Scene 2: compare straight wire plus coil patterns.");
            }
            else if (presetId == 3)
            {
                CreateSource("Scene 3 wire left", MagneticSourceType.StraightWire, 2, new Vector3(-3.7f, 2.35f, 0f), Quaternion.Euler(0f, 0f, -55f));
                CreateSource("Scene 3 wire top", MagneticSourceType.StraightWire, -5, new Vector3(2.4f, 4.0f, 0f), Quaternion.Euler(0f, 0f, -25f));
                CreateSource("Scene 3 wire right", MagneticSourceType.StraightWire, 3, new Vector3(3.9f, 2.1f, 0f), Quaternion.Euler(0f, 0f, -58f));
                SetStatus("Scene 3: three wires with mixed directions and magnitudes.");
            }
            else if (presetId == 4)
            {
                CreateSource("Scene 4 coil A", MagneticSourceType.Solenoid, -4, new Vector3(-2.5f, 3.0f, 0f), Quaternion.Euler(0f, 0f, 90f));
                CreateSource("Scene 4 coil B", MagneticSourceType.Solenoid, -4, new Vector3(2.5f, 3.0f, 0f), Quaternion.Euler(0f, 0f, 90f));
                SetStatus("Scene 4: two coils, same direction. Flip one with X.");
            }
            else
            {
                CreateSource("Scene 5 Helmholtz coil A", MagneticSourceType.Solenoid, 5, new Vector3(-2.5f, 3.0f, 0f), Quaternion.Euler(0f, 0f, 90f));
                CreateSource("Scene 5 Helmholtz coil B", MagneticSourceType.Solenoid, 5, new Vector3(2.5f, 3.0f, 0f), Quaternion.Euler(0f, 0f, 90f));
                SetStatus("Scene 5: Helmholtz-like pair, inspect the red ROI region.");
            }

            RefreshField();
        }

        public void ClearPresetSources()
        {
            if (presetSourceRoot != null)
            {
                for (int i = presetSourceRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(presetSourceRoot.GetChild(i).gameObject);
                }
            }

            SetStatus("Free lab: energize, move, rotate, then observe.");
            RefreshField();
        }

        public void ToggleFieldLines()
        {
            if (fieldVisualizer == null)
            {
                return;
            }

            fieldVisualizer.SetFieldLinesVisible(!fieldVisualizer.showFieldLines);
            SetStatus(fieldVisualizer.showFieldLines ? "Magnetic field lines: on." : "Magnetic field lines: off.");
        }

        public void ToggleVectors()
        {
            if (fieldVisualizer == null)
            {
                return;
            }

            fieldVisualizer.SetVectorMapVisible(!fieldVisualizer.showVectorMap);
            SetStatus(fieldVisualizer.showVectorMap ? "Direction samples: on." : "Direction samples: off.");
        }

        public void FlipAllSourcesInStage()
        {
            MagneticCurrentSource[] sources = FindObjectsOfType<MagneticCurrentSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null && IsInStage(sources[i]))
                {
                    sources[i].FlipCurrent();
                }
            }

            SetStatus("Flipped current direction for sources in the stage.");
            RefreshField();
        }

        public void AddCurrentToSourcesInStage(int amount)
        {
            MagneticCurrentSource[] sources = FindObjectsOfType<MagneticCurrentSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null && IsInStage(sources[i]))
                {
                    sources[i].SetCurrent(Mathf.Clamp(sources[i].currentLevel + amount, -presetMaxCurrent, presetMaxCurrent));
                }
            }

            SetStatus(amount > 0 ? "Increased stage current levels." : "Decreased stage current levels.");
            RefreshField();
        }

        bool IsInStage(MagneticCurrentSource source)
        {
            return fieldVisualizer == null || fieldVisualizer.Contains(source.transform.position);
        }

        void EnsureRoot()
        {
            if (presetSourceRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("Preset Magnetic Sources");
            presetSourceRoot = root.transform;
        }

        MagneticCurrentSource CreateSource(string objectName, MagneticSourceType sourceType, int currentLevel, Vector3 position, Quaternion rotation)
        {
            GameObject body = new GameObject(objectName);
            body.transform.SetParent(presetSourceRoot, true);
            body.transform.position = position;
            body.transform.rotation = rotation;

            if (sourceType == MagneticSourceType.StraightWire)
            {
                GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cylinder.name = "Straight Wire Body";
                cylinder.transform.SetParent(body.transform, false);
                cylinder.transform.localScale = new Vector3(0.13f, 1.5f, 0.13f);
                CapsuleCollider collider = body.AddComponent<CapsuleCollider>();
                collider.radius = 0.18f;
                collider.height = 3.15f;
                collider.direction = 1;
            }
            else
            {
                CreateRuntimeHelix(body.transform);
                CapsuleCollider collider = body.AddComponent<CapsuleCollider>();
                collider.radius = 0.72f;
                collider.height = 2.65f;
                collider.direction = 1;
                BoxCollider box = body.AddComponent<BoxCollider>();
                box.size = new Vector3(1.45f, 2.65f, 1.1f);
            }

            Rigidbody rigidbody = body.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            MagneticCurrentSource source = body.AddComponent<MagneticCurrentSource>();
            source.sourceType = sourceType;
            source.currentLevel = Mathf.Clamp(currentLevel, -presetMaxCurrent, presetMaxCurrent);
            source.displayName = sourceType == MagneticSourceType.StraightWire ? "Straight wire" : "Solenoid";
            TextMesh label = CreateLabel(body.transform);
            source.currentLabel = label;
            source.UpdateVisuals();

            ElectrostaticHoverAnnotation annotation = body.AddComponent<ElectrostaticHoverAnnotation>();
            annotation.annotation = label;
            annotation.worldOffset = new Vector3(0f, 1.35f, -0.38f);
            return source;
        }

        void CreateRuntimeHelix(Transform parent)
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

        TextMesh CreateLabel(Transform parent)
        {
            GameObject labelObject = new GameObject("Current Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.35f, -0.38f);
            labelObject.transform.localRotation = Quaternion.identity;

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.075f * BaseTextFontSize / HighResolutionTextFontSize;
            label.fontSize = HighResolutionTextFontSize;
            if (label.font != null && label.font.material != null && label.font.material.mainTexture != null)
            {
                label.font.RequestCharactersInTexture(label.text, HighResolutionTextFontSize, FontStyle.Normal);
                label.font.material.mainTexture.filterMode = FilterMode.Trilinear;
                label.font.material.mainTexture.anisoLevel = 8;
            }

            return label;
        }

        void RefreshField()
        {
            if (fieldVisualizer != null)
            {
                fieldVisualizer.ForceRefresh();
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
