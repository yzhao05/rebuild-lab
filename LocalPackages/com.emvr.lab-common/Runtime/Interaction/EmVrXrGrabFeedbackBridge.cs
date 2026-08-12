using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace EMVR.Common.Interaction
{
    /// <summary>
    /// Adapts an XRI grab lifecycle to the same feedback and telemetry path used
    /// by desktop dragging.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class EmVrXrGrabFeedbackBridge : MonoBehaviour
    {
        public EmVrDragFeedbackBridge feedbackBridge;

        [Min(0f)]
        public float movementEpsilon = 0.0001f;

        XRGrabInteractable grabInteractable;
        Vector3 lastPosition;
        bool xrDragActive;

        void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (feedbackBridge == null)
            {
                feedbackBridge = GetComponent<EmVrDragFeedbackBridge>();
            }
        }

        void OnEnable()
        {
            if (grabInteractable == null)
            {
                grabInteractable = GetComponent<XRGrabInteractable>();
            }

            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            grabInteractable.selectExited.AddListener(OnSelectExited);
        }

        void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
                grabInteractable.selectExited.RemoveListener(OnSelectExited);
            }

            EndXrDrag();
        }

        void Update()
        {
            if (!xrDragActive || feedbackBridge == null)
            {
                return;
            }

            float epsilon = Mathf.Max(0f, movementEpsilon);
            if ((transform.position - lastPosition).sqrMagnitude <= epsilon * epsilon)
            {
                return;
            }

            lastPosition = transform.position;
            feedbackBridge.OnEmVrDragMoved(transform, lastPosition);
        }

        void OnSelectEntered(SelectEnterEventArgs eventArgs)
        {
            if (xrDragActive || feedbackBridge == null)
            {
                return;
            }

            xrDragActive = true;
            lastPosition = transform.position;
            feedbackBridge.OnEmVrDragStarted(transform);
        }

        void OnSelectExited(SelectExitEventArgs eventArgs)
        {
            EndXrDrag();
        }

        void EndXrDrag()
        {
            if (!xrDragActive)
            {
                return;
            }

            xrDragActive = false;
            if (feedbackBridge != null)
            {
                feedbackBridge.OnEmVrDragEnded(transform);
            }
        }
    }
}
