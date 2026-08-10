using System;

namespace EMVR.Assist.Data
{
    [Serializable]
    public class DifyWorkflowResponse
    {
        public UnityAssistResponse unity_output;
    }

    [Serializable]
    public class UnityAssistResponse
    {
        public string action_type;
        public string session_id;
        public string trace_id;
        public string user_id;
        public string lab_id;
        public string step_id;
        public DisplayPayload display;
        public ScenePayload scene;
        public MetaPayload meta;
        public CallbackPayload callback;
    }

    [Serializable]
    public class DisplayPayload
    {
        public string voice_text;
        public string subtitle_text;
        public float display_duration_sec;
    }

    [Serializable]
    public class ScenePayload
    {
        public string[] highlight_objects;
        public string highlight_color;
        public float highlight_duration_sec;
    }

    [Serializable]
    public class MetaPayload
    {
        public string mode;
        public string intent_type;
        public string intervention_type;
        public int scaffolding_level;
        public string error_type;
        public string fallback_reason;
        public string no_action_reason;
        public string expected_next_action;
        public float confidence;
        public string timestamp;
    }

    [Serializable]
    public class CallbackPayload
    {
        public string endpoint;
        public string log_id;
        public string trace_id;
        public float timeout_sec;
    }
}
