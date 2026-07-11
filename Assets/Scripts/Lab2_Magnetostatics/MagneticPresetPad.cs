using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class MagneticPresetPad : MonoBehaviour
    {
        public MagneticLabController controller;
        public int presetId = 1;
        public float debounceSeconds = 0.25f;

        XRBaseInteractable interactable;
        float nextActivationTime;

        void OnEnable()
        {
            interactable = GetComponent<XRBaseInteractable>();
            if (interactable != null)
            {
                interactable.activated.AddListener(OnXrActivated);
                interactable.selectEntered.AddListener(OnXrSelected);
            }
        }

        void OnDisable()
        {
            if (interactable != null)
            {
                interactable.activated.RemoveListener(OnXrActivated);
                interactable.selectEntered.RemoveListener(OnXrSelected);
            }
        }

        void OnMouseDown()
        {
            Activate();
        }

        void OnXrActivated(ActivateEventArgs args)
        {
            Activate();
        }

        void OnXrSelected(SelectEnterEventArgs args)
        {
            Activate();
        }

        public void Activate()
        {
            if (Time.unscaledTime < nextActivationTime)
            {
                return;
            }

            nextActivationTime = Time.unscaledTime + debounceSeconds;
            if (controller != null)
            {
                controller.ApplyPreset(presetId);
            }
        }
    }
}
