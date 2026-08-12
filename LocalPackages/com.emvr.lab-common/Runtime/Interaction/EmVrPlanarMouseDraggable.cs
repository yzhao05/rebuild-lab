using System.Collections.Generic;
using UnityEngine;

namespace EMVR.Common.Interaction
{
    /// <summary>
    /// Projects a pointer ray onto a configurable world-space plane and moves
    /// the target while keeping lab-specific side effects in drag observers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrPlanarMouseDraggable : MonoBehaviour, IEmVrMouseDraggable
    {
        [Header("Drag plane")]
        [Tooltip("World-space normal of the plane used during a drag.")]
        public Vector3 planeNormal = Vector3.forward;

        [Tooltip("Optional point on the drag plane. The object's position is used when empty.")]
        public Transform planeAnchor;

        [Header("Input")]
        [Tooltip("Allow Unity OnMouseDown/OnMouseDrag messages in addition to an EmVrMousePointer.")]
        public bool handleUnityMouseMessages = true;

        [Header("Physics")]
        [Tooltip("Temporarily make a Rigidbody kinematic while it is being dragged.")]
        public bool controlRigidbody = true;

        readonly List<IEmVrDragObserver> observers = new List<IEmVrDragObserver>();

        Camera dragCamera;
        Rigidbody cachedRigidbody;
        Plane activePlane;
        Vector3 dragOffset;
        bool previousKinematic;
        bool dragging;
        bool unityMouseOwnsDrag;

        public bool IsDragging
        {
            get { return dragging; }
        }

        void Awake()
        {
            cachedRigidbody = GetComponent<Rigidbody>();
            RefreshObservers();
        }

        void OnDisable()
        {
            EndMouseDrag();
        }

        void Update()
        {
            if (!handleUnityMouseMessages || !unityMouseOwnsDrag || !dragging)
            {
                return;
            }

            if (EmVrDesktopInput.PrimaryHeld)
            {
                ContinueMouseDrag(EmVrDesktopInput.PointerScreenPosition);
            }
            else
            {
                EndMouseDrag();
            }
        }

        void OnMouseDown()
        {
            if (handleUnityMouseMessages)
            {
                bool wasDragging = dragging;
                BeginMouseDrag(Camera.main, EmVrDesktopInput.PointerScreenPosition);
                unityMouseOwnsDrag = !wasDragging && dragging;
            }
        }

        void OnMouseDrag()
        {
            if (handleUnityMouseMessages)
            {
                ContinueMouseDrag(EmVrDesktopInput.PointerScreenPosition);
            }
        }

        void OnMouseUp()
        {
            if (handleUnityMouseMessages)
            {
                EndMouseDrag();
            }
        }

        public void BeginMouseDrag(Camera cameraToUse, Vector3 screenPosition)
        {
            if (dragging || cameraToUse == null)
            {
                return;
            }

            Vector3 normal = planeNormal.sqrMagnitude > 0.0001f
                ? planeNormal.normalized
                : Vector3.forward;
            Vector3 planePoint = planeAnchor != null ? planeAnchor.position : transform.position;

            dragCamera = cameraToUse;
            activePlane = new Plane(normal, planePoint);

            Vector3 hitPoint;
            if (!TryScreenToPlane(screenPosition, out hitPoint))
            {
                dragCamera = null;
                return;
            }

            dragging = true;
            dragOffset = transform.position - hitPoint;

            if (controlRigidbody && cachedRigidbody != null)
            {
                previousKinematic = cachedRigidbody.isKinematic;
                if (!cachedRigidbody.isKinematic)
                {
                    cachedRigidbody.velocity = Vector3.zero;
                    cachedRigidbody.angularVelocity = Vector3.zero;
                }

                cachedRigidbody.isKinematic = true;
            }

            RefreshObservers();
            NotifyDragStarted();
        }

        public void ContinueMouseDrag(Vector3 screenPosition)
        {
            if (!dragging || dragCamera == null)
            {
                return;
            }

            Vector3 hitPoint;
            if (!TryScreenToPlane(screenPosition, out hitPoint))
            {
                return;
            }

            Vector3 targetPosition = hitPoint + dragOffset;
            transform.position = targetPosition;
            NotifyDragMoved(targetPosition);
        }

        public void EndMouseDrag()
        {
            if (!dragging)
            {
                return;
            }

            dragging = false;
            unityMouseOwnsDrag = false;

            if (controlRigidbody && cachedRigidbody != null)
            {
                cachedRigidbody.isKinematic = previousKinematic;
                if (!previousKinematic)
                {
                    cachedRigidbody.velocity = Vector3.zero;
                    cachedRigidbody.angularVelocity = Vector3.zero;
                }
            }

            NotifyDragEnded();
            dragCamera = null;
        }

        public void RefreshObservers()
        {
            observers.Clear();
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                IEmVrDragObserver observer = behaviours[i] as IEmVrDragObserver;
                if (observer != null)
                {
                    observers.Add(observer);
                }
            }
        }

        bool TryScreenToPlane(Vector3 screenPosition, out Vector3 hitPoint)
        {
            Ray ray = dragCamera.ScreenPointToRay(screenPosition);
            float distance;
            if (activePlane.Raycast(ray, out distance))
            {
                hitPoint = ray.GetPoint(distance);
                return true;
            }

            hitPoint = Vector3.zero;
            return false;
        }

        void NotifyDragStarted()
        {
            for (int i = 0; i < observers.Count; i++)
            {
                observers[i].OnEmVrDragStarted(transform);
            }
        }

        void NotifyDragMoved(Vector3 worldPosition)
        {
            for (int i = 0; i < observers.Count; i++)
            {
                observers[i].OnEmVrDragMoved(transform, worldPosition);
            }
        }

        void NotifyDragEnded()
        {
            for (int i = 0; i < observers.Count; i++)
            {
                observers[i].OnEmVrDragEnded(transform);
            }
        }
    }
}
