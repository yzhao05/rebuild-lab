using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class ElectrostaticLabController : MonoBehaviour
    {
        const int BaseTextFontSize = 48;
        const int HighResolutionTextFontSize = 384;

        public ElectrostaticFieldVisualizer fieldVisualizer;
        public Transform presetChargeRoot;
        public TextMesh statusLabel;
        public int presetMaxCharge = 6;

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
            if (Input.GetKeyDown(KeyCode.C))
            {
                ClearPresetCharges();
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

        public void ApplyPreset(int presetId)
        {
            ClearPresetCharges();

            if (presetChargeRoot == null)
            {
                GameObject root = new GameObject("Preset Charges");
                presetChargeRoot = root.transform;
            }

            if (presetId == 1)
            {
                CreateCharge("Scene 1 Left line +2", ChargeDistribution.Line, 2, new Vector3(-3.8f, 3.0f, 0f));
                CreateCharge("Scene 1 Right line -4", ChargeDistribution.Line, -4, new Vector3(3.8f, 3.0f, 0f));
                SetStatus("Preset 1: unequal opposite line charges.");
            }
            else if (presetId == 2)
            {
                CreateCharge("Scene 2 Positive point +2", ChargeDistribution.Point, 2, new Vector3(-3.3f, 3.0f, 0f));
                CreateCharge("Scene 2 Negative line -3", ChargeDistribution.Line, -3, new Vector3(3.5f, 3.0f, 0f));
                SetStatus("Preset 2: point charge plus line charge.");
            }
            else if (presetId == 3)
            {
                CreateCharge("Scene 3 Left line +4", ChargeDistribution.Line, 4, new Vector3(-4.0f, 3.0f, 0f));
                CreateCharge("Scene 3 Right surface -2", ChargeDistribution.Surface, -2, new Vector3(3.5f, 3.0f, 0f));
                SetStatus("Preset 3: line charge plus surface charge.");
            }
            else if (presetId == 4)
            {
                CreateCharge("Scene 4 Left surface +3", ChargeDistribution.Surface, 3, new Vector3(-4.5f, 3.0f, 0f));
                CreateCharge("Scene 4 Right surface -3", ChargeDistribution.Surface, -3, new Vector3(4.5f, 3.0f, 0f));
                SetStatus("Preset 4: opposite plates create a near-uniform ROI.");
            }
            else
            {
                CreateCharge("Alternative point row +1 A", ChargeDistribution.Point, 1, new Vector3(-4.9f, 4.1f, 0f));
                CreateCharge("Alternative point row +1 B", ChargeDistribution.Point, 1, new Vector3(-4.9f, 3.0f, 0f));
                CreateCharge("Alternative point row +1 C", ChargeDistribution.Point, 1, new Vector3(-4.9f, 1.9f, 0f));
                CreateCharge("Alternative point row -1 A", ChargeDistribution.Point, -1, new Vector3(4.9f, 4.1f, 0f));
                CreateCharge("Alternative point row -1 B", ChargeDistribution.Point, -1, new Vector3(4.9f, 3.0f, 0f));
                CreateCharge("Alternative point row -1 C", ChargeDistribution.Point, -1, new Vector3(4.9f, 1.9f, 0f));
                SetStatus("Preset 5: point-charge rows approximate plates.");
            }

            if (fieldVisualizer != null)
            {
                fieldVisualizer.ForceRefresh();
            }
        }

        public void ClearPresetCharges()
        {
            if (presetChargeRoot != null)
            {
                for (int i = presetChargeRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(presetChargeRoot.GetChild(i).gameObject);
                }
            }

            SetStatus("Free lab: charge objects, then place them in the cube.");

            if (fieldVisualizer != null)
            {
                fieldVisualizer.ForceRefresh();
            }
        }

        public void ToggleFieldLines()
        {
            if (fieldVisualizer == null)
            {
                return;
            }

            fieldVisualizer.SetFieldLinesVisible(!fieldVisualizer.showFieldLines);
            SetStatus(fieldVisualizer.showFieldLines ? "Field lines: on." : "Field lines: off.");
        }

        public void ToggleVectors()
        {
            if (fieldVisualizer == null)
            {
                return;
            }

            fieldVisualizer.SetVectorMapVisible(!fieldVisualizer.showVectorMap);
            SetStatus(fieldVisualizer.showVectorMap ? "Vector map: on." : "Vector map: off.");
        }

        ElectrostaticCharge CreateCharge(string objectName, ChargeDistribution distribution, int chargeUnits, Vector3 position)
        {
            GameObject body;
            if (distribution == ChargeDistribution.Point)
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                body.transform.localScale = Vector3.one * 0.42f;
            }
            else if (distribution == ChargeDistribution.Line)
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                body.transform.localScale = new Vector3(0.16f, 1.25f, 0.16f);
            }
            else
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.transform.localScale = new Vector3(0.14f, 2.15f, 1.05f);
            }

            body.name = objectName;
            body.transform.position = position;
            body.transform.SetParent(presetChargeRoot, true);

            Rigidbody rigidbody = body.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            body.AddComponent<XRSimpleInteractable>();

            ElectrostaticCharge charge = body.AddComponent<ElectrostaticCharge>();
            charge.distribution = distribution;
            charge.displayName = DistributionName(distribution);
            charge.chargeUnits = Mathf.Clamp(chargeUnits, -presetMaxCharge, presetMaxCharge);

            TextMesh label = CreateLabel(body.transform, new Vector3(0f, 1.25f, -0.35f));
            charge.chargeLabel = label;
            charge.UpdateVisuals();
            ElectrostaticHoverAnnotation annotation = body.AddComponent<ElectrostaticHoverAnnotation>();
            annotation.annotation = label;
            annotation.worldOffset = new Vector3(0f, 1.25f, -0.35f);

            return charge;
        }

        TextMesh CreateLabel(Transform parent, Vector3 localPosition)
        {
            GameObject labelObject = new GameObject("Charge Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.identity;

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.075f * BaseTextFontSize / HighResolutionTextFontSize;
            label.fontSize = HighResolutionTextFontSize;
            if (label.font != null && label.font.material != null && label.font.material.mainTexture != null)
            {
                label.font.material.mainTexture.filterMode = FilterMode.Bilinear;
            }

            return label;
        }

        string DistributionName(ChargeDistribution distribution)
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

        void SetStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message;
            }
        }
    }
}
