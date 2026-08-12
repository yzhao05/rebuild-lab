using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace EMVR.Common.UI
{
    /// <summary>
    /// Shows a world-space annotation for desktop mouse hover and XRI
    /// hover/select events, then keeps the annotation facing a runtime camera.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrHoverAnnotation : MonoBehaviour
    {
        public enum FacingDirection
        {
            ForwardTowardCamera,
            ForwardAwayFromCamera
        }

        [Tooltip("Root object that is shown and hidden. annotationText.gameObject is used when empty.")]
        public GameObject annotationRoot;
        public TextMesh annotationText;
        public Vector3 worldOffset = new Vector3(0f, 0.75f, -0.35f);
        public Camera facingCamera;
        public FacingDirection facingDirection = FacingDirection.ForwardAwayFromCamera;

        XRBaseInteractable interactable;
        int xrHoverCount;
        int xrSelectCount;
        bool mouseHovering;

        void Awake()
        {
            ResolveAnnotationRoot();
            SetVisible(false);
        }

        void OnEnable()
        {
            interactable = GetComponent<XRBaseInteractable>();
            if (interactable == null)
            {
                return;
            }

            interactable.hoverEntered.AddListener(OnXrHoverEntered);
            interactable.hoverExited.AddListener(OnXrHoverExited);
            interactable.selectEntered.AddListener(OnXrSelectEntered);
            interactable.selectExited.AddListener(OnXrSelectExited);
        }

        void OnDisable()
        {
            if (interactable != null)
            {
                interactable.hoverEntered.RemoveListener(OnXrHoverEntered);
                interactable.hoverExited.RemoveListener(OnXrHoverExited);
                interactable.selectEntered.RemoveListener(OnXrSelectEntered);
                interactable.selectExited.RemoveListener(OnXrSelectExited);
            }

            mouseHovering = false;
            xrHoverCount = 0;
            xrSelectCount = 0;
            SetVisible(false);
        }

        void OnMouseEnter()
        {
            mouseHovering = true;
            RefreshVisibility();
        }

        void OnMouseExit()
        {
            mouseHovering = false;
            RefreshVisibility();
        }

        void LateUpdate()
        {
            ResolveAnnotationRoot();
            if (annotationRoot == null)
            {
                return;
            }

            Transform annotationTransform = annotationRoot.transform;
            annotationTransform.position = transform.position + worldOffset;
            Camera cameraToFace = facingCamera != null ? facingCamera : Camera.main;
            if (cameraToFace != null)
            {
                Vector3 direction = facingDirection == FacingDirection.ForwardTowardCamera
                    ? cameraToFace.transform.position - annotationTransform.position
                    : annotationTransform.position - cameraToFace.transform.position;
                if (direction.sqrMagnitude > 0.001f)
                {
                    annotationTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }

            RefreshVisibility();
        }

        public void SetAnnotationText(string value)
        {
            if (annotationText != null)
            {
                annotationText.text = value ?? string.Empty;
            }
        }

        void OnXrHoverEntered(HoverEnterEventArgs args)
        {
            xrHoverCount++;
            RefreshVisibility();
        }

        void OnXrHoverExited(HoverExitEventArgs args)
        {
            xrHoverCount = Mathf.Max(0, xrHoverCount - 1);
            RefreshVisibility();
        }

        void OnXrSelectEntered(SelectEnterEventArgs args)
        {
            xrSelectCount++;
            RefreshVisibility();
        }

        void OnXrSelectExited(SelectExitEventArgs args)
        {
            xrSelectCount = Mathf.Max(0, xrSelectCount - 1);
            RefreshVisibility();
        }

        void ResolveAnnotationRoot()
        {
            if (annotationRoot == null && annotationText != null)
            {
                annotationRoot = annotationText.gameObject;
            }
        }

        void RefreshVisibility()
        {
            SetVisible(mouseHovering || xrHoverCount > 0 || xrSelectCount > 0);
        }

        void SetVisible(bool visible)
        {
            if (annotationRoot == null)
            {
                return;
            }

            // Never deactivate the GameObject that owns this component; doing
            // so would prevent it from receiving the next hover event.
            if (annotationRoot == gameObject)
            {
                Renderer[] renderers = annotationRoot.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].enabled = visible;
                }

                return;
            }

            if (annotationRoot.activeSelf != visible)
            {
                annotationRoot.SetActive(visible);
            }
        }
    }
}
