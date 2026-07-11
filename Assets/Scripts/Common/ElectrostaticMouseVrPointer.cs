using UnityEngine;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class ElectrostaticMouseVrPointer : MonoBehaviour
    {
        public float maxDistance = 18f;
        public Color idleColor = new Color(1f, 0.92f, 0.25f, 0.55f);
        public Color activeColor = new Color(1f, 0.92f, 0.25f, 1f);
        public float idleWidth = 0.018f;
        public float activeWidth = 0.035f;

        LineRenderer lineRenderer;
        Material lineMaterial;
        IEmVrMouseDraggable heldObject;
        Camera pointerCamera;

        void Awake()
        {
            EnsureLineRenderer();
        }

        void LateUpdate()
        {
            pointerCamera = Camera.main != null ? Camera.main : GetComponent<Camera>();
            if (pointerCamera == null)
            {
                SetVisible(false);
                return;
            }

            EnsureLineRenderer();
            Ray ray = pointerCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 endPoint = ray.origin + ray.direction * maxDistance;

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxDistance))
            {
                endPoint = hit.point;
            }
            else
            {
                Plane labPlane = new Plane(Vector3.forward, Vector3.zero);
                float distance;
                if (labPlane.Raycast(ray, out distance) && distance > 0f && distance < maxDistance)
                {
                    endPoint = ray.GetPoint(distance);
                }
            }

            bool active = Input.GetMouseButton(0);
            lineRenderer.startColor = active ? activeColor : idleColor;
            lineRenderer.endColor = active ? activeColor : idleColor;
            lineRenderer.startWidth = active ? activeWidth : idleWidth;
            lineRenderer.endWidth = active ? activeWidth : idleWidth;
            lineRenderer.SetPosition(0, ray.origin + ray.direction * 0.35f);
            lineRenderer.SetPosition(1, endPoint);
            SetVisible(true);

            UpdateMouseGrab(ray);
        }

        void UpdateMouseGrab(Ray ray)
        {
            if (Input.GetMouseButtonDown(0))
            {
                heldObject = FindDraggable(ray);
                if (heldObject != null)
                {
                    heldObject.BeginMouseDrag(pointerCamera, Input.mousePosition);
                }
            }

            if (Input.GetMouseButton(0) && heldObject != null)
            {
                heldObject.ContinueMouseDrag(Input.mousePosition);
            }

            if (Input.GetMouseButtonUp(0) && heldObject != null)
            {
                heldObject.EndMouseDrag();
                heldObject = null;
            }
        }

        IEmVrMouseDraggable FindDraggable(Ray ray)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
            IEmVrMouseDraggable closest = null;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider == null)
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

        IEmVrMouseDraggable FindDraggableOn(Transform hitTransform)
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

        void EnsureLineRenderer()
        {
            if (lineRenderer != null)
            {
                return;
            }

            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.name = "Mouse VR Pointer Ray";
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.numCapVertices = 4;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            Shader shader = Shader.Find("Sprites/Default");
            lineMaterial = new Material(shader);
            lineRenderer.sharedMaterial = lineMaterial;
        }

        void SetVisible(bool visible)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = visible;
            }
        }
    }
}
