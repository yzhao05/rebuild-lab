using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class ElectrostaticPresetPad : MonoBehaviour
    {
        public ElectrostaticLabController controller;
        public int presetId = 1;

        XRBaseInteractable interactable;
        float nextAllowedActivationTime;

        void OnEnable()
        {
            interactable = GetComponent<XRBaseInteractable>();
            if (interactable != null)
            {
                interactable.activated.AddListener(OnActivated);
                interactable.selectEntered.AddListener(OnSelected);
            }
        }

        void OnDisable()
        {
            if (interactable != null)
            {
                interactable.activated.RemoveListener(OnActivated);
                interactable.selectEntered.RemoveListener(OnSelected);
            }
        }

        void OnMouseDown()
        {
            Activate();
        }

        void OnActivated(ActivateEventArgs args)
        {
            Activate();
        }

        void OnSelected(SelectEnterEventArgs args)
        {
            Activate();
        }

        public void Activate()
        {
            if (Time.unscaledTime < nextAllowedActivationTime)
            {
                return;
            }

            nextAllowedActivationTime = Time.unscaledTime + 0.25f;

            if (controller != null)
            {
                controller.ApplyPreset(presetId);
            }
        }
    }
}
