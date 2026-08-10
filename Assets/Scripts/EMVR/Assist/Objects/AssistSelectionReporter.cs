using EMVR.Assist.Logging;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace EMVR.Assist.Objects
{
    public class AssistSelectionReporter : MonoBehaviour
    {
        private ExperimentObject _experimentObject;
        private XRGrabInteractable _grabInteractable;

        private void Awake()
        {
            _experimentObject = GetComponent<ExperimentObject>();
            _grabInteractable = GetComponent<XRGrabInteractable>();
        }

        private void OnEnable()
        {
            if (_grabInteractable == null) _grabInteractable = GetComponent<XRGrabInteractable>();
            if (_grabInteractable == null) return;

            _grabInteractable.selectEntered.AddListener(OnSelectEntered);
            _grabInteractable.selectExited.AddListener(OnSelectExited);
            _grabInteractable.hoverEntered.AddListener(OnHoverEntered);
        }

        private void OnDisable()
        {
            if (_grabInteractable == null) return;

            _grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            _grabInteractable.selectExited.RemoveListener(OnSelectExited);
            _grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
        }

        private void OnMouseEnter()
        {
            PointAtObject();
        }

        private void OnMouseDown()
        {
            Select("select_object");
        }

        private void OnMouseUp()
        {
            Release();
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            Select("grab_object");
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            Release();
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            PointAtObject();
        }

        private void Select(string actionType)
        {
            if (_experimentObject == null) _experimentObject = GetComponent<ExperimentObject>();
            if (_experimentObject == null) return;

            if (SelectionTracker.Instance != null)
            {
                SelectionTracker.Instance.SetSelected(_experimentObject);
            }

            if (InteractionEventLogger.Instance != null)
            {
                InteractionEventLogger.Instance.LogAction(actionType, _experimentObject);
            }
        }

        private void PointAtObject()
        {
            if (_experimentObject == null) _experimentObject = GetComponent<ExperimentObject>();
            if (_experimentObject == null) return;

            if (SelectionTracker.Instance != null)
            {
                SelectionTracker.Instance.MarkInteracted(_experimentObject);
            }

            if (InteractionEventLogger.Instance != null)
            {
                InteractionEventLogger.Instance.LogAction("point_object", _experimentObject);
            }
        }

        private void Release()
        {
            if (_experimentObject == null) _experimentObject = GetComponent<ExperimentObject>();

            if (InteractionEventLogger.Instance != null)
            {
                InteractionEventLogger.Instance.LogAction("release_object", _experimentObject);
            }

            if (SelectionTracker.Instance != null
                && _experimentObject != null
                && SelectionTracker.Instance.CurrentSelected == _experimentObject)
            {
                SelectionTracker.Instance.ClearSelection();
            }
        }
    }
}
