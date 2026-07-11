using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class ElectrostaticLabButton : MonoBehaviour
    {
        public enum LabButtonAction
        {
            ApplyPreset,
            ClearCharges,
            ToggleFieldLines,
            ToggleVectors
        }

        public ElectrostaticLabController controller;
        public LabButtonAction action = LabButtonAction.ApplyPreset;
        public int presetId = 1;

        XRBaseInteractable interactable;
        float nextAllowedActivationTime;

        void OnEnable()
        {
            interactable = GetComponent<XRBaseInteractable>();
            if (interactable == null)
            {
                return;
            }

            interactable.selectEntered.AddListener(OnSelected);
            interactable.activated.AddListener(OnActivated);
        }

        void OnDisable()
        {
            if (interactable == null)
            {
                return;
            }

            interactable.selectEntered.RemoveListener(OnSelected);
            interactable.activated.RemoveListener(OnActivated);
        }

        void OnMouseDown()
        {
            Activate();
        }

        void OnSelected(SelectEnterEventArgs args)
        {
            Activate();
        }

        void OnActivated(ActivateEventArgs args)
        {
            Activate();
        }

        public void Activate()
        {
            if (controller == null || Time.unscaledTime < nextAllowedActivationTime)
            {
                return;
            }

            nextAllowedActivationTime = Time.unscaledTime + 0.25f;

            if (action == LabButtonAction.ApplyPreset)
            {
                controller.ApplyPreset(presetId);
            }
            else if (action == LabButtonAction.ClearCharges)
            {
                controller.ClearPresetCharges();
            }
            else if (action == LabButtonAction.ToggleFieldLines)
            {
                controller.ToggleFieldLines();
            }
            else if (action == LabButtonAction.ToggleVectors)
            {
                controller.ToggleVectors();
            }
        }
    }
}
