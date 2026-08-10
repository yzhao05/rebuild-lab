using EMVR.Common.Assist;
using EMVR.Common.Interaction;
using EMVR.Common.Objects;
using EMVR.Common.Telemetry;
using EMVR.Common.UI;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace EMVR.Common.Validation
{
    /// <summary>
    /// Runtime smoke-test probe used by the generated minimal validation scene.
    /// The scene passes after a drag completes and its assist payload contains
    /// the expected lab and object identities.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrMinimalValidationProbe : MonoBehaviour
    {
        public const string ExpectedLabId = "emvr_common_minimal_validation";
        public const string ExpectedObjectId = "validation_draggable_cube";

        public EmVrMousePointer mousePointer;
        public EmVrPlanarMouseDraggable draggable;
        public XRGrabInteractable xrGrabInteractable;
        public EmVrLabObject labObject;
        public EmVrStatusDisplay statusDisplay;
        public EmVrTelemetryEmitter telemetryEmitter;
        public EmVrAssistGroundingProvider assistGrounding;

        public bool HasPassed { get; private set; }

        bool ready;

        void Start()
        {
            PrepareLegacyGeneratedScene();
            ready = ValidateReferencesAndScene();
            if (!ready)
            {
                return;
            }

            telemetryEmitter.EventEmitted += OnTelemetryEvent;
            statusDisplay.SetStatus("scene", "READY");
            statusDisplay.SetStatus("drag_test", "drag the orange cube");
            statusDisplay.SetStatus("telemetry", "waiting");
            statusDisplay.SetStatus("assist", "waiting");
            Debug.Log("[EMVR Minimal Validation] READY - drag the orange cube with the left mouse button.", this);
        }

        void OnDestroy()
        {
            if (telemetryEmitter != null)
            {
                telemetryEmitter.EventEmitted -= OnTelemetryEvent;
            }
        }

        bool ValidateReferencesAndScene()
        {
            if (mousePointer == null
                || draggable == null
                || labObject == null
                || statusDisplay == null
                || telemetryEmitter == null
                || assistGrounding == null)
            {
                Fail("One or more validation references are missing.");
                return false;
            }

            int activeListenerCount = 0;
            AudioListener[] listeners = FindObjectsOfType<AudioListener>(true);
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != null && listeners[i].isActiveAndEnabled)
                {
                    activeListenerCount++;
                }
            }

            if (activeListenerCount != 1)
            {
                Fail("Expected one active AudioListener, found " + activeListenerCount + ".");
                return false;
            }

            if (labObject.ObjectId != ExpectedObjectId)
            {
                Fail("Unexpected lab object ID: " + labObject.ObjectId + ".");
                return false;
            }

            return true;
        }

        void PrepareLegacyGeneratedScene()
        {
            if (FindObjectOfType<EmVrMinimalValidationViewMode>(true) != null)
            {
                return;
            }

            Camera desktopCamera = mousePointer != null
                ? mousePointer.GetComponent<Camera>()
                : null;
            if (desktopCamera != null)
            {
                desktopCamera.stereoTargetEye = StereoTargetEyeMask.None;
            }

            if (draggable != null)
            {
                draggable.handleUnityMouseMessages = true;
            }
        }

        void OnTelemetryEvent(EmVrTelemetryEmitter.TelemetryEvent telemetryEvent)
        {
            if (!ready || telemetryEvent == null)
            {
                return;
            }

            if (telemetryEvent.event_type != "object_drag_ended")
            {
                statusDisplay.SetStatus(
                    "telemetry",
                    telemetryEvent.event_type == "object_drag_started" ? "started" : "moving");
                return;
            }

            statusDisplay.SetStatus("telemetry", "PASS: object_drag_ended");
            string groundingJson = assistGrounding.BuildAssistGroundingJson();
            bool assistPassed = groundingJson.Contains("\"lab_id\":\"" + ExpectedLabId + "\"")
                && groundingJson.Contains(ExpectedObjectId);
            statusDisplay.SetStatus("assist", assistPassed ? "PASS" : "FAIL");
            statusDisplay.SetStatus("drag_test", "PASS");

            if (!assistPassed)
            {
                Fail("Assist grounding payload did not contain the expected lab and object IDs.");
                return;
            }

            HasPassed = true;
            statusDisplay.SetStatus("scene", "PASS");
            Debug.Log("[EMVR Minimal Validation] PASS - pointer, drag, status, telemetry, and assist grounding are connected.", this);
        }

        void Fail(string message)
        {
            HasPassed = false;
            if (statusDisplay != null)
            {
                statusDisplay.SetStatus("scene", "FAIL");
                statusDisplay.SetStatus("error", message);
            }

            Debug.LogError("[EMVR Minimal Validation] FAIL - " + message, this);
        }
    }
}
