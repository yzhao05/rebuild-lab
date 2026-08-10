using UnityEngine;
using ChargedObjects;
using ChargePlane = ChargedObjects.Plane;

namespace EMVR.Lab1Rebuild
{
    [RequireComponent(typeof(Collider))]
    public sealed class Lab1Summer26SourceRuntime : MonoBehaviour
    {
        public static Lab1Summer26SourceRuntime Selected { get; private set; }

        public TextMesh label;
        public string displayName = "charge";
        public float dragSensitivity = 1.8f;

        private Camera mainCamera;
        private Vector3 previousMousePosition;
        private float fixedZ;
        private Renderer cachedRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Sphere sphere;
        private Wire wire;
        private ChargePlane plane;

        private void Awake()
        {
            sphere = GetComponent<Sphere>();
            wire = GetComponent<Wire>();
            plane = GetComponent<ChargePlane>();
            cachedRenderer = GetComponentInChildren<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
            RefreshVisuals();
        }

        private void Update()
        {
            RefreshVisuals();
        }

        private void OnMouseDown()
        {
            mainCamera = Camera.main;
            previousMousePosition = Input.mousePosition;
            fixedZ = transform.position.z;
            Selected = this;
            Lab1RebuildStatus.Set("selected " + displayName + ", q = " + GetCharge().ToString("0"));
        }

        private void OnMouseDrag()
        {
            if (mainCamera == null) return;

            Vector3 delta = Input.mousePosition - previousMousePosition;
            float worldHeight = mainCamera.orthographicSize * 2f;
            float worldWidth = worldHeight * mainCamera.aspect;
            Vector3 worldDelta = new Vector3(
                delta.x / Mathf.Max(Screen.width, 1) * worldWidth,
                delta.y / Mathf.Max(Screen.height, 1) * worldHeight,
                0f) * dragSensitivity;

            transform.position = new Vector3(transform.position.x + worldDelta.x, transform.position.y + worldDelta.y, fixedZ);
            previousMousePosition = Input.mousePosition;
        }

        public float GetCharge()
        {
            if (sphere != null) return sphere.electric_mag;
            if (wire != null) return wire.electric_mag;
            if (plane != null) return plane.electric_mag;
            return 0f;
        }

        public void SetCharge(float value)
        {
            float charge = Mathf.Clamp(Mathf.Round(value), -5f, 5f);
            if (sphere != null) sphere.electric_mag = charge;
            if (wire != null) wire.electric_mag = charge;
            if (plane != null) plane.electric_mag = charge;
            RefreshVisuals();
        }

        public void AddCharge(float delta)
        {
            SetCharge(GetCharge() + delta);
        }

        private void RefreshVisuals()
        {
            float charge = GetCharge();
            if (label != null)
            {
                label.text = displayName + "\nq = " + charge.ToString("0");
            }

            if (cachedRenderer == null) return;
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

            Color color = Color.white;
            float strength = Mathf.Clamp01(Mathf.Abs(charge) / 5f);
            if (charge > 0f) color = Color.Lerp(new Color(1f, 0.86f, 0.76f), new Color(1f, 0.1f, 0.05f), strength);
            if (charge < 0f) color = Color.Lerp(new Color(0.78f, 0.9f, 1f), new Color(0.08f, 0.32f, 1f), strength);

            cachedRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", color);
            cachedRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
