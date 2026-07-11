using UnityEngine;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class MagneticScenePresentation : MonoBehaviour
    {
        const int BaseTextFontSize = 48;

        [Header("Desktop Camera")]
        public Camera desktopCamera;
        public bool forceFrontOrthographicCamera = true;
        public bool hideDeviceSimulatorUi = true;
        public Vector3 cameraPosition = new Vector3(0f, 3.65f, -16.5f);
        public float orthographicSize = 6.7f;
        public float legibleOrthographicSize = 5.6f;

        [Header("Text Legibility")]
        public bool improveLegacyText = true;
        public int textFontSize = 384;

        float nextTextPassTime;

        void Awake()
        {
            NormalizeLegibilityDefaults();
        }

        void Start()
        {
            NormalizeLegibilityDefaults();
            ApplyPresentation();
        }

        void LateUpdate()
        {
            if (forceFrontOrthographicCamera)
            {
                ApplyCamera();
            }

            if (hideDeviceSimulatorUi)
            {
                HideDeviceSimulatorUi();
            }

            if (improveLegacyText && Time.unscaledTime >= nextTextPassTime)
            {
                nextTextPassTime = Time.unscaledTime + 1f;
                ApplyTextPresentation();
            }
        }

        public void ApplyPresentation()
        {
            NormalizeLegibilityDefaults();
            ApplyCamera();
            EnsureSingleAudioListener();
            HideDeviceSimulatorUi();
            DisableVisualizationBlockers();
            ApplyTextPresentation();
        }

        void NormalizeLegibilityDefaults()
        {
            if (improveLegacyText)
            {
                textFontSize = 384;
                legibleOrthographicSize = 5.6f;
            }
        }

        void ApplyCamera()
        {
            Camera cameraToUse = desktopCamera != null ? desktopCamera : Camera.main;
            if (cameraToUse == null)
            {
                return;
            }

            cameraToUse.orthographic = true;
            cameraToUse.orthographicSize = improveLegacyText ? Mathf.Min(orthographicSize, legibleOrthographicSize) : orthographicSize;
            cameraToUse.transform.position = cameraPosition;
            cameraToUse.transform.rotation = Quaternion.identity;
            cameraToUse.tag = "MainCamera";
        }

        void DisableVisualizationBlockers()
        {
            GameObject stage = GameObject.Find("Magnetic Field Visualization Cube");
            if (stage == null)
            {
                return;
            }

            Collider[] colliders = stage.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = false;
                }
            }
        }

        void ApplyTextPresentation()
        {
            if (!improveLegacyText)
            {
                return;
            }

            PlacePanel("Workflow Panel", new Vector3(-6.65f, 5.55f, -2.55f), new Vector3(4.0f, 1.45f, 0.04f));
            PlacePanel("Report Panel", new Vector3(0f, 5.55f, -2.55f), new Vector3(4.4f, 1.45f, 0.04f));
            PlacePanel("Models Panel", new Vector3(6.65f, 5.55f, -2.55f), new Vector3(4.0f, 1.45f, 0.04f));
            PlacePanel("Magnetic Preset Help Panel", new Vector3(0f, -0.08f, -5.75f), new Vector3(7.4f, 0.34f, 0.04f));

            TextMesh[] labels = FindObjectsOfType<TextMesh>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TextMesh label = labels[i];
                if (label == null)
                {
                    continue;
                }

                string objectName = label.gameObject.name;
                label.fontSize = textFontSize;
                label.characterSize = DesiredCharacterSize(objectName);
                label.text = LegibleText(objectName, label.text);
                label.alignment = IsLeftAlignedText(objectName) ? TextAlignment.Left : TextAlignment.Center;
                label.anchor = IsLeftAlignedText(objectName) ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;

                Font font = label.font;
                if (font != null)
                {
                    font.RequestCharactersInTexture(label.text, textFontSize, FontStyle.Normal);
                    if (font.material != null && font.material.mainTexture != null)
                    {
                        font.material.mainTexture.filterMode = FilterMode.Trilinear;
                        font.material.mainTexture.anisoLevel = 8;
                    }
                }

                PlaceText(label.transform, objectName);
            }
        }

        static bool IsLeftAlignedText(string objectName)
        {
            return objectName == "Workflow Board" || objectName == "Report Board" || objectName == "Models Board";
        }

        static void PlacePanel(string objectName, Vector3 position, Vector3 scale)
        {
            GameObject panel = GameObject.Find(objectName);
            if (panel == null)
            {
                return;
            }

            panel.transform.position = position;
            panel.transform.localScale = scale;
        }

        static void PlaceText(Transform label, string objectName)
        {
            if (objectName == "Workflow Board")
            {
                label.position = new Vector3(-8.42f, 5.88f, -2.64f);
            }
            else if (objectName == "Report Board")
            {
                label.position = new Vector3(-1.95f, 5.88f, -2.64f);
            }
            else if (objectName == "Models Board")
            {
                label.position = new Vector3(4.9f, 5.88f, -2.64f);
            }
            else if (objectName == "Stage Label")
            {
                label.position = new Vector3(0f, 6.22f, -0.98f);
            }
            else if (objectName == "Magnetic Preset Help")
            {
                label.position = new Vector3(0f, -0.08f, -5.82f);
            }
        }

        float DesiredCharacterSize(string objectName)
        {
            float visualSize = 0.075f;

            if (objectName == "Status")
            {
                visualSize = 0.065f;
            }
            else if (objectName == "Workflow Board" || objectName == "Models Board")
            {
                visualSize = 0.082f;
            }
            else if (objectName == "Report Board")
            {
                visualSize = 0.082f;
            }
            else if (objectName == "Stage Label")
            {
                visualSize = 0.07f;
            }
            else if (objectName.Contains("Magnetic Preset Number"))
            {
                visualSize = objectName.EndsWith("1") || objectName.EndsWith("4") || objectName.EndsWith("5") ? 0.145f : 0.12f;
            }
            else if (objectName.Contains("Magnetic Preset Annotation"))
            {
                visualSize = 0.11f;
            }
            else if (objectName.Contains("Current Loader Label"))
            {
                visualSize = 0.072f;
            }
            else if (objectName.Contains("Current Loader Hover"))
            {
                visualSize = 0.11f;
            }
            else if (objectName.Contains("Current Label"))
            {
                visualSize = 0.12f;
            }
            else if (objectName.Contains("Button") && objectName.Contains("Label"))
            {
                visualSize = 0.12f;
            }
            else if (objectName.Contains("Button") && objectName.Contains("Annotation"))
            {
                visualSize = 0.11f;
            }
            else if (objectName == "Magnetic Preset Help")
            {
                visualSize = 0.07f;
            }

            return visualSize * BaseTextFontSize / Mathf.Max(BaseTextFontSize, textFontSize);
        }

        static string LegibleText(string objectName, string currentText)
        {
            if (objectName == "Workflow Board")
            {
                return "Workflow\n1 Drag source\n2 Set current\n3 Place in cube";
            }

            if (objectName == "Models Board")
            {
                return "Models\nWire: B curls\nCoil: loop field\nB_total: sum";
            }

            if (objectName == "Report Board")
            {
                return "Report\nQ1-Q4 sources\nQ5 coil pair\nQ6 Helmholtz";
            }

            if (objectName == "Magnetic Preset Help")
            {
                return "1-5 presets | C clear | F lines | V samples | X flip | +/- current";
            }

            if (objectName == "Status" && currentText.Contains("Rotate with R"))
            {
                return "Energize source, place it in the cube, then observe B-lines.";
            }

            return currentText;
        }

        void EnsureSingleAudioListener()
        {
            AudioListener[] listeners = FindObjectsOfType<AudioListener>(true);
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

        void HideDeviceSimulatorUi()
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas != null && IsDeviceSimulatorObject(canvas.gameObject))
                {
                    canvas.gameObject.SetActive(false);
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
    }
}
