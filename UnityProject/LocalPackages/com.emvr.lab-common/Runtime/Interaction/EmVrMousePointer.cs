using UnityEngine;

namespace EMVR.Common.Interaction
{
    /// <summary>
    /// Desktop pointer that mirrors a VR ray and operates any component that
    /// implements IEmVrMouseDraggable.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrMousePointer : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Optional MonoBehaviour implementing IEmVrPointerInput. Legacy mouse input is used when empty.")]
        public MonoBehaviour inputProvider;

        [Header("Raycast")]
        [Min(0.1f)]
        public float maxDistance = 18f;
        public LayerMask interactionLayers = ~0;
        public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
        public Transform[] ignoredRoots;
        [Min(0f)] public float pointerRadius = 0.08f;

        [Header("Visual")]
        public bool showPointerRay = true;
        public Color idleColor = new Color(1f, 0.92f, 0.25f, 0.55f);
        public Color activeColor = new Color(1f, 0.92f, 0.25f, 1f);
        [Min(0f)] public float idleWidth = 0.018f;
        [Min(0f)] public float activeWidth = 0.035f;
        [Min(0f)] public float rayStartOffset = 0.35f;

        [Header("Fallback plane")]
        public bool useFallbackPlane = true;
        public Vector3 fallbackPlaneNormal = Vector3.forward;
        public Transform fallbackPlaneAnchor;

        LineRenderer lineRenderer;
        Material ownedLineMaterial;
        IEmVrMouseDraggable heldObject;
        IEmVrPointerInput pointerInput;
        Camera pointerCamera;

        void Awake()
        {
            ResolveInputProvider();
            EnsureLineRenderer();
        }

        void OnValidate()
        {
            maxDistance = Mathf.Max(0.1f, maxDistance);
            pointerRadius = Mathf.Max(0f, pointerRadius);
            idleWidth = Mathf.Max(0f, idleWidth);
            activeWidth = Mathf.Max(0f, activeWidth);
        }

        void OnDisable()
        {
            ReleaseHeldObject();
            SetVisible(false);
        }

        void OnDestroy()
        {
            ReleaseHeldObject();
            DestroyOwnedMaterial(ownedLineMaterial);
            ownedLineMaterial = null;
        }

        void LateUpdate()
        {
            pointerCamera = Camera.main != null ? Camera.main : GetComponent<Camera>();
            if (pointerCamera == null)
            {
                SetVisible(false);
                return;
            }

            Vector3 pointerPosition = PointerPosition();
            Ray ray = pointerCamera.ScreenPointToRay(pointerPosition);
            Vector3 endPoint = ResolveEndPoint(ray);
            bool active = PrimaryHeld();

            if (showPointerRay)
            {
                EnsureLineRenderer();
                if (lineRenderer != null)
                {
                    lineRenderer.startColor = active ? activeColor : idleColor;
                    lineRenderer.endColor = active ? activeColor : idleColor;
                    lineRenderer.startWidth = active ? activeWidth : idleWidth;
                    lineRenderer.endWidth = active ? activeWidth : idleWidth;
                    lineRenderer.SetPosition(0, ray.origin + ray.direction * rayStartOffset);
                    lineRenderer.SetPosition(1, endPoint);
                    SetVisible(true);
                }
            }
            else
            {
                SetVisible(false);
            }

            UpdateGrab(ray, pointerPosition);
        }

        public void ResolveInputProvider()
        {
            pointerInput = inputProvider as IEmVrPointerInput;
            if (inputProvider != null && pointerInput == null)
            {
                Debug.LogError("EmVrMousePointer inputProvider must implement IEmVrPointerInput.", inputProvider);
            }
        }

        Vector3 ResolveEndPoint(Ray ray)
        {
            RaycastHit[] hits = PointerHits(ray);

            float closestDistance = float.PositiveInfinity;
            Vector3 endPoint = ray.origin + ray.direction * maxDistance;
            for (int i = 0; i < hits.Length; i++)
            {
                if (IsIgnored(hits[i].transform) || hits[i].distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = hits[i].distance;
                endPoint = hits[i].point;
            }

            if (closestDistance < float.PositiveInfinity || !useFallbackPlane)
            {
                return endPoint;
            }

            Vector3 normal = fallbackPlaneNormal.sqrMagnitude > 0.0001f
                ? fallbackPlaneNormal.normalized
                : Vector3.forward;
            Vector3 anchor = fallbackPlaneAnchor != null ? fallbackPlaneAnchor.position : Vector3.zero;
            Plane plane = new Plane(normal, anchor);
            float planeDistance;
            if (plane.Raycast(ray, out planeDistance) && planeDistance > 0f && planeDistance < maxDistance)
            {
                return ray.GetPoint(planeDistance);
            }

            return endPoint;
        }

        void UpdateGrab(Ray ray, Vector3 pointerPosition)
        {
            if (PrimaryPressedThisFrame())
            {
                heldObject = FindDraggable(ray);
                if (heldObject != null)
                {
                    heldObject.BeginMouseDrag(pointerCamera, pointerPosition);
                }
            }

            if (PrimaryHeld() && heldObject != null)
            {
                heldObject.ContinueMouseDrag(pointerPosition);
            }

            if (PrimaryReleasedThisFrame())
            {
                ReleaseHeldObject();
            }
        }

        IEmVrMouseDraggable FindDraggable(Ray ray)
        {
            RaycastHit[] hits = PointerHits(ray);

            IEmVrMouseDraggable closest = null;
            float closestDistance = float.PositiveInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider == null || IsIgnored(hitCollider.transform))
                {
                    continue;
                }

                IEmVrMouseDraggable draggable = FindDraggableOn(hitCollider.transform);
                if (draggable != null && hits[i].distance < closestDistance)
                {
                    closest = draggable;
                    closestDistance = hits[i].distance;
                }
            }

            return closest;
        }

        RaycastHit[] PointerHits(Ray ray)
        {
            float radius = Mathf.Max(0f, pointerRadius);
            if (radius <= 0.0001f)
            {
                return Physics.RaycastAll(
                    ray,
                    maxDistance,
                    interactionLayers.value,
                    triggerInteraction);
            }

            return Physics.SphereCastAll(
                ray,
                radius,
                maxDistance,
                interactionLayers.value,
                triggerInteraction);
        }

        static IEmVrMouseDraggable FindDraggableOn(Transform hitTransform)
        {
            Transform current = hitTransform;
            while (current != null)
            {
                MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    IEmVrMouseDraggable draggable = behaviours[i] as IEmVrMouseDraggable;
                    if (draggable != null)
                    {
                        return draggable;
                    }
                }

                current = current.parent;
            }

            return null;
        }

        bool IsIgnored(Transform candidate)
        {
            if (candidate == null || ignoredRoots == null)
            {
                return false;
            }

            for (int i = 0; i < ignoredRoots.Length; i++)
            {
                Transform ignored = ignoredRoots[i];
                if (ignored != null && (candidate == ignored || candidate.IsChildOf(ignored)))
                {
                    return true;
                }
            }

            return false;
        }

        void EnsureLineRenderer()
        {
            if (lineRenderer != null)
            {
                return;
            }

            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.numCapVertices = 4;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            if (lineRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    Debug.LogError("EmVrMousePointer could not find the Sprites/Default shader.", this);
                    return;
                }

                ownedLineMaterial = new Material(shader);
                ownedLineMaterial.name = "EMVR Mouse Pointer Material";
                lineRenderer.sharedMaterial = ownedLineMaterial;
            }
        }

        void ReleaseHeldObject()
        {
            if (heldObject != null)
            {
                heldObject.EndMouseDrag();
                heldObject = null;
            }
        }

        Vector3 PointerPosition()
        {
            return pointerInput != null
                ? pointerInput.PointerScreenPosition
                : EmVrDesktopInput.PointerScreenPosition;
        }

        bool PrimaryPressedThisFrame()
        {
            return pointerInput != null
                ? pointerInput.PrimaryPressedThisFrame
                : EmVrDesktopInput.PrimaryPressedThisFrame;
        }

        bool PrimaryHeld()
        {
            return pointerInput != null
                ? pointerInput.PrimaryHeld
                : EmVrDesktopInput.PrimaryHeld;
        }

        bool PrimaryReleasedThisFrame()
        {
            return pointerInput != null
                ? pointerInput.PrimaryReleasedThisFrame
                : EmVrDesktopInput.PrimaryReleasedThisFrame;
        }

        void SetVisible(bool visible)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = visible;
            }
        }

        static void DestroyOwnedMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(material);
            }
            else
            {
                Object.DestroyImmediate(material);
            }
        }
    }
}
