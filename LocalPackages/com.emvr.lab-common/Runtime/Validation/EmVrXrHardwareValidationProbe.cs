using System;
using EMVR.Common.Assist;
using EMVR.Common.Interaction;
using EMVR.Common.Objects;
using EMVR.Common.Telemetry;
using EMVR.Common.UI;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace EMVR.Common.Validation
{
    /// <summary>
    /// Manual real-device validation. Simulated and mock devices are reported
    /// but deliberately cannot satisfy the hardware gate.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrXrHardwareValidationProbe : MonoBehaviour
    {
        public const string ExpectedLabId = "common_xr_hardware_validation";
        public const string ExpectedObjectId = "xr_hardware_validation_cube";

        public XRGrabInteractable grabInteractable;
        public EmVrLabObject labObject;
        public EmVrStatusDisplay statusDisplay;
        public EmVrTelemetryEmitter telemetryEmitter;
        public EmVrAssistGroundingProvider assistGrounding;
        [Min(0.001f)] public float requiredMovementDistance = 0.05f;
        [Min(0.05f)] public float deviceRefreshInterval = 0.25f;
        public bool requireBothControllers = true;

        bool referencesReady;
        bool hardwareReady;
        bool grabActive;
        bool pendingGrabValidation;
        bool hasPassed;
        Vector3 grabStartPosition;
        float maximumGrabDistance;
        float nextDeviceRefreshTime;
        int activeControllerRayCount;

        void OnEnable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OnSelectEntered);
                grabInteractable.selectExited.AddListener(OnSelectExited);
            }
        }

        void Start()
        {
            referencesReady = ValidateReferences();
            if (!referencesReady)
            {
                return;
            }

            statusDisplay.SetStatus("scene", "WAITING FOR REAL XR HARDWARE");
            statusDisplay.SetStatus("instruction", "connect HMD + controllers");
            statusDisplay.SetStatus("grab_test", "waiting");
            Debug.Log(
                "[EMVR XR Hardware Validation] READY - connect a real HMD and both controllers, "
                + "then ray-grab, move, and release the orange cube.",
                this);
        }

        void Update()
        {
            if (!referencesReady || hasPassed)
            {
                return;
            }

            if (Time.unscaledTime >= nextDeviceRefreshTime)
            {
                RefreshDeviceStatus();
                nextDeviceRefreshTime = Time.unscaledTime
                    + Mathf.Max(0.05f, deviceRefreshInterval);
            }

            if (grabActive)
            {
                maximumGrabDistance = Mathf.Max(
                    maximumGrabDistance,
                    Vector3.Distance(grabStartPosition, labObject.transform.position));
                statusDisplay.SetStatus(
                    "movement",
                    maximumGrabDistance.ToString("0.000") + " m");
            }

            if (pendingGrabValidation)
            {
                pendingGrabValidation = false;
                ValidateCompletedGrab();
            }
        }

        void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
                grabInteractable.selectExited.RemoveListener(OnSelectExited);
            }
        }

        void OnSelectEntered(SelectEnterEventArgs eventArgs)
        {
            if (!referencesReady || hasPassed)
            {
                return;
            }

            if (!hardwareReady)
            {
                statusDisplay.SetStatus(
                    "grab_test",
                    "IGNORED: real tracked hardware not ready");
                return;
            }

            grabActive = true;
            maximumGrabDistance = 0f;
            grabStartPosition = labObject.transform.position;
            statusDisplay.SetStatus("grab_test", "ACTIVE - move then release");
        }

        void OnSelectExited(SelectExitEventArgs eventArgs)
        {
            if (!grabActive)
            {
                return;
            }

            maximumGrabDistance = Mathf.Max(
                maximumGrabDistance,
                Vector3.Distance(grabStartPosition, labObject.transform.position));
            grabActive = false;
            pendingGrabValidation = true;
        }

        void RefreshDeviceStatus()
        {
            InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            bool headReady = IsPhysicalTrackedDevice(head);
            bool leftReady = IsPhysicalTrackedDevice(left);
            bool rightReady = IsPhysicalTrackedDevice(right);
            activeControllerRayCount = CountActiveControllerRays();

            statusDisplay.SetStatus("hmd", FormatDevice(head, headReady));
            statusDisplay.SetStatus("left_controller", FormatDevice(left, leftReady));
            statusDisplay.SetStatus("right_controller", FormatDevice(right, rightReady));
            statusDisplay.SetStatus("controller_rays", activeControllerRayCount + " active");

            bool controllersReady = requireBothControllers
                ? leftReady && rightReady
                : leftReady || rightReady;
            int requiredRayCount = requireBothControllers ? 2 : 1;
            hardwareReady = headReady && controllersReady
                && activeControllerRayCount >= requiredRayCount;
            statusDisplay.SetStatus(
                "hardware_gate",
                hardwareReady ? "PASS" : "WAITING");
            if (hardwareReady)
            {
                statusDisplay.SetStatus(
                    "instruction",
                    "ray-grab, move, and release orange cube");
            }
        }

        void ValidateCompletedGrab()
        {
            if (!hardwareReady)
            {
                statusDisplay.SetStatus("grab_test", "FAIL: hardware tracking was lost");
                return;
            }

            if (maximumGrabDistance < Mathf.Max(0.001f, requiredMovementDistance))
            {
                statusDisplay.SetStatus(
                    "grab_test",
                    "RETRY: move at least " + requiredMovementDistance.ToString("0.00") + " m");
                return;
            }

            bool hasStartedEvent = false;
            bool hasEndedEvent = false;
            for (int i = 0; i < telemetryEmitter.RecentEvents.Count; i++)
            {
                string eventType = telemetryEmitter.RecentEvents[i].event_type;
                hasStartedEvent |= string.Equals(
                    eventType,
                    "object_drag_started",
                    StringComparison.Ordinal);
                hasEndedEvent |= string.Equals(
                    eventType,
                    "object_drag_ended",
                    StringComparison.Ordinal);
            }

            if (!hasStartedEvent || !hasEndedEvent)
            {
                statusDisplay.SetStatus("grab_test", "FAIL: drag telemetry incomplete");
                return;
            }

            string groundingJson = assistGrounding.BuildAssistGroundingJson();
            bool groundingPassed = groundingJson.IndexOf(
                    "\"lab_id\":\"" + ExpectedLabId + "\"",
                    StringComparison.Ordinal) >= 0
                && groundingJson.IndexOf(ExpectedObjectId, StringComparison.Ordinal) >= 0;
            if (!groundingPassed)
            {
                Fail("Assist grounding is missing the hardware lab or object ID.");
                return;
            }

            hasPassed = true;
            statusDisplay.SetStatus("movement", maximumGrabDistance.ToString("0.000") + " m PASS");
            statusDisplay.SetStatus("telemetry", "PASS: drag started + ended");
            statusDisplay.SetStatus("assist", "PASS");
            statusDisplay.SetStatus("grab_test", "PASS");
            statusDisplay.SetStatus("scene", "PASS");
            Debug.Log(
                "[EMVR XR Hardware Validation] PASS - physical HMD/controllers, "
                + "controller rays, XR grab movement, telemetry, and assist grounding are connected.",
                this);
        }

        bool ValidateReferences()
        {
            if (grabInteractable == null || labObject == null || statusDisplay == null
                || telemetryEmitter == null || assistGrounding == null)
            {
                Fail("One or more validation references are missing.");
                return false;
            }

            if (!string.Equals(labObject.ObjectId, ExpectedObjectId, StringComparison.Ordinal))
            {
                Fail("Unexpected hardware validation object ID: " + labObject.ObjectId + ".");
                return false;
            }

            return true;
        }

        int CountActiveControllerRays()
        {
            XRRayInteractor[] rays = FindObjectsOfType<XRRayInteractor>(true);
            int count = 0;
            for (int i = 0; i < rays.Length; i++)
            {
                if (EmVrXrRayUtility.IsControllerRay(rays[i])
                    && rays[i].isActiveAndEnabled)
                {
                    count++;
                }
            }

            return count;
        }

        static bool IsPhysicalTrackedDevice(InputDevice device)
        {
            if (!device.isValid || IsSimulationName(device.name))
            {
                return false;
            }

            bool isTracked;
            if (device.TryGetFeatureValue(CommonUsages.isTracked, out isTracked))
            {
                return isTracked;
            }

            InputTrackingState trackingState;
            return device.TryGetFeatureValue(CommonUsages.trackingState, out trackingState)
                && (trackingState & (InputTrackingState.Position | InputTrackingState.Rotation))
                    != InputTrackingState.None;
        }

        static string FormatDevice(InputDevice device, bool ready)
        {
            if (!device.isValid)
            {
                return "missing";
            }

            string safeName = string.IsNullOrWhiteSpace(device.name)
                ? "unnamed device"
                : device.name;
            if (IsSimulationName(safeName))
            {
                return "SIMULATED: " + safeName;
            }

            return (ready ? "TRACKED: " : "not tracked: ") + safeName;
        }

        static bool IsSimulationName(string deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
            {
                return false;
            }

            string normalized = deviceName.ToLowerInvariant();
            return normalized.Contains("simulat")
                || normalized.Contains("mock")
                || normalized.Contains("synthetic");
        }

        void Fail(string message)
        {
            hasPassed = false;
            if (statusDisplay != null)
            {
                statusDisplay.SetStatus("scene", "FAIL");
                statusDisplay.SetStatus("error", message);
            }

            Debug.LogError("[EMVR XR Hardware Validation] FAIL - " + message, this);
        }
    }
}
