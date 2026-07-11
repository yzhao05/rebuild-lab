using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class PolarizationLabButton : MonoBehaviour
    {
        public enum LabButtonAction
        {
            ClearElements,
            ToggleWaveTrace,
            ToggleOrbit,
            ApplyPreset
        }

        public PolarizationLabController controller;
        public LabButtonAction action;
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
            if (controller == null || Time.unscaledTime < nextActivationTime)
            {
                return;
            }

            nextActivationTime = Time.unscaledTime + debounceSeconds;
            if (action == LabButtonAction.ClearElements)
            {
                controller.ClearPresetElements();
            }
            else if (action == LabButtonAction.ToggleWaveTrace)
            {
                controller.ToggleWaveTrace();
            }
            else if (action == LabButtonAction.ToggleOrbit)
            {
                controller.ToggleOrbit();
            }
            else
            {
                controller.ApplyPreset(presetId);
            }
        }
    }
}
