using System;
using System.Collections;
using EMVR.Common.Objects;
using EMVR.Common.UI;
using UnityEngine;

namespace EMVR.Common.Validation
{
    /// <summary>
    /// Automated runtime proof for object identity, parameter state, JSON
    /// round-tripping, snapshot restore, and authored reset behavior.
    /// </summary>
    public sealed class EmVrObjectStateValidationProbe : MonoBehaviour
    {
        public const string ExpectedTransformObjectId = "validation_transform_cube";
        public const string ExpectedParameterizedObjectId = "validation_parameter_cylinder";

        public EmVrObjectStateSnapshotStore snapshotStore;
        public EmVrLabObject transformObject;
        public EmVrParameterizedLabObject parameterizedObject;
        public EmVrStatusDisplay statusDisplay;
        [Min(0f)] public float stepDelaySeconds = 0.65f;
        public bool runOnStart = true;

        bool running;

        void Start()
        {
            if (runOnStart)
            {
                RunValidation();
            }
        }

        [ContextMenu("Run Object State Validation")]
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
            SetStatus("phase", "checking contracts");

            string referenceError;
            if (!ValidateReferences(out referenceError))
            {
                Fail(referenceError);
                yield break;
            }

            Vector3 transformPosition = transformObject.transform.position;
            Quaternion transformRotation = transformObject.transform.rotation;
            Vector3 transformScale = transformObject.transform.localScale;
            Vector3 parameterPosition = parameterizedObject.transform.position;
            Quaternion parameterRotation = parameterizedObject.transform.rotation;
            Vector3 parameterScale = parameterizedObject.transform.localScale;
            float parameterValue = parameterizedObject.ParameterValue;

            string baselineJson;
            string error;
            if (!snapshotStore.TryCaptureSnapshot(out baselineJson, out error))
            {
                Fail("Capture failed: " + error);
                yield break;
            }

            SetStatus("capture", "PASS (2 unique IDs)");
            SetStatus("json_bytes", baselineJson.Length.ToString());

            string roundTrippedJson;
            if (!snapshotStore.TryRoundTripJson(baselineJson, out roundTrippedJson, out error)
                || !string.Equals(baselineJson, roundTrippedJson, StringComparison.Ordinal))
            {
                Fail("JSON round trip was not deterministic. " + error);
                yield break;
            }

            SetStatus("json_roundtrip", "PASS");
            yield return WaitForStep();

            SetStatus("phase", "mutating transform + parameter");
            MutateObjects();
            SetStatus("parameter", parameterizedObject.ParameterName + "="
                + parameterizedObject.ParameterValue.ToString("0.00"));
            yield return WaitForStep();

            SetStatus("phase", "restoring captured JSON");
            if (!snapshotStore.TryRestoreSnapshot(roundTrippedJson, out error))
            {
                Fail("Restore failed: " + error);
                yield break;
            }

            if (!MatchesAuthoredState(
                    transformPosition,
                    transformRotation,
                    transformScale,
                    parameterPosition,
                    parameterRotation,
                    parameterScale,
                    parameterValue))
            {
                Fail("Restored state does not match the captured state.");
                yield break;
            }

            SetStatus("restore", "PASS");
            SetStatus("parameter", parameterizedObject.ParameterName + "="
                + parameterizedObject.ParameterValue.ToString("0.00"));
            yield return WaitForStep();

            SetStatus("phase", "mutating again before Reset");
            MutateObjects();
            yield return WaitForStep();

            SetStatus("phase", "resetting to scene-authored state");
            if (!snapshotStore.TryResetObjects(out error))
            {
                Fail("Reset failed: " + error);
                yield break;
            }

            if (!MatchesAuthoredState(
                    transformPosition,
                    transformRotation,
                    transformScale,
                    parameterPosition,
                    parameterRotation,
                    parameterScale,
                    parameterValue))
            {
                Fail("Reset state does not match the scene-authored state.");
                yield break;
            }

            SetStatus("reset", "PASS");
            SetStatus("phase", "complete");
            SetStatus("result", "PASS");
            Debug.Log(
                "[EMVR Object State Validation] PASS - two object types, unique IDs, "
                + "transform/parameter state, JSON round trip, restore, and reset are connected.",
                this);
            running = false;
        }

        bool ValidateReferences(out string error)
        {
            if (snapshotStore == null || transformObject == null
                || parameterizedObject == null || statusDisplay == null)
            {
                error = "One or more validation references are missing.";
                return false;
            }

            if (!string.Equals(
                    transformObject.ObjectId,
                    ExpectedTransformObjectId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    parameterizedObject.ObjectId,
                    ExpectedParameterizedObjectId,
                    StringComparison.Ordinal))
            {
                error = "Validation object IDs do not match their stable contract IDs.";
                return false;
            }

            if (string.Equals(
                transformObject.ObjectId,
                parameterizedObject.ObjectId,
                StringComparison.Ordinal))
            {
                error = "Validation object IDs are not unique.";
                return false;
            }

            if (string.Equals(
                transformObject.ObjectType,
                parameterizedObject.ObjectType,
                StringComparison.Ordinal))
            {
                error = "Validation requires two distinct object types.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        void MutateObjects()
        {
            transformObject.transform.position += new Vector3(0f, 1.25f, 0.8f);
            transformObject.transform.rotation = Quaternion.Euler(18f, 55f, 8f);
            transformObject.transform.localScale = new Vector3(0.65f, 1.45f, 0.85f);

            parameterizedObject.transform.position += new Vector3(0f, 0.9f, -0.75f);
            parameterizedObject.transform.rotation = Quaternion.Euler(25f, -48f, 30f);
            parameterizedObject.transform.localScale = new Vector3(1.3f, 0.7f, 1.3f);
            parameterizedObject.SetParameterValue(parameterizedObject.ParameterValue + 4.25f);
        }

        bool MatchesAuthoredState(
            Vector3 transformPosition,
            Quaternion transformRotation,
            Vector3 transformScale,
            Vector3 parameterPosition,
            Quaternion parameterRotation,
            Vector3 parameterScale,
            float parameterValue)
        {
            const float positionTolerance = 0.0001f;
            const float angleTolerance = 0.02f;
            return Vector3.Distance(transformObject.transform.position, transformPosition)
                       <= positionTolerance
                && Quaternion.Angle(transformObject.transform.rotation, transformRotation)
                       <= angleTolerance
                && Vector3.Distance(transformObject.transform.localScale, transformScale)
                       <= positionTolerance
                && Vector3.Distance(parameterizedObject.transform.position, parameterPosition)
                       <= positionTolerance
                && Quaternion.Angle(parameterizedObject.transform.rotation, parameterRotation)
                       <= angleTolerance
                && Vector3.Distance(parameterizedObject.transform.localScale, parameterScale)
                       <= positionTolerance
                && Mathf.Abs(parameterizedObject.ParameterValue - parameterValue)
                       <= positionTolerance;
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
            Debug.LogError("[EMVR Object State Validation] FAIL - " + message, this);
            running = false;
        }
    }
}
