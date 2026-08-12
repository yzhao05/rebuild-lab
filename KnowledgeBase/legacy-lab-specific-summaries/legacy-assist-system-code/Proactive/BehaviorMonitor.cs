using System;
using System.Collections;
using System.Collections.Generic;
using EMVR.Assist.Core;
using EMVR.Assist.Data;
using EMVR.Assist.Logging;
using EMVR.Assist.Network;
using EMVR.Assist.Validation;
using UnityEngine;

namespace EMVR.Assist.Proactive
{
    public class BehaviorMonitor : MonoBehaviour
    {
        public static BehaviorMonitor Instance { get; private set; }

        [Header("Detection")]
        [SerializeField] private bool enableDetection = true;
        [SerializeField] private float checkIntervalSec = 1.67f;
        [SerializeField] private bool enableIdleTrigger = true;
        [SerializeField] private bool enableRepeatTrigger = true;
        [SerializeField] private bool enableDeviationTrigger = true;
        [SerializeField] private bool enableStepStuckTrigger = true;
        [SerializeField] private float idleThresholdSec = 16.67f;
        [SerializeField] private int repeatThreshold = 4;
        [SerializeField] private float minStepStuckSec = 30f;
        [SerializeField] private float stepStuckIdleMultiplier = 2f;

        [Header("Cooldown")]
        [SerializeField] private float cooldownSec = 20f;
        [SerializeField] private float sameSignalSuppressSec = 60f;

        private float _lastTriggerRealtime = -999f;
        private string _lastTriggerSignature = "";
        private string _lastTriggerStepId = "";
        private int _lastTriggerActionCount = -1;
        private float _stepStartedRealtime;
        private long _stepStartedUnixMs;
        private bool _subscribedToStepChanges;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _stepStartedRealtime = Time.realtimeSinceStartup;
            _stepStartedUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private void OnEnable()
        {
            TrySubscribeToStepChanges();
        }

        private void OnDisable()
        {
            if (_subscribedToStepChanges && StepProgressManager.Instance != null)
            {
                StepProgressManager.Instance.OnStepChanged -= HandleStepChanged;
            }
            _subscribedToStepChanges = false;
        }

        private IEnumerator Start()
        {
            TrySubscribeToStepChanges();

            while (true)
            {
                yield return new WaitForSeconds(checkIntervalSec);
                if (enableDetection) Tick();
            }
        }

        private void Tick()
        {
            if (AssistGatewayClient.Instance == null) return;
            if (AssistGatewayClient.Instance.IsRequesting) return;

            ProactiveContext context = BuildContext();
            TriggerReason reason = EvaluateTrigger(context);
            if (reason == null) return;

            string signature = BuildTriggerSignature(reason);
            if (ShouldSuppress(signature, context)) return;

            _lastTriggerRealtime = Time.realtimeSinceStartup;
            _lastTriggerSignature = signature;
            _lastTriggerStepId = context.stepId;
            _lastTriggerActionCount = context.recentActionCount;

            Debug.Log($"[BehaviorMonitor] proactive trigger={reason.trigger_type} step={reason.step_id} idle={reason.idle_duration_sec:F1} stepElapsed={reason.step_elapsed_sec:F1} repeat={reason.repeat_count} deviation={reason.sequence_deviation} detail={reason.detail}");
            StartCoroutine(AssistGatewayClient.Instance.CallProactiveAssist(reason));
        }

        private ProactiveContext BuildContext()
        {
            var context = new ProactiveContext
            {
                stepId = StepProgressManager.Instance != null ? StepProgressManager.Instance.CurrentStepId : "unknown",
                expectedAction = StepProgressManager.Instance != null ? StepProgressManager.Instance.ExpectedAction : "",
                idleSec = ComputeIdleSec(),
                stepElapsedSec = Mathf.Max(0f, Time.realtimeSinceStartup - _stepStartedRealtime),
                stepIdleThresholdSec = ResolveIdleThreshold(),
                recentEvents = InteractionEventLogger.Instance != null
                    ? InteractionEventLogger.Instance.GetRecentEvents()
                    : new List<InteractionEvent>()
            };

            context.recentActionCount = context.recentEvents.Count;
            context.lastAction = FindLastMeaningfulAction(context.recentEvents);
            context.currentStepActionCount = CountEventsInStep(context.recentEvents, context.stepId, _stepStartedUnixMs);
            context.trailingRepeatCount = CountTrailingMeaningfulRepeats(context.recentEvents, out context.repeatedAction);

            if (LocalValidationManager.Instance != null)
            {
                var hints = LocalValidationManager.Instance.BuildHints();
                context.sequenceDeviation = hints != null && hints.sequence_deviation;
            }

            return context;
        }

        private TriggerReason EvaluateTrigger(ProactiveContext context)
        {
            if (enableDeviationTrigger && context.sequenceDeviation)
            {
                return BuildReason(context, "deviation", "Local validation detected a likely sequence or attention error.");
            }

            if (enableRepeatTrigger && context.trailingRepeatCount >= repeatThreshold)
            {
                return BuildReason(context, "repeat", "The same meaningful action was repeated several times without progress.");
            }

            float stuckThreshold = Mathf.Max(minStepStuckSec, context.stepIdleThresholdSec * Mathf.Max(1f, stepStuckIdleMultiplier));
            bool hasActionsInStep = context.currentStepActionCount > 0;
            bool idleAlreadyExplainsIt = context.idleSec >= context.stepIdleThresholdSec;
            if (enableStepStuckTrigger
                && hasActionsInStep
                && context.stepElapsedSec >= stuckThreshold
                && !idleAlreadyExplainsIt)
            {
                return BuildReason(context, "step_stuck", "The learner has stayed on the same step for a long time despite interacting.");
            }

            if (enableIdleTrigger && context.idleSec >= context.stepIdleThresholdSec)
            {
                return BuildReason(context, "idle", "No meaningful learner action was recorded for longer than the current step allows.");
            }

            return null;
        }

        private TriggerReason BuildReason(ProactiveContext context, string triggerType, string detail)
        {
            InteractionEvent last = context.lastAction;
            return new TriggerReason
            {
                trigger_type = triggerType,
                step_id = context.stepId,
                expected_action = context.expectedAction,
                last_action = last != null ? last.action_type : "",
                object_id = last != null ? last.object_id : "",
                idle_duration_sec = context.idleSec,
                step_elapsed_sec = context.stepElapsedSec,
                no_progress_duration_sec = context.stepElapsedSec,
                repeat_count = context.trailingRepeatCount,
                repeated_action = context.repeatedAction,
                recent_action_count = context.recentActionCount,
                sequence_deviation = context.sequenceDeviation,
                detail = detail
            };
        }

        private bool ShouldSuppress(string signature, ProactiveContext context)
        {
            float sinceLast = Time.realtimeSinceStartup - _lastTriggerRealtime;
            if (sinceLast < cooldownSec) return true;

            bool sameSignal = signature == _lastTriggerSignature
                && context.stepId == _lastTriggerStepId
                && context.recentActionCount == _lastTriggerActionCount;

            return sameSignal && sinceLast < sameSignalSuppressSec;
        }

        private string BuildTriggerSignature(TriggerReason reason)
        {
            return reason.trigger_type + ":"
                + reason.step_id + ":"
                + reason.repeated_action + ":"
                + reason.object_id;
        }

        private float ResolveIdleThreshold()
        {
            LabStep currentStep = StepProgressManager.Instance != null
                ? StepProgressManager.Instance.CurrentStep
                : null;

            if (currentStep != null && currentStep.allowedIdleSec > 0f)
            {
                return currentStep.allowedIdleSec;
            }

            return idleThresholdSec;
        }

        private void HandleStepChanged(string stepId)
        {
            _stepStartedRealtime = Time.realtimeSinceStartup;
            _stepStartedUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private void TrySubscribeToStepChanges()
        {
            if (_subscribedToStepChanges || StepProgressManager.Instance == null) return;
            StepProgressManager.Instance.OnStepChanged += HandleStepChanged;
            _subscribedToStepChanges = true;
        }

        private static float ComputeIdleSec()
        {
            if (InteractionEventLogger.Instance == null) return 0f;
            long lastMs = InteractionEventLogger.Instance.LastActionUnixMs;
            if (lastMs <= 0) return 0f;
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return Mathf.Max(0f, (nowMs - lastMs) / 1000f);
        }

        private static InteractionEvent FindLastMeaningfulAction(List<InteractionEvent> events)
        {
            if (events == null) return null;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                if (IsMeaningfulForProactive(events[i])) return events[i];
            }
            return null;
        }

        private static int CountEventsInStep(List<InteractionEvent> events, string stepId, long sinceUnixMs)
        {
            if (events == null || string.IsNullOrEmpty(stepId)) return 0;

            int count = 0;
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] == null) continue;
                if (events[i].timestamp_ms >= sinceUnixMs
                    && events[i].step_id == stepId
                    && IsMeaningfulForProactive(events[i]))
                {
                    count++;
                }
            }
            return count;
        }

        private static int CountTrailingMeaningfulRepeats(List<InteractionEvent> events, out string repeatedAction)
        {
            repeatedAction = "";
            if (events == null || events.Count == 0) return 0;

            InteractionEvent last = FindLastMeaningfulAction(events);
            if (last == null) return 0;

            repeatedAction = BuildRepeatSignature(last);
            int count = 0;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                InteractionEvent current = events[i];
                if (!IsMeaningfulForProactive(current)) continue;

                if (BuildRepeatSignature(current) == repeatedAction) count++;
                else break;
            }

            return count;
        }

        private static string BuildRepeatSignature(InteractionEvent action)
        {
            if (action == null) return "";
            return action.action_type + ":" + action.object_id;
        }

        private static bool IsMeaningfulForProactive(InteractionEvent action)
        {
            if (action == null || string.IsNullOrEmpty(action.action_type)) return false;

            switch (action.action_type)
            {
                case "ask_for_help":
                case "point_object":
                case "release_object":
                    return false;
                default:
                    return true;
            }
        }

        private class ProactiveContext
        {
            public string stepId;
            public string expectedAction;
            public float idleSec;
            public float stepElapsedSec;
            public float stepIdleThresholdSec;
            public int currentStepActionCount;
            public int trailingRepeatCount;
            public string repeatedAction;
            public bool sequenceDeviation;
            public int recentActionCount;
            public InteractionEvent lastAction;
            public List<InteractionEvent> recentEvents;
        }
    }
}
