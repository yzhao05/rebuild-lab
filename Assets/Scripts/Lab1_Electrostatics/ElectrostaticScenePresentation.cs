using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class ElectrostaticScenePresentation : MonoBehaviour
    {
        [Header("Desktop Camera")]
        public Camera desktopCamera;
        public bool forceFrontOrthographicCamera = true;
        public Vector3 cameraPosition = new Vector3(0f, 3.45f, -15.8f);
        public float orthographicSize = 6.5f;

        [Header("XR Device Simulator UI")]
        public bool hideDeviceSimulatorUi = true;
        public bool shrinkInsteadOfHide;
        public float simulatorUiScale = 0.22f;
        public Vector3 simulatorUiPosition = new Vector3(-4.55f, 2.25f, -2.45f);

        [Header("Text Legibility")]
        public bool improveLegacyText = true;
        public bool ensureVrControlButtons = true;
        public int textFontSize = 384;
        public float legibleOrthographicSize = 5.6f;

        float nextUiPassTime;
        float nextTextPassTime;
        Material runtimeButtonMaterial;
        Material runtimeHighlightedPadMaterial;

        void Awake()
        {
            ApplyCameraPresentation();
            ApplyDeviceSimulatorUiPresentation();
            ApplyTextPresentation();
            ApplyPresetPadHighlights();
            EnsureVrControlButtons();
            EnsureMouseVrPointer();
            EnsureXrRayFallback();
        }

        void LateUpdate()
        {
            if (forceFrontOrthographicCamera)
            {
                ApplyCameraPresentation();
            }

            if (Time.unscaledTime >= nextUiPassTime)
            {
                nextUiPassTime = Time.unscaledTime + 0.5f;
                ApplyDeviceSimulatorUiPresentation();
            }

            if (improveLegacyText && Time.unscaledTime >= nextTextPassTime)
            {
                nextTextPassTime = Time.unscaledTime + 1f;
                ApplyTextPresentation();
                ApplyPresetPadHighlights();
                EnsureVrControlButtons();
                EnsureXrRayFallback();
            }
        }

        void ApplyCameraPresentation()
        {
            Camera cameraToUse = desktopCamera != null ? desktopCamera : Camera.main;
            if (cameraToUse == null)
            {
                return;
            }

            cameraToUse.orthographic = true;
            cameraToUse.orthographicSize = improveLegacyText ? Mathf.Min(orthographicSize, legibleOrthographicSize) : orthographicSize;
            cameraToUse.depth = 10f;
            cameraToUse.transform.position = cameraPosition;
            cameraToUse.transform.rotation = Quaternion.identity;
            EnsureMouseVrPointer(cameraToUse);

            if (!cameraToUse.gameObject.CompareTag("MainCamera"))
            {
                cameraToUse.gameObject.tag = "MainCamera";
            }

            Camera[] cameras = FindObjectsOfType<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i] != cameraToUse && cameras[i].CompareTag("MainCamera"))
                {
                    cameras[i].gameObject.tag = "Untagged";
                }
            }
        }

        void ApplyTextPresentation()
        {
            if (!improveLegacyText)
            {
                return;
            }

            PlacePanel("Tutorial Panel", new Vector3(-6.2f, 5.55f, -2.55f), new Vector3(4.2f, 1.35f, 0.04f));
            PlacePanel("Report Hints Panel", new Vector3(0f, 5.55f, -2.55f), new Vector3(4.4f, 1.35f, 0.04f));
            PlacePanel("Theory Panel", new Vector3(6.2f, 5.55f, -2.55f), new Vector3(4.2f, 1.35f, 0.04f));

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
                label.alignment = objectName.Contains("Board") || objectName.Contains("Report Hints") ? TextAlignment.Left : TextAlignment.Center;
                label.anchor = objectName.Contains("Board") || objectName.Contains("Report Hints") ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;

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

        void ApplyPresetPadHighlights()
        {
            for (int presetId = 1; presetId <= 5; presetId++)
            {
                bool highlighted = presetId == 1 || presetId == 2 || presetId == 5;
                GameObject pad = GameObject.Find("Preset Pad " + presetId);
                if (pad != null)
                {
                    pad.transform.localScale = highlighted ? new Vector3(1.85f, 0.22f, 0.9f) : new Vector3(1.55f, 0.16f, 0.72f);
                    Renderer renderer = pad.GetComponent<Renderer>();
                    if (renderer != null && highlighted)
                    {
                        renderer.sharedMaterial = RuntimeHighlightedPadMaterial();
                    }
                }

                TextMesh number = FindTextMesh("Preset Pad Number " + presetId);
                if (number != null)
                {
                    number.color = highlighted ? Color.white : Color.black;
                    number.characterSize = (highlighted ? 0.145f : 0.12f) * 48f / Mathf.Max(48f, textFontSize);
                }
            }
        }

        TextMesh FindTextMesh(string objectName)
        {
            GameObject textObject = GameObject.Find(objectName);
            return textObject != null ? textObject.GetComponent<TextMesh>() : null;
        }

        void EnsureVrControlButtons()
        {
            if (!ensureVrControlButtons || GameObject.Find("Clear Button") != null)
            {
                return;
            }

            ElectrostaticLabController controller = GetComponent<ElectrostaticLabController>();
            if (controller == null)
            {
                controller = FindObjectOfType<ElectrostaticLabController>();
            }

            if (controller == null)
            {
                return;
            }

            CreateRuntimeLabButton("Clear Button", "C", "Clear\nremove presets", new Vector3(-2.8f, 0.64f, -6.15f), ElectrostaticLabButton.LabButtonAction.ClearCharges, controller);
            CreateRuntimeLabButton("Field Lines Button", "F", "Field lines\nshow / hide", new Vector3(0f, 0.64f, -6.15f), ElectrostaticLabButton.LabButtonAction.ToggleFieldLines, controller);
            CreateRuntimeLabButton("Vectors Button", "V", "Vectors\nshow / hide", new Vector3(2.8f, 0.64f, -6.15f), ElectrostaticLabButton.LabButtonAction.ToggleVectors, controller);
        }

        void CreateRuntimeLabButton(string objectName, string faceText, string hoverText, Vector3 position, ElectrostaticLabButton.LabButtonAction action, ElectrostaticLabController controller)
        {
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = objectName;
            button.transform.position = position;
            button.transform.localScale = new Vector3(1.45f, 0.18f, 0.72f);

            Renderer renderer = button.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = RuntimeButtonMaterial();
            }

            button.AddComponent<XRSimpleInteractable>();
            ElectrostaticLabButton labButton = button.AddComponent<ElectrostaticLabButton>();
            labButton.controller = controller;
            labButton.action = action;

            CreateRuntimeText(objectName + " Label", faceText, position + new Vector3(0f, 0.24f, -0.13f), 0.12f, Color.black, TextAnchor.MiddleCenter);
            TextMesh annotation = CreateRuntimeText(objectName + " Annotation", hoverText, position + new Vector3(0f, 0.6f, -0.35f), 0.078f, Color.white, TextAnchor.MiddleCenter);

            ElectrostaticHoverAnnotation hoverAnnotation = button.AddComponent<ElectrostaticHoverAnnotation>();
            hoverAnnotation.annotation = annotation;
            hoverAnnotation.worldOffset = new Vector3(0f, 0.52f, -0.35f);
            annotation.gameObject.SetActive(false);
        }

        TextMesh CreateRuntimeText(string objectName, string text, Vector3 position, float visualCharacterSize, Color color, TextAnchor anchor)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.position = position;
            textObject.transform.rotation = Quaternion.identity;

            TextMesh label = textObject.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = textFontSize;
            label.characterSize = visualCharacterSize * 48f / Mathf.Max(48f, textFontSize);
            label.anchor = anchor;
            label.alignment = anchor == TextAnchor.MiddleLeft ? TextAlignment.Left : TextAlignment.Center;
            label.color = color;
            return label;
        }

        Material RuntimeButtonMaterial()
        {
            if (runtimeButtonMaterial == null)
            {
                runtimeButtonMaterial = new Material(Shader.Find("Standard"));
                runtimeButtonMaterial.color = new Color(0.95f, 0.82f, 0.25f, 1f);
            }

            return runtimeButtonMaterial;
        }

        Material RuntimeHighlightedPadMaterial()
        {
            if (runtimeHighlightedPadMaterial == null)
            {
                runtimeHighlightedPadMaterial = new Material(Shader.Find("Standard"));
                runtimeHighlightedPadMaterial.color = new Color(1f, 0.08f, 0.03f, 1f);
            }

            return runtimeHighlightedPadMaterial;
        }

        void PlacePanel(string objectName, Vector3 position, Vector3 scale)
        {
            GameObject panel = GameObject.Find(objectName);
            if (panel == null)
            {
                return;
            }

            panel.transform.position = position;
            panel.transform.localScale = scale;
        }

        void PlaceText(Transform label, string objectName)
        {
            if (objectName == "Tutorial Board")
            {
                label.position = new Vector3(-8.05f, 5.86f, -2.64f);
            }
            else if (objectName == "Report Hints")
            {
                label.position = new Vector3(-1.95f, 5.86f, -2.64f);
            }
            else if (objectName == "Theory Board")
            {
                label.position = new Vector3(4.35f, 5.86f, -2.64f);
            }
            else if (objectName == "Stage Label")
            {
                label.position = new Vector3(0f, 6.28f, -0.98f);
            }
            else if (objectName == "Preset Help")
            {
                label.position = new Vector3(0f, 0.29f, -5.82f);
            }
        }

        float DesiredCharacterSize(string objectName)
        {
            float visualSize = 0.075f;

            if (objectName == "Status")
            {
                visualSize = 0.065f;
            }
            else if (objectName.Contains("Preset Pad Number"))
            {
                visualSize = 0.15f;
            }
            else if (objectName.Contains("Preset Pad Annotation"))
            {
                visualSize = 0.13f;
            }
            else if (objectName.Contains("Charge Label"))
            {
                visualSize = 0.12f;
            }
            else if (objectName.Contains("Charger Label"))
            {
                visualSize = 0.11f;
            }
            else if (objectName == "Preset Help")
            {
                visualSize = 0.07f;
            }
            else if (objectName.Contains("Board") || objectName.Contains("Report Hints"))
            {
                visualSize = 0.082f;
            }

            return visualSize * 48f / Mathf.Max(48f, textFontSize);
        }

        string LegibleText(string objectName, string currentText)
        {
            if (objectName == "Tutorial Board")
            {
                return "Workflow\n1 Grip a charge\n2 Hold in red/blue\n3 Place in cube\n4 Select buttons";
            }

            if (objectName == "Theory Board")
            {
                return "Models\nPoint: E ~ q/r^2\nLine: E ~ lambda/r\nPlate: E ~ sigma/2eps\nTotal: vector sum";
            }

            if (objectName == "Report Hints")
            {
                return "Report\nQ1-Q2 line charges\nQ3-Q4 superposition\nQ5 top/front views\nQ6 plate ROI\nQ7 point rows";
            }

            if (objectName == "Preset Help")
            {
                return "VR: point ray or touch, then select";
            }

            if (objectName.Contains("Preset Pad Annotation"))
            {
                if (objectName.EndsWith("1"))
                {
                    return "Scene 1\nUnequal lines";
                }

                if (objectName.EndsWith("2"))
                {
                    return "Scene 2\nPoint + line";
                }

                if (objectName.EndsWith("3"))
                {
                    return "Scene 3\nLine + plate";
                }

                if (objectName.EndsWith("4"))
                {
                    return "Scene 4\nOpposite plates";
                }

                return "Scene 5\nPoint rows";
            }

            if (objectName == "Status")
            {
                return ShortStatus(currentText);
            }

            return currentText;
        }

        string ShortStatus(string currentText)
        {
            if (currentText.Contains("Preset 1"))
            {
                return "Preset 1: unequal opposite line charges.";
            }

            if (currentText.Contains("Preset 2"))
            {
                return "Preset 2: point charge plus line charge.";
            }

            if (currentText.Contains("Preset 3"))
            {
                return "Preset 3: line charge plus surface charge.";
            }

            if (currentText.Contains("Preset 4"))
            {
                return "Preset 4: opposite plates create a near-uniform ROI.";
            }

            if (currentText.Contains("Preset 5") || currentText.Contains("Alt"))
            {
                return "Preset 5: point-charge rows approximate plates.";
            }

            return "Free lab: charge objects, then place them in the cube.";
        }

        void EnsureMouseVrPointer()
        {
            Camera cameraToUse = desktopCamera != null ? desktopCamera : Camera.main;
            if (cameraToUse != null)
            {
                EnsureMouseVrPointer(cameraToUse);
            }
        }

        void EnsureMouseVrPointer(Camera cameraToUse)
        {
            if (cameraToUse != null && cameraToUse.GetComponent<ElectrostaticMouseVrPointer>() == null)
            {
                cameraToUse.gameObject.AddComponent<ElectrostaticMouseVrPointer>();
            }
        }

        void EnsureXrRayFallback()
        {
            if (GetComponent<ElectrostaticXrRayFallback>() == null)
            {
                gameObject.AddComponent<ElectrostaticXrRayFallback>();
            }
        }

        void ApplyDeviceSimulatorUiPresentation()
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || !IsDeviceSimulatorObject(canvas.gameObject))
                {
                    continue;
                }

                if (hideDeviceSimulatorUi && !shrinkInsteadOfHide)
                {
                    canvas.gameObject.SetActive(false);
                    continue;
                }

                canvas.gameObject.SetActive(true);
                canvas.transform.position = simulatorUiPosition;
                canvas.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                canvas.transform.localScale = Vector3.one * simulatorUiScale;
            }
        }

        static bool IsDeviceSimulatorObject(GameObject candidate)
        {
            Transform current = candidate.transform;
            while (current != null)
            {
                string objectName = current.name;
                if (objectName.Contains("XR Device Simulator"))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
