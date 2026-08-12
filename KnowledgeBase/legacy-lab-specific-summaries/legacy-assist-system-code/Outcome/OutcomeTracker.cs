using System.Collections;
using EMVR.Assist.Core;
using EMVR.Assist.Data;
using EMVR.Assist.Logging;
using EMVR.Assist.Network;
using UnityEngine;

namespace EMVR.Assist.Outcome
{
    public class OutcomeTracker : MonoBehaviour
    {
        public static OutcomeTracker Instance { get; private set; }

        [SerializeField] private int observationWindowEvents = 5;

        private string _activeLogId;
        private string _activeTraceId;
        private string _expectedAction;
        private float _startRealtime;
        private float _timeoutSec;
        private int _eventCountAtStart;
        private string _stepIdAtStart;
        private Coroutine _watchRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void StartTracking(string logId, string traceId, string expectedAction, float timeoutSec)
        {
            if (string.IsNullOrEmpty(logId) && string.IsNullOrEmpty(traceId)) return;

            if (_watchRoutine != null) StopCoroutine(_watchRoutine);

            _activeLogId = logId;
            _activeTraceId = traceId;
            _expectedAction = expectedAction ?? "";
            _startRealtime = Time.realtimeSinceStartup;
            _timeoutSec = timeoutSec > 0 ? timeoutSec : 120f;
            _eventCountAtStart = InteractionEventLogger.Instance != null
                ? InteractionEventLogger.Instance.GetRecentEvents().Count
                : 0;
            _stepIdAtStart = StepProgressManager.Instance != null
                ? StepProgressManager.Instance.CurrentStepId
                : "unknown";

            _watchRoutine = StartCoroutine(Watch());
        }

        private IEnumerator Watch()
        {
            while (true)
            {
                float elapsed = Time.realtimeSinceStartup - _startRealtime;

                if (CheckResolved(out string finalAction))
                {
                    EmitOutcome(true, finalAction, "resolved");
                    yield break;
                }

                if (elapsed >= _timeoutSec)
                {
                    EmitOutcome(false, "", "timeout");
                    yield break;
                }

                yield return new WaitForSeconds(1f);
            }
        }

        private bool CheckResolved(out string finalAction)
        {
            finalAction = "";

            if (StepProgressManager.Instance != null
                && StepProgressManager.Instance.CurrentStepId != _stepIdAtStart)
            {
                finalAction = "step_advanced:" + StepProgressManager.Instance.CurrentStepId;
                return true;
            }

            if (InteractionEventLogger.Instance == null) return false;

            var events = InteractionEventLogger.Instance.GetRecentEvents();
            int newCount = events.Count - _eventCountAtStart;
            if (newCount <= 0) return false;

            int windowStart = Mathf.Max(0, events.Count - observationWindowEvents);
            for (int i = windowStart; i < events.Count; i++)
            {
                var ev = events[i];
                if (ev == null) continue;

                if (!string.IsNullOrEmpty(_expectedAction)
                    && ev.action_type != null
                    && ev.action_type.Equals(_expectedAction, System.StringComparison.OrdinalIgnoreCase))
                {
                    finalAction = ev.action_type;
                    return true;
                }

                if (ev.action_type == "step_completed")
                {
                    finalAction = ev.action_type;
                    return true;
                }
            }
            return false;
        }

        private void EmitOutcome(bool resolved, string finalAction, string status)
        {
            float elapsed = Time.realtimeSinceStartup - _startRealtime;

            int newEvents = 0;
            if (InteractionEventLogger.Instance != null)
            {
                newEvents = Mathf.Max(0, InteractionEventLogger.Instance.GetRecentEvents().Count - _eventCountAtStart);
            }

            string sessionId = LabSessionManager.Instance != null
                ? LabSessionManager.Instance.SessionId
                : "";

            var record = new OutcomeRecord
            {
                log_id = _activeLogId,
                trace_id = _activeTraceId,
                session_id = sessionId,
                step_id = _stepIdAtStart,
                resolved = resolved,
                steps_to_resolve = newEvents,
                time_to_resolve_sec = elapsed,
                final_action = finalAction ?? "",
                status = status
            };

            Debug.Log("[OutcomeTracker] " + JsonUtilityCompat(record));

            if (AssistGatewayClient.Instance != null)
            {
                StartCoroutine(AssistGatewayClient.Instance.PostOutcome(record));
            }

            _watchRoutine = null;
        }

        private static string JsonUtilityCompat(OutcomeRecord r)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(r);
        }
    }
}
