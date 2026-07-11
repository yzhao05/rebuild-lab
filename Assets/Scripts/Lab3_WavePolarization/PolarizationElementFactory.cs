using UnityEngine;

namespace ElectrostaticsLab
{
    public static class PolarizationElementFactory
    {
        const int BaseTextFontSize = 48;
        const int HighResolutionTextFontSize = 384;

        public static GameObject CreateRuntimeElement(string objectName, PolarizationElementType type, Vector3 position, float angleDegrees)
        {
            GameObject root = new GameObject(objectName);
            root.transform.position = position;

            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Polarization Plate";
            plate.transform.SetParent(root.transform, false);
            plate.transform.localScale = new Vector3(1.05f, 1.05f, 0.08f);
            DestroyCollider(plate.GetComponent<Collider>());

            GameObject axis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            axis.name = type == PolarizationElementType.LinearPolarizer ? "Pass Axis" : "Fast Axis";
            axis.transform.SetParent(root.transform, false);
            axis.transform.localScale = new Vector3(0.9f, 0.055f, 0.105f);
            DestroyCollider(axis.GetComponent<Collider>());

            if (type == PolarizationElementType.QuarterWavePlate)
            {
                GameObject slowAxis = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slowAxis.name = "Slow Axis";
                slowAxis.transform.SetParent(root.transform, false);
                slowAxis.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                slowAxis.transform.localScale = new Vector3(0.82f, 0.04f, 0.105f);
                DestroyCollider(slowAxis.GetComponent<Collider>());
            }

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            PolarizationElement element = root.AddComponent<PolarizationElement>();
            element.elementType = type;
            element.displayName = type == PolarizationElementType.LinearPolarizer ? "Linear polarizer" : "Quarter-wave plate";
            element.axisAngleDegrees = angleDegrees;
            element.axisVisual = axis.transform;
            element.label = CreateLabel(root.transform, "Element Label", string.Empty, new Vector3(0f, 0.68f, -0.22f), 0.064f, Color.white);
            element.angleLabel = CreateLabel(root.transform, "Angle Label", string.Empty, new Vector3(0f, -0.68f, -0.22f), 0.058f, Color.white);

            root.AddComponent<PolarizationDesktopDraggable>();
            AddXRGrabInteractable(root);
            element.SetAngle(angleDegrees);
            return root;
        }

        static void DestroyCollider(Collider collider)
        {
            if (collider == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(collider);
            }
            else
            {
                Object.DestroyImmediate(collider);
            }
        }

        static void AddXRGrabInteractable(GameObject target)
        {
            System.Type type = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
            if (type != null && target.GetComponent(type) == null)
            {
                target.AddComponent(type);
            }
        }

        static TextMesh CreateLabel(Transform parent, string objectName, string text, Vector3 localPosition, float characterSize, Color color)
        {
            GameObject labelObject = new GameObject(objectName);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.identity;

            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = characterSize * BaseTextFontSize / HighResolutionTextFontSize;
            label.fontSize = HighResolutionTextFontSize;
            label.color = color;
            if (label.font != null && label.font.material != null && label.font.material.mainTexture != null)
            {
                label.font.RequestCharactersInTexture(text, HighResolutionTextFontSize, FontStyle.Normal);
                label.font.material.mainTexture.filterMode = FilterMode.Trilinear;
                label.font.material.mainTexture.anisoLevel = 8;
            }

            return label;
        }
    }
}
