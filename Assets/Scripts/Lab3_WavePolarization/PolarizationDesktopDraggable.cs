using UnityEngine;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class PolarizationDesktopDraggable : MonoBehaviour, IEmVrMouseDraggable
    {
        public static PolarizationElement CurrentTarget { get; private set; }

        public float dragPlaneZ;
        public float fallbackDragPlaneZ = -0.45f;
        public float dragSensitivity = 3f;
        public float rotateDegreesPerPixel = 0.22f;
        public float keyRotateDegreesPerSecond = 72f;

        Camera dragCamera;
        Rigidbody cachedRigidbody;
        PolarizationElement element;
        Vector3 previousMouse;
        bool dragging;
        int activePointerId;

        void Awake()
        {
            dragPlaneZ = transform.position.z;
            cachedRigidbody = GetComponent<Rigidbody>();
            element = GetComponent<PolarizationElement>();
            ConfigureRigidbody();
            EnsureStableCollider();
        }

        void Update()
        {
            if (CurrentTarget != element || element == null)
            {
                return;
            }

            float direction = 0f;
            if (Input.GetKey(KeyCode.Q))
            {
                direction += 1f;
            }
            if (Input.GetKey(KeyCode.E))
            {
                direction -= 1f;
            }

            if (Mathf.Abs(direction) > 0.01f)
            {
                element.AddAngle(direction * keyRotateDegreesPerSecond * Time.deltaTime);
            }
        }

        void OnMouseDown()
        {
            if (dragging)
            {
                return;
            }

            activePointerId = 1;
            BeginMouseDrag(Camera.main, Input.mousePosition);
        }

        public void BeginMouseDrag(Camera cameraToUse, Vector3 screenPosition)
        {
            if (dragging)
            {
                return;
            }

            activePointerId = activePointerId == 0 ? 2 : activePointerId;
            dragCamera = cameraToUse;
            if (dragCamera == null)
            {
                return;
            }

            ConfigureRigidbody();
            dragging = true;
            dragPlaneZ = ResolveDragPlaneZ();
            Vector3 snappedPosition = transform.position;
            snappedPosition.z = dragPlaneZ;
            transform.position = snappedPosition;
            previousMouse = screenPosition;
            CurrentTarget = element;
        }

        void OnMouseDrag()
        {
            if (activePointerId == 1)
            {
                ContinueMouseDrag(Input.mousePosition);
            }
        }

        public void ContinueMouseDrag(Vector3 screenPosition)
        {
            if (!dragging || dragCamera == null)
            {
                return;
            }

            Vector3 mouseDelta = screenPosition - previousMouse;
            if (Input.GetKey(KeyCode.R) && element != null)
            {
                element.AddAngle(-mouseDelta.x * rotateDegreesPerPixel + mouseDelta.y * rotateDegreesPerPixel);
            }
            else
            {
                Vector3 targetPosition = transform.position + ScreenDeltaToWorldDelta(mouseDelta);
                targetPosition.z = dragPlaneZ;
                if (cachedRigidbody != null)
                {
                    cachedRigidbody.MovePosition(targetPosition);
                }

                transform.position = targetPosition;
            }

            previousMouse = screenPosition;
        }

        void OnMouseUp()
        {
            if (activePointerId == 1)
            {
                EndMouseDrag();
            }
        }

        public void EndMouseDrag()
        {
            dragging = false;
            activePointerId = 0;
            ConfigureRigidbody();
        }

        void ConfigureRigidbody()
        {
            if (cachedRigidbody == null)
            {
                return;
            }

            cachedRigidbody.useGravity = false;
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.velocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
        }

        Vector3 ScreenDeltaToWorldDelta(Vector3 screenDelta)
        {
            if (dragCamera == null)
            {
                return Vector3.zero;
            }

            if (dragCamera.orthographic)
            {
                float unitsPerPixel = (dragCamera.orthographicSize * 2f) / Mathf.Max(1f, dragCamera.pixelHeight);
                return (dragCamera.transform.right * screenDelta.x + dragCamera.transform.up * screenDelta.y) * unitsPerPixel * dragSensitivity;
            }

            return (dragCamera.transform.right * screenDelta.x + dragCamera.transform.up * screenDelta.y) * 0.01f * dragSensitivity;
        }

        float ResolveDragPlaneZ()
        {
            PolarizationWaveVisualizer visualizer = FindObjectOfType<PolarizationWaveVisualizer>();
            if (visualizer != null)
            {
                return visualizer.transform.position.z - 0.2f;
            }

            return fallbackDragPlaneZ;
        }

        void EnsureStableCollider()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null)
            {
                box = gameObject.AddComponent<BoxCollider>();
            }

            box.isTrigger = false;
            box.center = Vector3.zero;
            box.size = new Vector3(1.35f, 1.35f, 0.48f);
        }
    }
}
