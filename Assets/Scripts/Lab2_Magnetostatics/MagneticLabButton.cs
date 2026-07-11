using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ElectrostaticsLab
{
    [DisallowMultipleComponent]
    public sealed class MagneticLabButton : MonoBehaviour
    {
        public enum LabButtonAction
        {
            ClearSources,
            ToggleFieldLines,
            ToggleVectors,
            FlipCurrents,
            IncreaseCurrent,
            DecreaseCurrent,
            ApplyPreset
        }

        public MagneticLabController controller;
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
            if (action == LabButtonAction.ClearSources)
            {
                controller.ClearPresetSources();
            }
            else if (action == LabButtonAction.ToggleFieldLines)
            {
                controller.ToggleFieldLines();
            }
            else if (action == LabButtonAction.ToggleVectors)
            {
                controller.ToggleVectors();
            }
            else if (action == LabButtonAction.FlipCurrents)
            {
                controller.FlipAllSourcesInStage();
            }
            else if (action == LabButtonAction.IncreaseCurrent)
            {
                controller.AddCurrentToSourcesInStage(1);
            }
            else if (action == LabButtonAction.DecreaseCurrent)
            {
                controller.AddCurrentToSourcesInStage(-1);
            }
            else
            {
                controller.ApplyPreset(presetId);
            }
        }
    }
}
