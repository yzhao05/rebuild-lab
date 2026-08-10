using System;
using System.Collections;
using EMVR.Common.Objects;
using EMVR.Common.SceneFlow;
using EMVR.Common.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace EMVR.Common.Validation
{
    /// <summary>
    /// Runs Start -> Lab -> Reset -> Back repeatedly and verifies that persistent
    /// services, XR origin, audio, EventSystem, and input routing remain singular.
    /// </summary>
    public sealed class EmVrSceneLifecycleValidationProbe : MonoBehaviour
    {
        public const string StartRole = "start";
        public const string LabRole = "lab";
        public const string ExpectedResetObjectId = "lifecycle_reset_cube";

        public EmVrPersistentServiceRoot persistentRoot;
        public EmVrSceneLoader sceneLoader;
        public EmVrStatusDisplay statusDisplay;
        public GameObject inputProbeTarget;
        public string startSceneName;
        public string labSceneName;
        [Min(1)] public int repetitionCount = 3;
        [Min(0f)] public float stageDelaySeconds = 0.2f;
        [Min(1f)] public float sceneLoadTimeoutSeconds = 8f;

        bool sceneWaitSucceeded;
        string sceneWaitError;

        IEnumerator Start()
        {
            if (persistentRoot == null || !persistentRoot.IsPrimary)
            {
                yield break;
            }

            string error;
            if (!ValidateConfiguration(out error))
            {
                Fail(error);
                yield break;
            }

            statusDisplay.SetStatus("result", "RUNNING");
            statusDisplay.SetStatus("route", "Start -> Lab -> Reset -> Back");
            statusDisplay.SetStatus("cycle", "0/" + repetitionCount);
            yield return WaitForSettledFrame();

            for (int cycle = 1; cycle <= repetitionCount; cycle++)
            {
                statusDisplay.SetStatus("phase", "checking Start");
                if (!ValidateSceneAndServices(StartRole, out error))
                {
                    Fail("Start cycle " + cycle + ": " + error);
                    yield break;
                }

                statusDisplay.SetStatus("phase", "loading Lab");
                if (!sceneLoader.TryLoadScene(labSceneName))
                {
                    Fail("Could not request Lab scene load. Check Build Settings.");
                    yield break;
                }

                yield return WaitForScene(labSceneName, -1);
                if (!sceneWaitSucceeded)
                {
                    Fail(sceneWaitError);
                    yield break;
                }

                if (!ValidateSceneAndServices(LabRole, out error))
                {
                    Fail("Lab cycle " + cycle + ": " + error);
                    yield break;
                }

                EmVrSceneLifecycleMarker labMarker =
                    FindObjectOfType<EmVrSceneLifecycleMarker>(true);
                if (labMarker == null || labMarker.resetValidationObject == null)
                {
                    Fail("Lab reset marker or reset object is missing.");
                    yield break;
                }

                EmVrLabObject oldResetObject = labMarker.resetValidationObject;
                Vector3 authoredPosition = oldResetObject.transform.position;
                int oldMarkerInstanceId = labMarker.GetInstanceID();
                oldResetObject.transform.position += new Vector3(1.4f, 1.1f, 0.7f);
                statusDisplay.SetStatus("reset", "mutated; reloading Lab");
                yield return StageDelay();

                sceneLoader.ReloadActiveScene();
                yield return WaitForScene(labSceneName, oldMarkerInstanceId);
                if (!sceneWaitSucceeded)
                {
                    Fail(sceneWaitError);
                    yield break;
                }

                if (!ValidateSceneAndServices(LabRole, out error))
                {
                    Fail("Reset cycle " + cycle + ": " + error);
                    yield break;
                }

                EmVrSceneLifecycleMarker reloadedMarker =
                    FindObjectOfType<EmVrSceneLifecycleMarker>(true);
                if (reloadedMarker == null || reloadedMarker.resetValidationObject == null
                    || reloadedMarker.GetInstanceID() == oldMarkerInstanceId
                    || Vector3.Distance(
                        reloadedMarker.resetValidationObject.transform.position,
                        authoredPosition) > 0.0001f)
                {
                    Fail("Lab Reset did not recreate the authored object state.");
                    yield break;
                }

                statusDisplay.SetStatus("reset", "PASS");
                statusDisplay.SetStatus("phase", "returning to Start");
                if (!sceneLoader.TryLoadScene(startSceneName))
                {
                    Fail("Could not request Start scene load. Check Build Settings.");
                    yield break;
                }

                yield return WaitForScene(startSceneName, -1);
                if (!sceneWaitSucceeded)
                {
                    Fail(sceneWaitError);
                    yield break;
                }

                if (!ValidateSceneAndServices(StartRole, out error))
                {
                    Fail("Back cycle " + cycle + ": " + error);
                    yield break;
                }

                statusDisplay.SetStatus("cycle", cycle + "/" + repetitionCount + " PASS");
                yield return StageDelay();
            }

            if (EmVrPersistentServiceRoot.DuplicateDestroyCount < repetitionCount)
            {
                Fail("Bootstrap duplicates were not detected and removed on every return.");
                yield break;
            }

            statusDisplay.SetStatus(
                "duplicates_removed",
                EmVrPersistentServiceRoot.DuplicateDestroyCount.ToString());
            statusDisplay.SetStatus("phase", "complete");
            statusDisplay.SetStatus("result", "PASS");
            Debug.Log(
                "[EMVR Scene Lifecycle Validation] PASS - Start, Lab, Reset, and Back "
                + "completed " + repetitionCount + " cycles with one persistent service root, "
                + "XR Origin, EventSystem, AudioListener, and working input route.",
                this);
        }

        bool ValidateConfiguration(out string error)
        {
            if (persistentRoot == null || sceneLoader == null || statusDisplay == null
                || inputProbeTarget == null)
            {
                error = "One or more persistent validation references are missing.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(startSceneName)
                || string.IsNullOrWhiteSpace(labSceneName))
            {
                error = "Start and Lab scene names are required.";
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(startSceneName)
                || !Application.CanStreamedLevelBeLoaded(labSceneName))
            {
                error = "Start and Lab scenes must both be enabled in Build Settings.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        bool ValidateSceneAndServices(string expectedRole, out string error)
        {
            EmVrSceneLifecycleMarker marker =
                FindObjectOfType<EmVrSceneLifecycleMarker>(true);
            if (marker == null
                || !string.Equals(marker.sceneRole, expectedRole, StringComparison.Ordinal))
            {
                error = "Expected scene role '" + expectedRole + "'.";
                return false;
            }

            EmVrPersistentServiceRoot[] serviceRoots =
                FindObjectsOfType<EmVrPersistentServiceRoot>(true);
            if (serviceRoots.Length != 1 || serviceRoots[0] != persistentRoot)
            {
                error = "Expected one persistent service root, found "
                    + serviceRoots.Length + ".";
                return false;
            }

            int activeListeners = 0;
            AudioListener[] listeners = FindObjectsOfType<AudioListener>(true);
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != null && listeners[i].isActiveAndEnabled)
                {
                    activeListeners++;
                }
            }

            if (activeListeners != 1)
            {
                error = "Expected one active AudioListener, found " + activeListeners + ".";
                return false;
            }

            EventSystem[] eventSystems = FindObjectsOfType<EventSystem>(true);
            if (eventSystems.Length != 1 || EventSystem.current != eventSystems[0]
                || !eventSystems[0].isActiveAndEnabled)
            {
                error = "Expected one active current EventSystem, found "
                    + eventSystems.Length + ".";
                return false;
            }

            BaseInputModule[] inputModules =
                eventSystems[0].GetComponents<BaseInputModule>();
            int enabledInputModules = 0;
            for (int i = 0; i < inputModules.Length; i++)
            {
                if (inputModules[i] != null && inputModules[i].isActiveAndEnabled)
                {
                    enabledInputModules++;
                }
            }

            if (enabledInputModules < 1)
            {
                error = "The persistent EventSystem has no enabled input module.";
                return false;
            }

            int xrOriginCount = CountComponentsByFullName("Unity.XR.CoreUtils.XROrigin");
            if (xrOriginCount != 1)
            {
                error = "Expected one XR Origin, found " + xrOriginCount + ".";
                return false;
            }

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(inputProbeTarget);
            if (EventSystem.current.currentSelectedGameObject != inputProbeTarget)
            {
                error = "EventSystem selection routing did not survive the scene transition.";
                return false;
            }

            statusDisplay.SetStatus("services", "PASS: 1 persistent root");
            statusDisplay.SetStatus("xr_origin", "PASS: 1");
            statusDisplay.SetStatus("event_system", "PASS: 1");
            statusDisplay.SetStatus("audio_listener", "PASS: 1");
            statusDisplay.SetStatus("input_route", "PASS");
            error = string.Empty;
            return true;
        }

        static int CountComponentsByFullName(string fullName)
        {
            MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>(true);
            int count = 0;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null
                    && string.Equals(
                        behaviours[i].GetType().FullName,
                        fullName,
                        StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        IEnumerator WaitForScene(string expectedSceneName, int forbiddenMarkerInstanceId)
        {
            sceneWaitSucceeded = false;
            sceneWaitError = string.Empty;
            float deadline = Time.realtimeSinceStartup
                + Mathf.Max(1f, sceneLoadTimeoutSeconds);
            while (Time.realtimeSinceStartup < deadline)
            {
                Scene activeScene = SceneManager.GetActiveScene();
                EmVrSceneLifecycleMarker marker =
                    FindObjectOfType<EmVrSceneLifecycleMarker>(true);
                bool markerIsNew = marker != null
                    && (forbiddenMarkerInstanceId < 0
                        || marker.GetInstanceID() != forbiddenMarkerInstanceId);
                if (!sceneLoader.IsLoading
                    && activeScene.IsValid()
                    && activeScene.isLoaded
                    && string.Equals(activeScene.name, expectedSceneName, StringComparison.Ordinal)
                    && markerIsNew)
                {
                    yield return WaitForSettledFrame();
                    sceneWaitSucceeded = true;
                    yield break;
                }

                yield return null;
            }

            sceneWaitError = "Timed out waiting for scene '" + expectedSceneName + "'.";
        }

        IEnumerator WaitForSettledFrame()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return null;
        }

        object StageDelay()
        {
            return stageDelaySeconds <= 0f
                ? null
                : (object)new WaitForSecondsRealtime(stageDelaySeconds);
        }

        void Fail(string message)
        {
            if (statusDisplay != null)
            {
                statusDisplay.SetStatus("phase", "failed");
                statusDisplay.SetStatus("result", "FAIL: " + message);
            }

            Debug.LogError("[EMVR Scene Lifecycle Validation] FAIL - " + message, this);
        }
    }
}
