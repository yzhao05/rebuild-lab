using System;

namespace EMVR.Assist.Data
{
    [Serializable]
    public class AssistPayload
    {
        public string session_id;
        public string user_id;
        public string lab_id;
        public string scene_id;
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
    public class DifyDirectRequest
    {
        public AssistPayload inputs;
        public string response_mode;
        public string user;
    }
}
