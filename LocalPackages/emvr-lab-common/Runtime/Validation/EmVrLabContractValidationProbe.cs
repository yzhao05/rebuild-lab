using System;
using System.Collections;
using System.Collections.Generic;
using EMVR.Common.Assist;
using EMVR.Common.Objects;
using EMVR.Common.SceneFlow;
using EMVR.Common.Telemetry;
using EMVR.Common.UI;
using UnityEngine;

namespace EMVR.Common.Validation
{
    /// <summary>
    /// Automated proof that a generated lab contract rejects missing fields,
    /// advances steps, emits the canonical event sequence, and keeps assist
    /// grounding synchronized with the active step and selected object.
    /// </summary>
    public sealed class EmVrLabContractValidationProbe : MonoBehaviour
    {
        public const string ExpectedLabId = "common_contract_validation";
        public const string ExpectedObjectId = "contract_validation_object";
        public const string FirstStepId = "select_and_drag";
        public const string SecondStepId = "confirm_result";

        static readonly string[] ExpectedEventSequence =
        {
            EmVrLabContractRunner.LabStartedEvent,
            EmVrLabContractRunner.StepStartedEvent,
            EmVrLabContractRunner.ObjectSelectedEvent,
            EmVrLabContractRunner.ObjectDragStartedEvent,
            EmVrLabContractRunner.ObjectDragEndedEvent,
            EmVrLabContractRunner.StepCompletedEvent,
            EmVrLabContractRunner.StepStartedEvent,
            EmVrLabContractRunner.StepCompletedEvent,
            EmVrLabContractRunner.LabCompletedEvent
        };

        public EmVrLabContractRunner contractRunner;
        public EmVrLabObject labObject;
        public EmVrTelemetryEmitter telemetryEmitter;
        public EmVrAssistGroundingProvider assistGrounding;
        public EmVrStatusDisplay statusDisplay;
        [Min(0f)] public float stepDelaySeconds = 0.55f;
        public bool runOnStart = true;

        bool running;

        void Start()
        {
            if (runOnStart)
            {
                RunValidation();
            }
        }

        [ContextMenu("Run Lab Contract Validation")]
        public void RunValidation()
        {
            if (!running)
            {
                StartCoroutine(RunValidationSequence());
            }
        }

        IEnumerator RunValidationSequence()
        {
            running = true;
            SetStatus("result", "RUNNING");
            SetStatus("phase", "checking required fields");

            string error;
            if (!ValidateReferences(out error))
            {
                Fail(error);
                yield break;
            }

            string validLabId = contractRunner.labId;
            contractRunner.labId = string.Empty;
            string missingFieldError;
            bool incorrectlyAccepted = contractRunner.TryValidateContract(out missingFieldError);
            contractRunner.labId = validLabId;
            if (incorrectlyAccepted || string.IsNullOrWhiteSpace(missingFieldError))
            {
                Fail("Contract validation did not reject a missing labId.");
                yield break;
            }

            SetStatus("missing_fields", "PASS (explicitly rejected)");
            yield return WaitForStep();

            Vector3 authoredPosition = labObject.transform.position;
            Quaternion authoredRotation = labObject.transform.rotation;

            SetStatus("phase", "starting lab + step 1");
            if (!contractRunner.TryStartLab(out error))
            {
                Fail("Start failed: " + error);
                yield break;
            }

            if (!ValidateCurrentGrounding(FirstStepId, false, out error))
            {
                Fail("Initial grounding failed: " + error);
                yield break;
            }

            yield return WaitForStep();

            SetStatus("phase", "selecting + dragging contract object");
            if (!contractRunner.TrySelectObject(labObject, out error)
                || !contractRunner.TryBeginObjectDrag(labObject, out error))
            {
                Fail("Interaction start failed: " + error);
                yield break;
            }

            labObject.transform.position += new Vector3(1.2f, 0.75f, 0.6f);
            labObject.transform.rotation = Quaternion.Euler(16f, 48f, 12f);
            if (!contractRunner.TryEndObjectDrag(labObject, out error))
            {
                Fail("Interaction end failed: " + error);
                yield break;
            }

            if (!ValidateCurrentGrounding(FirstStepId, true, out error))
            {
                Fail("Selected-object grounding failed: " + error);
                yield break;
            }

            SetStatus("grounding_step_1", "PASS");
            yield return WaitForStep();

            SetStatus("phase", "completing step 1 -> step 2");
            if (!contractRunner.TryCompleteCurrentStep(out error))
            {
                Fail("Step 1 completion failed: " + error);
                yield break;
            }

            if (contractRunner.CurrentStepIndex != 1
                || !string.Equals(
                    contractRunner.CurrentStepId,
                    SecondStepId,
                    StringComparison.Ordinal))
            {
                Fail("Step navigation did not advance to '" + SecondStepId + "'.");
                yield break;
            }

            if (!ValidateCurrentGrounding(SecondStepId, true, out error))
            {
                Fail("Step 2 grounding failed: " + error);
                yield break;
            }

            SetStatus("step_navigation", "PASS (1/2 -> 2/2)");
            SetStatus("grounding_step_2", "PASS");
            yield return WaitForStep();

            SetStatus("phase", "completing lab");
            if (!contractRunner.TryCompleteCurrentStep(out error))
            {
                Fail("Step 2 completion failed: " + error);
                yield break;
            }

            if (!contractRunner.IsLabCompleted || contractRunner.IsLabActive)
            {
                Fail("The contract did not reach its completed state.");
                yield break;
            }

            if (!ValidateTelemetry(out error))
            {
                Fail("Telemetry validation failed: " + error);
                yield break;
            }

            labObject.transform.position = authoredPosition;
            labObject.transform.rotation = authoredRotation;
            SetStatus("telemetry_order", "PASS (9 canonical events)");
            SetStatus("phase", "complete");
            SetStatus("result", "PASS");
            Debug.Log(
                "[EMVR Lab Contract Validation] PASS - required fields, lab/step/object IDs, "
                + "step navigation, telemetry order, and assist grounding are connected.",
                this);
            running = false;
        }

        bool ValidateReferences(out string error)
        {
            if (contractRunner == null || labObject == null || telemetryEmitter == null
                || assistGrounding == null || statusDisplay == null)
            {
                error = "One or more validation references are missing.";
                return false;
            }

            if (!string.Equals(contractRunner.labId, ExpectedLabId, StringComparison.Ordinal)
                || !string.Equals(labObject.ObjectId, ExpectedObjectId, StringComparison.Ordinal))
            {
                error = "Lab or object ID does not match the validation contract.";
                return false;
            }

            if (contractRunner.steps == null || contractRunner.steps.Length != 2
                || contractRunner.steps[0] == null || contractRunner.steps[1] == null
                || !string.Equals(
                    contractRunner.steps[0].stepId,
                    FirstStepId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    contractRunner.steps[1].stepId,
                    SecondStepId,
                    StringComparison.Ordinal))
            {
                error = "The validation contract must contain its two stable step IDs.";
                return false;
            }

            return contractRunner.TryValidateContract(out error);
        }

        bool ValidateCurrentGrounding(
            string expectedStepId,
            bool requireSelectedObject,
            out string error)
        {
            string json = assistGrounding.BuildAssistGroundingJson();
            AssistPayload payload;
            try
            {
                payload = JsonUtility.FromJson<AssistPayload>(json);
            }
            catch (Exception exception)
            {
                error = "Grounding JSON is invalid: " + exception.Message;
                return false;
            }

            if (payload == null
                || string.IsNullOrWhiteSpace(payload.session_id)
                || string.IsNullOrWhiteSpace(payload.user_id)
                || !string.Equals(payload.lab_id, ExpectedLabId, StringComparison.Ordinal)
                || !string.Equals(payload.step_id, expectedStepId, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(payload.mode))
            {
                error = "Required session/lab/step grounding fields are missing or stale.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(payload.objects_json)
                || payload.objects_json.IndexOf(ExpectedObjectId, StringComparison.Ordinal) < 0)
            {
                error = "Grounding does not contain the registered lab object.";
                return false;
            }

            if (requireSelectedObject
                && (string.IsNullOrWhiteSpace(payload.selected_object_json)
                    || payload.selected_object_json.IndexOf(
                        ExpectedObjectId,
                        StringComparison.Ordinal) < 0))
            {
                error = "Grounding does not contain the selected object state.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(payload.recent_actions_json)
                || payload.recent_actions_json.IndexOf(
                    EmVrLabContractRunner.LabStartedEvent,
                    StringComparison.Ordinal) < 0)
            {
                error = "Grounding does not contain recent telemetry actions.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        bool ValidateTelemetry(out string error)
        {
            IReadOnlyList<EmVrTelemetryEmitter.TelemetryEvent> events =
                telemetryEmitter.RecentEvents;
            if (events.Count != ExpectedEventSequence.Length)
            {
                error = "Expected " + ExpectedEventSequence.Length + " events, found "
                    + events.Count + ".";
                return false;
            }

            for (int i = 0; i < events.Count; i++)
            {
                EmVrTelemetryEmitter.TelemetryEvent telemetryEvent = events[i];
                if (!string.Equals(
                    telemetryEvent.event_type,
                    ExpectedEventSequence[i],
                    StringComparison.Ordinal))
                {
                    error = "Event " + i + " is '" + telemetryEvent.event_type
                        + "', expected '" + ExpectedEventSequence[i] + "'.";
                    return false;
                }

                EventPayload payload;
                try
                {
                    payload = JsonUtility.FromJson<EventPayload>(telemetryEvent.payload_json);
                }
                catch (Exception exception)
                {
                    error = "Event " + i + " payload is invalid: " + exception.Message;
                    return false;
                }

                string expectedStepId = i <= 5 ? FirstStepId : SecondStepId;
                if (payload == null
                    || string.IsNullOrWhiteSpace(payload.session_id)
                    || string.IsNullOrWhiteSpace(payload.user_id)
                    || !string.Equals(payload.lab_id, ExpectedLabId, StringComparison.Ordinal)
                    || !string.Equals(payload.step_id, expectedStepId, StringComparison.Ordinal)
                    || payload.step_count != 2)
                {
                    error = "Event " + i + " is missing a required contract field.";
                    return false;
                }

                bool objectEvent = i >= 2 && i <= 4;
                if (objectEvent
                    && !string.Equals(
                        payload.object_id,
                        ExpectedObjectId,
                        StringComparison.Ordinal))
                {
                    error = "Object event " + i + " has the wrong object ID.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        object WaitForStep()
        {
            return stepDelaySeconds <= 0f
                ? null
                : (object)new WaitForSecondsRealtime(stepDelaySeconds);
        }

        void SetStatus(string key, string value)
        {
            statusDisplay.SetStatus(key, value);
        }

        void Fail(string message)
        {
            SetStatus("phase", "failed");
            SetStatus("result", "FAIL: " + message);
            Debug.LogError("[EMVR Lab Contract Validation] FAIL - " + message, this);
            running = false;
        }

        [Serializable]
        sealed class AssistPayload
        {
            public string session_id;
            public string user_id;
            public string lab_id;
            public string step_id;
            public string mode;
            public string selected_object_json;
            public string objects_json;
            public string recent_actions_json;
        }

        [Serializable]
        sealed class EventPayload
        {
            public string session_id;
            public string user_id;
            public string lab_id;
            public string step_id;
            public string object_id;
            public int step_count;
        }
    }
}
