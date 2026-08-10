using System;
using System.Collections.Generic;
using EMVR.Common.Contracts;
using EMVR.Common.Telemetry;
using UnityEngine;

namespace EMVR.Common.Assist
{
    /// <summary>
    /// Builds the common assist payload required by EMVR lab contracts. It does
    /// not perform networking; a gateway client can send the returned JSON.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrAssistGroundingProvider : MonoBehaviour, IAssistGroundingProvider
    {
        [Header("Session context")]
        public string sessionId;
        public string userId;
        public string labId;
        public string stepId;
        public string mode = "student_requested";

        [Header("Dynamic grounding")]
        [TextArea] public string studentInputText;
        [TextArea] public string diagnosticHintsJson = "{}";
        [TextArea] public string triggerReasonJson = "{}";
        [TextArea] public string stepRubricJson = "{}";
        [TextArea] public string sessionMemoryJson = "{}";

        [Header("Sources")]
        [Tooltip("Optional explicit providers. Every entry must implement ILabObjectStateProvider.")]
        public MonoBehaviour[] explicitObjectSources;
        public bool discoverObjectSourcesInChildren = true;
        public EmVrTelemetryEmitter telemetryEmitter;
        [Tooltip("Optional MonoBehaviour implementing ILabObjectStateProvider.")]
        public MonoBehaviour selectedObjectSource;

        public void SetSessionContext(
            string newSessionId,
            string newUserId,
            string newLabId,
            string newStepId,
            string newMode)
        {
            sessionId = newSessionId ?? string.Empty;
            userId = newUserId ?? string.Empty;
            labId = newLabId ?? string.Empty;
            stepId = newStepId ?? string.Empty;
            mode = string.IsNullOrWhiteSpace(newMode) ? "student_requested" : newMode;
        }

        public void SetSelectedObject(MonoBehaviour value)
        {
            if (value != null && !(value is ILabObjectStateProvider))
            {
                Debug.LogError("Selected object must implement ILabObjectStateProvider.", value);
                return;
            }

            selectedObjectSource = value;
        }

        public string BuildAssistGroundingJson()
        {
            List<ILabObjectStateProvider> objectProviders = CollectObjectProviders();
            ObjectStateCollection objectCollection = new ObjectStateCollection();
            for (int i = 0; i < objectProviders.Count; i++)
            {
                ILabObjectStateProvider provider = objectProviders[i];
                objectCollection.objects.Add(new ObjectStateRecord
                {
                    object_id = provider.ObjectId,
                    object_type = provider.ObjectType,
                    display_name = provider.DisplayName,
                    state_json = SafeExportState(provider)
                });
            }

            ILabObjectStateProvider selectedProvider = selectedObjectSource as ILabObjectStateProvider;
            AssistGroundingPayload payload = new AssistGroundingPayload
            {
                session_id = sessionId ?? string.Empty,
                user_id = userId ?? string.Empty,
                lab_id = labId ?? string.Empty,
                step_id = stepId ?? string.Empty,
                mode = mode ?? string.Empty,
                student_input_text = studentInputText ?? string.Empty,
                selected_object_json = selectedProvider != null ? SafeExportState(selectedProvider) : "{}",
                objects_json = JsonUtility.ToJson(objectCollection),
                recent_actions_json = telemetryEmitter != null
                    ? telemetryEmitter.ExportRecentEventsJson()
                    : "{\"events\":[]}",
                diagnostic_hints_json = NormalizeJson(diagnosticHintsJson),
                trigger_reason_json = NormalizeJson(triggerReasonJson),
                step_rubric_json = NormalizeJson(stepRubricJson),
                session_memory_json = NormalizeJson(sessionMemoryJson)
            };

            return JsonUtility.ToJson(payload);
        }

        List<ILabObjectStateProvider> CollectObjectProviders()
        {
            List<ILabObjectStateProvider> providers = new List<ILabObjectStateProvider>();
            HashSet<ILabObjectStateProvider> unique = new HashSet<ILabObjectStateProvider>();

            if (explicitObjectSources != null)
            {
                for (int i = 0; i < explicitObjectSources.Length; i++)
                {
                    AddProvider(explicitObjectSources[i], providers, unique);
                }
            }

            if (discoverObjectSourcesInChildren)
            {
                MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    AddProvider(behaviours[i], providers, unique);
                }
            }

            return providers;
        }

        void AddProvider(
            MonoBehaviour source,
            List<ILabObjectStateProvider> providers,
            HashSet<ILabObjectStateProvider> unique)
        {
            if (source == null)
            {
                return;
            }

            ILabObjectStateProvider provider = source as ILabObjectStateProvider;
            if (provider != null && unique.Add(provider))
            {
                providers.Add(provider);
            }
        }

        string SafeExportState(ILabObjectStateProvider provider)
        {
            try
            {
                return NormalizeJson(provider.ExportStateJson());
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Failed to export state for lab object '" + provider.ObjectId + "': " + exception.Message,
                    this);
                return "{}";
            }
        }

        static string NormalizeJson(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "{}" : value.Trim();
        }

        [Serializable]
        sealed class AssistGroundingPayload
        {
            public string session_id;
            public string user_id;
            public string lab_id;
            public string step_id;
            public string mode;
            public string student_input_text;
            public string selected_object_json;
            public string objects_json;
            public string recent_actions_json;
            public string diagnostic_hints_json;
            public string trigger_reason_json;
            public string step_rubric_json;
            public string session_memory_json;
        }

        [Serializable]
        sealed class ObjectStateRecord
        {
            public string object_id;
            public string object_type;
            public string display_name;
            public string state_json;
        }

        [Serializable]
        sealed class ObjectStateCollection
        {
            public List<ObjectStateRecord> objects = new List<ObjectStateRecord>();
        }
    }
}
