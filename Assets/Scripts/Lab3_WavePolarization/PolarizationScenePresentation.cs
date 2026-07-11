using UnityEngine;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class PolarizationScenePresentation : MonoBehaviour
    {
        const int BaseTextFontSize = 48;

        public Camera desktopCamera;
        public bool forceFrontOrthographicCamera = true;
        public bool hideDeviceSimulatorUi = true;
        public Vector3 cameraPosition = new Vector3(0f, 3.65f, -16.5f);
        public float orthographicSize = 6.7f;
        public float legibleOrthographicSize = 5.8f;
        public bool improveLegacyText = true;
        public int textFontSize = 384;

        float nextTextPassTime;

        void Start()
        {
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
            ApplyCamera();
            EnsureSingleAudioListener();
            HideDeviceSimulatorUi();
            DisableVisualizationBlockers();
            ApplyTextPresentation();
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
            GameObject stage = GameObject.Find("Polarization Observation Stage");
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

            PlacePanel("Workflow Panel", new Vector3(-6.75f, 7.05f, -2.55f), new Vector3(3.7f, 1.18f, 0.04f));
            PlacePanel("Report Panel", new Vector3(0f, 7.05f, -2.55f), new Vector3(4.2f, 1.18f, 0.04f));
            PlacePanel("Models Panel", new Vector3(6.75f, 7.05f, -2.55f), new Vector3(3.7f, 1.18f, 0.04f));
            PlacePanel("Incident Wave Label Panel", new Vector3(-5.85f, 4.9f, -0.95f), new Vector3(2.65f, 0.42f, 0.04f));
            PlacePanel("Polarization Preset Help Panel", new Vector3(0f, -1.32f, -5.75f), new Vector3(8.8f, 0.32f, 0.04f));

            TextMesh[] labels = FindObjectsOfType<TextMesh>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TextMesh label = labels[i];
                if (label == null)
                {
                    continue;
                }

                label.fontSize = textFontSize;
                label.characterSize = DesiredCharacterSize(label.gameObject.name);
                if (label.font != null)
                {
                    label.font.RequestCharactersInTexture(label.text, textFontSize, FontStyle.Normal);
                    if (label.font.material != null && label.font.material.mainTexture != null)
                    {
                        label.font.material.mainTexture.filterMode = FilterMode.Trilinear;
                        label.font.material.mainTexture.anisoLevel = 8;
                    }
                }

                PlaceText(label.transform, label.gameObject.name);
            }
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
                label.position = new Vector3(-8.38f, 7.18f, -2.64f);
            }
            else if (objectName == "Report Board")
            {
                label.position = new Vector3(-1.9f, 7.18f, -2.64f);
            }
            else if (objectName == "Models Board")
            {
                label.position = new Vector3(5.15f, 7.18f, -2.64f);
            }
            else if (objectName == "Status")
            {
                label.position = new Vector3(0f, 6.17f, -2.7f);
            }
            else if (objectName == "Stage Label")
            {
                label.position = new Vector3(0f, 5.35f, -0.98f);
                TextMesh textMesh = label.GetComponent<TextMesh>();
                if (textMesh != null)
                {
                    textMesh.text = "Drop films here";
                }
            }
            else if (objectName == "Incident Wave Board")
            {
                label.position = new Vector3(-7.08f, 4.9f, -1.04f);
            }
            else if (objectName == "Polarization Result")
            {
                label.position = new Vector3(6.55f, 4.85f, -0.65f);
            }
            else if (objectName == "Polarization Preset Help")
            {
                label.position = new Vector3(0f, -1.32f, -5.82f);
            }
            else if (objectName == "Input Wave Label")
            {
                label.position = new Vector3(-2.45f, 4.52f, -0.82f);
            }
            else if (objectName == "Output Wave Label")
            {
                label.position = new Vector3(2.25f, 2.08f, -0.82f);
            }
        }

        float DesiredCharacterSize(string objectName)
        {
            float visualSize = 0.076f;
            if (objectName == "Status" || objectName == "Polarization Result")
            {
                visualSize = 0.07f;
            }
            else if (objectName.Contains("Board"))
            {
                visualSize = 0.082f;
            }
            else if (objectName.Contains("Preset Number"))
            {
                visualSize = 0.13f;
            }
            else if (objectName.Contains("Annotation") || objectName.Contains("Hover"))
            {
                visualSize = 0.108f;
            }
            else if (objectName.Contains("Element") || objectName.Contains("Angle"))
            {
                visualSize = objectName.Contains("Angle") ? 0.068f : 0.076f;
            }
            else if (objectName == "Input Wave Label" || objectName == "Output Wave Label")
            {
                visualSize = 0.064f;
            }

            return visualSize * BaseTextFontSize / Mathf.Max(BaseTextFontSize, textFontSize);
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
