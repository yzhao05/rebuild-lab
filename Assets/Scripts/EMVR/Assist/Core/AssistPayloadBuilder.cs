using EMVR.Assist.Data;
using EMVR.Assist.Logging;
using EMVR.Assist.Objects;
using EMVR.Assist.Validation;
using Newtonsoft.Json;
using UnityEngine;

namespace EMVR.Assist.Core
{
    public class AssistPayloadBuilder : MonoBehaviour
    {
        public static AssistPayloadBuilder Instance { get; private set; }

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public AssistPayload BuildReactivePayload(string studentInputText)
        {
            return BuildBase("reactive", studentInputText, null);
        }

        public AssistPayload BuildProactivePayload(TriggerReason triggerReason)
        {
            return BuildBase("proactive", "", triggerReason);
        }

        public AssistPayload BuildEvalPayload(string studentInputText)
        {
            return BuildBase("eval", studentInputText, null);
        }

        private AssistPayload BuildBase(string mode, string studentInputText, TriggerReason triggerReason)
        {
            string sessionId = LabSessionManager.Instance != null
                ? LabSessionManager.Instance.SessionId
                : System.Guid.NewGuid().ToString();
            string userId = LabSessionManager.Instance != null
                ? LabSessionManager.Instance.UserId
                : "local_user_001";
            string labId = LabSessionManager.Instance != null
                ? LabSessionManager.Instance.LabId
                : "electrostatics";
            string sceneId = LabSessionManager.Instance != null
                ? LabSessionManager.Instance.SceneId
                : UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string stepId = StepProgressManager.Instance != null
                ? StepProgressManager.Instance.CurrentStepId
                : "unknown";

            return new AssistPayload
            {
                session_id = sessionId,
                user_id = userId,
                lab_id = labId,
                scene_id = sceneId,
                step_id = stepId,
                mode = mode,
                student_input_text = studentInputText ?? "",
                selected_object_json = BuildSelectedObjectJson(),
                objects_json = BuildObjectsJson(),
                recent_actions_json = BuildRecentActionsJson(),
                diagnostic_hints_json = BuildDiagnosticHintsJson(),
                trigger_reason_json = triggerReason == null
                    ? "{}"
                    : JsonConvert.SerializeObject(triggerReason, JsonSettings),
                step_rubric_json = "{}",
                session_memory_json = "{}"
            };
        }

        private static string BuildSelectedObjectJson()
        {
            if (SelectionTracker.Instance == null) return "{}";
            var snap = SelectionTracker.Instance.GetSelectedSnapshot();
            return snap == null ? "{}" : JsonConvert.SerializeObject(snap, JsonSettings);
        }

        private static string BuildObjectsJson()
        {
            if (ExperimentObjectRegistry.Count == 0) return "[]";
            var list = ExperimentObjectRegistry.GetAllSnapshots();
            return JsonConvert.SerializeObject(list, JsonSettings);
        }

        private static string BuildRecentActionsJson()
        {
            if (InteractionEventLogger.Instance == null) return "[]";
            var events = InteractionEventLogger.Instance.GetRecentEvents();
            if (events == null || events.Count == 0) return "[]";
            return JsonConvert.SerializeObject(events, JsonSettings);
        }

        private static string BuildDiagnosticHintsJson()
        {
            if (LocalValidationManager.Instance == null) return "{}";
            var hints = LocalValidationManager.Instance.BuildHints();
            return hints == null ? "{}" : JsonConvert.SerializeObject(hints, JsonSettings);
        }
    }
}
