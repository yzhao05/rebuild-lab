using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class ElectrostaticHoverAnnotation : MonoBehaviour
    {
        public TextMesh annotation;
        public Vector3 worldOffset = new Vector3(0f, 0.75f, -0.35f);
        public Camera facingCamera;

        XRBaseInteractable interactable;
        int xrHoverCount;
        int xrSelectCount;
        bool hovering;

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
            if (interactable == null)
            {
                return;
            }

            interactable.hoverEntered.RemoveListener(OnXrHoverEntered);
            interactable.hoverExited.RemoveListener(OnXrHoverExited);
            interactable.selectEntered.RemoveListener(OnXrSelectEntered);
            interactable.selectExited.RemoveListener(OnXrSelectExited);
        }

        void Awake()
        {
            SetVisible(false);
        }

        void OnMouseEnter()
        {
            hovering = true;
            RefreshVisibility();
        }

        void OnMouseExit()
        {
            hovering = false;
            RefreshVisibility();
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

        void LateUpdate()
        {
            if (annotation == null)
            {
                return;
            }

            annotation.transform.position = transform.position + worldOffset;
            Camera cameraToFace = facingCamera != null ? facingCamera : Camera.main;
            if (cameraToFace != null)
            {
                Vector3 awayFromCamera = annotation.transform.position - cameraToFace.transform.position;
                if (awayFromCamera.sqrMagnitude > 0.001f)
                {
                    annotation.transform.rotation = Quaternion.LookRotation(awayFromCamera.normalized, Vector3.up);
                }
            }

            RefreshVisibility();
        }

        void RefreshVisibility()
        {
            SetVisible(hovering || xrHoverCount > 0 || xrSelectCount > 0);
        }

        void SetVisible(bool visible)
        {
            if (annotation != null)
            {
                annotation.gameObject.SetActive(visible);
            }
        }
    }
}
