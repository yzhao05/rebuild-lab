using System;

[Serializable]
public class AssistantRequest
{
    public string session_id;
    public string user_id;
    public string scene_id;
    public string mode;
    public string step_id;
    public string student_input_text;
    public string selected_object_json;
    public string objects_json;
    public string recent_actions_json;
    public string diagnostic_hints_json;
}