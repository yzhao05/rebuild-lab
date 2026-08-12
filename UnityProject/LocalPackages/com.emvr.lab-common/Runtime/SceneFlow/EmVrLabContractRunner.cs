using System;
using System.Collections.Generic;
using EMVR.Common.Assist;
using EMVR.Common.Objects;
using EMVR.Common.Telemetry;
using EMVR.Common.UI;
using UnityEngine;

namespace EMVR.Common.SceneFlow
{
    /// <summary>
    /// Runs the reusable lab/step contract lifecycle and keeps telemetry,
    /// visible status, object selection, and assist grounding synchronized.
    /// Domain-specific completion rules remain in the consuming lab and call
    /// this component only after their own physics conditions pass.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrLabContractRunner : MonoBehaviour
    {
        public const string LabStartedEvent = "lab_started";
        public const string StepStartedEvent = "step_started";
        public const string ObjectSelectedEvent = "object_selected";
        public const string ObjectDragStartedEvent = "object_drag_started";
        public const string ObjectDragEndedEvent = "object_drag_ended";
        public const string StepCompletedEvent = "step_completed";
        public const string LabCompletedEvent = "lab_completed";

        [Serializable]
        public sealed class StepDefinition
        {
            public string stepId;
            public string displayName;
            public string requiredObjectId;
            public bool requireSelection;
            public bool requireCompletedDrag;
        }

        [Header("Required contract IDs")]
        public string sessionId;
        public string userId;
        public string labId;
        public string mode = "validation";
        public StepDefinition[] steps = new StepDefinition[0];
        public EmVrLabObject[] knownObjects = new EmVrLabObject[0];

        [Header("Shared services")]
        public EmVrTelemetryEmitter telemetryEmitter;
        public EmVrAssistGroundingProvider assistGrounding;
        public EmVrStatusDisplay statusDisplay;
        public bool clearTelemetryOnStart = true;

        readonly Dictionary<string, EmVrLabObject> objectsById =
            new Dictionary<string, EmVrLabObject>(StringComparer.Ordinal);

        int currentStepIndex = -1;
        EmVrLabObject selectedObject;
        bool selectionCompletedThisStep;
        bool dragActive;
        bool dragCompletedThisStep;
        bool labActive;
        bool labCompleted;

        public int CurrentStepIndex
        {
            get { return currentStepIndex; }
        }

        public string CurrentStepId
        {
            get
            {
                return currentStepIndex >= 0 && steps != null
                    && currentStepIndex < steps.Length && steps[currentStepIndex] != null
                    ? steps[currentStepIndex].stepId
                    : string.Empty;
            }
        }

        public bool IsLabActive
        {
            get { return labActive; }
        }

        public bool IsLabCompleted
        {
            get { return labCompleted; }
        }

        public bool TryValidateContract(out string error)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                error = "sessionId is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                error = "userId is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(labId))
            {
                error = "labId is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(mode))
            {
                error = "mode is required.";
                return false;
            }

            if (telemetryEmitter == null || assistGrounding == null || statusDisplay == null)
            {
                error = "Telemetry, assist grounding, and status display references are required.";
                return false;
            }

            if (steps == null || steps.Length == 0)
            {
                error = "At least one step definition is required.";
                return false;
            }

            HashSet<string> stepIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < steps.Length; i++)
            {
                StepDefinition step = steps[i];
                if (step == null || string.IsNullOrWhiteSpace(step.stepId))
                {
                    error = "Step " + i + " has no stepId.";
                    return false;
                }

                step.stepId = step.stepId.Trim();
                if (!stepIds.Add(step.stepId))
                {
                    error = "Duplicate stepId '" + step.stepId + "'.";
                    return false;
                }

                if ((step.requireSelection || step.requireCompletedDrag)
                    && string.IsNullOrWhiteSpace(step.requiredObjectId))
                {
                    error = "Step '" + step.stepId
                        + "' requires interaction but has no requiredObjectId.";
                    return false;
                }

                step.requiredObjectId = string.IsNullOrWhiteSpace(step.requiredObjectId)
                    ? string.Empty
                    : step.requiredObjectId.Trim();
            }

            if (!TryBuildObjectMap(out error))
            {
                return false;
            }

            for (int i = 0; i < steps.Length; i++)
            {
                string requiredObjectId = steps[i].requiredObjectId;
                if (!string.IsNullOrWhiteSpace(requiredObjectId)
                    && !objectsById.ContainsKey(requiredObjectId.Trim()))
                {
                    error = "Step '" + steps[i].stepId + "' requires unknown object ID '"
                        + requiredObjectId + "'.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public bool TryStartLab(out string error)
        {
            if (!TryValidateContract(out error))
            {
                FailVisible(error);
                return false;
            }

            if (clearTelemetryOnStart)
            {
                telemetryEmitter.ClearHistory();
            }

            currentStepIndex = 0;
            selectedObject = null;
            labActive = true;
            labCompleted = false;
            SetStatus("lab", labId);
            SetStatus("contract", "running");
            Emit(LabStartedEvent, null);
            BeginCurrentStep();
            error = string.Empty;
            return true;
        }

        public bool TrySelectObject(EmVrLabObject labObject, out string error)
        {
            if (!RequireActiveLab(out error))
            {
                return false;
            }

            if (labObject == null || string.IsNullOrWhiteSpace(labObject.ObjectId)
                || !objectsById.ContainsKey(labObject.ObjectId))
            {
                error = "Selected object is missing or not registered in the lab contract.";
                FailVisible(error);
                return false;
            }

            string requiredObjectId = steps[currentStepIndex].requiredObjectId;
            if (!string.IsNullOrWhiteSpace(requiredObjectId)
                && !string.Equals(requiredObjectId, labObject.ObjectId, StringComparison.Ordinal))
            {
                error = "Step '" + CurrentStepId + "' requires object '"
                    + requiredObjectId + "', not '" + labObject.ObjectId + "'.";
                FailVisible(error);
                return false;
            }

            selectedObject = labObject;
            selectionCompletedThisStep = true;
            assistGrounding.SetSelectedObject(labObject);
            SetStatus("selected", labObject.ObjectId);
            Emit(ObjectSelectedEvent, labObject);
            error = string.Empty;
            return true;
        }

        public bool TryBeginObjectDrag(EmVrLabObject labObject, out string error)
        {
            if (!RequireActiveLab(out error))
            {
                return false;
            }

            if (dragActive)
            {
                error = "An object drag is already active.";
                FailVisible(error);
                return false;
            }

            if (labObject == null || selectedObject != labObject)
            {
                error = "The dragged object must be selected first.";
                FailVisible(error);
                return false;
            }

            dragActive = true;
            SetStatus("drag", "active");
            Emit(ObjectDragStartedEvent, labObject);
            error = string.Empty;
            return true;
        }

        public bool TryEndObjectDrag(EmVrLabObject labObject, out string error)
        {
            if (!RequireActiveLab(out error))
            {
                return false;
            }

            if (!dragActive || labObject == null || selectedObject != labObject)
            {
                error = "No matching object drag is active.";
                FailVisible(error);
                return false;
            }

            dragActive = false;
            dragCompletedThisStep = true;
            SetStatus("drag", "complete");
            Emit(ObjectDragEndedEvent, labObject);
            error = string.Empty;
            return true;
        }

        public bool TryCompleteCurrentStep(out string error)
        {
            if (!RequireActiveLab(out error))
            {
                return false;
            }

            StepDefinition step = steps[currentStepIndex];
            if (dragActive)
            {
                error = "Cannot complete step '" + step.stepId + "' while a drag is active.";
                FailVisible(error);
                return false;
            }

            if (step.requireSelection && !selectionCompletedThisStep)
            {
                error = "Step '" + step.stepId + "' requires an object selection.";
                FailVisible(error);
                return false;
            }

            if (step.requireCompletedDrag && !dragCompletedThisStep)
            {
                error = "Step '" + step.stepId + "' requires a completed object drag.";
                FailVisible(error);
                return false;
            }

            Emit(StepCompletedEvent, selectedObject);
            if (currentStepIndex + 1 < steps.Length)
            {
                currentStepIndex++;
                BeginCurrentStep();
            }
            else
            {
                labActive = false;
                labCompleted = true;
                Emit(LabCompletedEvent, selectedObject);
                SetStatus("contract", "complete");
            }

            error = string.Empty;
            return true;
        }

        void BeginCurrentStep()
        {
            selectionCompletedThisStep = false;
            dragActive = false;
            dragCompletedThisStep = false;
            assistGrounding.SetSessionContext(
                sessionId.Trim(),
                userId.Trim(),
                labId.Trim(),
                CurrentStepId,
                mode.Trim());
            SetStatus("step", (currentStepIndex + 1) + "/" + steps.Length
                + " " + CurrentStepId);
            Emit(StepStartedEvent, null);
        }

        bool TryBuildObjectMap(out string error)
        {
            objectsById.Clear();
            if (knownObjects == null || knownObjects.Length == 0)
            {
                error = "At least one known lab object is required.";
                return false;
            }

            for (int i = 0; i < knownObjects.Length; i++)
            {
                EmVrLabObject labObject = knownObjects[i];
                if (labObject == null || string.IsNullOrWhiteSpace(labObject.ObjectId))
                {
                    error = "Known object " + i + " is missing or has no object ID.";
                    return false;
                }

                if (objectsById.ContainsKey(labObject.ObjectId))
                {
                    error = "Duplicate object ID '" + labObject.ObjectId + "'.";
                    return false;
                }

                objectsById.Add(labObject.ObjectId, labObject);
            }

            error = string.Empty;
            return true;
        }

        bool RequireActiveLab(out string error)
        {
            if (labActive && currentStepIndex >= 0 && currentStepIndex < steps.Length)
            {
                error = string.Empty;
                return true;
            }

            error = "No lab contract session is active.";
            FailVisible(error);
            return false;
        }

        void Emit(string eventType, EmVrLabObject labObject)
        {
            ContractEventPayload payload = new ContractEventPayload
            {
                session_id = sessionId.Trim(),
                user_id = userId.Trim(),
                lab_id = this.labId.Trim(),
                step_id = CurrentStepId,
                object_id = labObject != null ? labObject.ObjectId : string.Empty,
                object_type = labObject != null ? labObject.ObjectType : string.Empty,
                step_index = currentStepIndex,
                step_count = steps.Length,
                position = labObject != null ? labObject.transform.position : Vector3.zero
            };
            telemetryEmitter.Emit(eventType, JsonUtility.ToJson(payload));
        }

        void SetStatus(string key, string value)
        {
            if (statusDisplay != null)
            {
                statusDisplay.SetStatus(key, value);
            }
        }

        void FailVisible(string error)
        {
            SetStatus("contract", "FAIL: " + error);
            Debug.LogError("[EMVR Lab Contract] FAIL - " + error, this);
        }

        [Serializable]
        sealed class ContractEventPayload
        {
            public string session_id;
            public string user_id;
            public string lab_id;
            public string step_id;
            public string object_id;
            public string object_type;
            public int step_index;
            public int step_count;
            public Vector3 position;
        }
    }
}
