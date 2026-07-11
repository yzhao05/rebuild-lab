using UnityEngine;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class MagneticDesktopDraggable : MonoBehaviour, IEmVrMouseDraggable
    {
        public static MagneticCurrentSource CurrentTarget { get; private set; }

        public float dragPlaneZ;
        public float fallbackDragPlaneZ;
        public float dragSensitivity = 2f;
        public float rotateDegreesPerPixel = 0.22f;

        Camera dragCamera;
        Rigidbody cachedRigidbody;
        MagneticCurrentSource source;
        Vector3 dragOffset;
        Vector3 previousMouse;
        bool previousKinematic;
        bool dragging;
        int activePointerId;
        float nextEnergizeTime;

        void Awake()
        {
            dragPlaneZ = transform.position.z;
            cachedRigidbody = GetComponent<Rigidbody>();
            source = GetComponent<MagneticCurrentSource>();
            ConfigureRigidbodyForDesktopDrag();
            EnsureStableDragCollider();
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
            ConfigureRigidbodyForDesktopDrag();
            dragCamera = cameraToUse;
            if (dragCamera == null)
            {
                return;
            }

            dragging = true;
            dragPlaneZ = ResolveDragPlaneZ();
            Vector3 snappedPosition = transform.position;
            snappedPosition.z = dragPlaneZ;
            transform.position = snappedPosition;
            previousMouse = screenPosition;
            CurrentTarget = source;

            if (cachedRigidbody != null)
            {
                previousKinematic = true;
                cachedRigidbody.velocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
                cachedRigidbody.isKinematic = true;
            }
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

            Vector3 targetPosition = transform.position + ScreenDeltaToWorldDelta(screenPosition - previousMouse);
            targetPosition.z = dragPlaneZ;
            if (cachedRigidbody != null)
            {
                cachedRigidbody.MovePosition(targetPosition);
            }

            transform.position = targetPosition;

            if (Input.GetKey(KeyCode.R))
            {
                Vector3 delta = screenPosition - previousMouse;
                transform.Rotate(Vector3.forward, -delta.x * rotateDegreesPerPixel, Space.World);
                transform.Rotate(Vector3.right, delta.y * rotateDegreesPerPixel, Space.World);
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
            if (cachedRigidbody != null)
            {
                cachedRigidbody.velocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
                cachedRigidbody.isKinematic = true;
            }
        }

        void ConfigureRigidbodyForDesktopDrag()
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
            MagneticFieldVisualizer visualizer = FindObjectOfType<MagneticFieldVisualizer>();
            if (visualizer != null)
            {
                return visualizer.transform.position.z;
            }

            return fallbackDragPlaneZ;
        }

        public static MagneticCurrentSource FindBestCurrentTarget()
        {
            if (CurrentTarget != null)
            {
                return CurrentTarget;
            }

            Camera camera = Camera.main;
            MagneticCurrentSource[] sources = FindObjectsOfType<MagneticCurrentSource>();
            MagneticCurrentSource best = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < sources.Length; i++)
            {
                MagneticCurrentSource candidate = sources[i];
                if (candidate == null)
                {
                    continue;
                }

                float distance;
                if (camera != null)
                {
                    Vector3 viewport = camera.WorldToViewportPoint(candidate.transform.position);
                    if (viewport.z < 0f)
                    {
                        continue;
                    }

                    distance = Vector2.Distance(new Vector2(viewport.x, viewport.y), new Vector2(0.5f, 0.5f));
                }
                else
                {
                    distance = Mathf.Abs(candidate.transform.position.x) + Mathf.Abs(candidate.transform.position.y - 3.2f);
                }

                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            CurrentTarget = best;
            return best;
        }

        void EnsureStableDragCollider()
        {
            if (source == null)
            {
                source = GetComponent<MagneticCurrentSource>();
            }

            if (!IsSolenoidCandidate())
            {
                return;
            }

            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null)
            {
                box = gameObject.AddComponent<BoxCollider>();
            }

            box.isTrigger = false;
            box.center = Vector3.zero;
            box.size = new Vector3(1.45f, 2.65f, 1.1f);

            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.radius = Mathf.Max(capsule.radius, 0.72f);
                capsule.height = Mathf.Max(capsule.height, 2.65f);
            }
        }

        bool IsSolenoidCandidate()
        {
            if (source == null)
            {
                source = GetComponent<MagneticCurrentSource>();
            }

            if (source != null)
            {
                return source.sourceType == MagneticSourceType.Solenoid;
            }

            if (name.Contains("Solenoid"))
            {
                return true;
            }

            return transform.Find("Solenoid Helix") != null;
        }
    }
}
