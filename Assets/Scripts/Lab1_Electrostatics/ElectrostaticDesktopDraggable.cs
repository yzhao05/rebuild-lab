using UnityEngine;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class ElectrostaticDesktopDraggable : MonoBehaviour, IEmVrMouseDraggable
    {
        public float dragPlaneZ;

        Camera dragCamera;
        Rigidbody cachedRigidbody;
        ElectrostaticCharge charge;
        Vector3 dragOffset;
        bool previousKinematic;
        bool dragging;
        float nextDragChargeTime;

        void Awake()
        {
            dragPlaneZ = transform.position.z;
            cachedRigidbody = GetComponent<Rigidbody>();
            charge = GetComponent<ElectrostaticCharge>();
        }

        void OnMouseDown()
        {
            BeginMouseDrag(Camera.main, Input.mousePosition);
        }

        public void BeginMouseDrag(Camera cameraToUse, Vector3 screenPosition)
        {
            dragCamera = cameraToUse;
            if (dragCamera == null)
            {
                return;
            }

            Vector3 hitPoint;
            if (TryScreenToPlane(screenPosition, out hitPoint))
            {
                dragging = true;
                dragOffset = transform.position - hitPoint;
                dragPlaneZ = transform.position.z;

                if (cachedRigidbody != null)
                {
                    previousKinematic = cachedRigidbody.isKinematic;
                    if (!cachedRigidbody.isKinematic)
                    {
                        cachedRigidbody.velocity = Vector3.zero;
                        cachedRigidbody.angularVelocity = Vector3.zero;
                    }

                    cachedRigidbody.isKinematic = true;
                }
            }
        }

        void OnMouseDrag()
        {
            ContinueMouseDrag(Input.mousePosition);
        }

        public void ContinueMouseDrag(Vector3 screenPosition)
        {
            if (!dragging || dragCamera == null)
            {
                return;
            }

            Vector3 hitPoint;
            if (TryScreenToPlane(screenPosition, out hitPoint))
            {
                Vector3 targetPosition = hitPoint + dragOffset;
                if (cachedRigidbody != null)
                {
                    cachedRigidbody.MovePosition(targetPosition);
                }

                transform.position = targetPosition;
                TryChargeNearMouseDrag();
            }
        }

        void OnMouseUp()
        {
            EndMouseDrag();
        }

        public void EndMouseDrag()
        {
            dragging = false;
            if (cachedRigidbody != null)
            {
                cachedRigidbody.isKinematic = previousKinematic;
                if (!cachedRigidbody.isKinematic)
                {
                    cachedRigidbody.velocity = Vector3.zero;
                    cachedRigidbody.angularVelocity = Vector3.zero;
                }
            }
        }

        bool TryScreenToPlane(Vector3 screenPosition, out Vector3 hitPoint)
        {
            Ray ray = dragCamera.ScreenPointToRay(screenPosition);
            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, dragPlaneZ));
            float distance;
            if (plane.Raycast(ray, out distance))
            {
                hitPoint = ray.GetPoint(distance);
                return true;
            }

            hitPoint = Vector3.zero;
            return false;
        }

        void TryChargeNearMouseDrag()
        {
            if (charge == null || Time.unscaledTime < nextDragChargeTime)
            {
                return;
            }

            ElectrostaticCharger[] chargers = FindObjectsOfType<ElectrostaticCharger>();
            for (int i = 0; i < chargers.Length; i++)
            {
                ElectrostaticCharger charger = chargers[i];
                if (charger == null)
                {
                    continue;
                }

                if (Vector3.Distance(transform.position, charger.transform.position) <= charger.effectiveRadius)
                {
                    charger.ChargeImmediately(charge);
                    nextDragChargeTime = Time.unscaledTime + 0.25f;
                    return;
                }
            }
        }
    }
}
