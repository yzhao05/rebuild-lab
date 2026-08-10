using System;
using System.Collections.Generic;

namespace EMVR.Assist.Data
{
    [Serializable]
    public class RuleResult
    {
        public string rule_id;
        public bool passed;
        public string severity;
        public string message;
        public string[] related_objects;
    }

    [Serializable]
    public class DiagnosticHints
    {
        public string current_step;
        public string expected_action;
        public List<RuleResult> rule_results;
        public bool sequence_deviation;
        public string local_error_candidate;
    }

    [Serializable]
    public class TriggerReason
    {
        public string trigger_type;
        public string step_id;
        public string expected_action;
        public string last_action;
        public string object_id;
        public float idle_duration_sec;
        public float step_elapsed_sec;
        public float no_progress_duration_sec;
        public int repeat_count;
        public string repeated_action;
        public int recent_action_count;
        public bool sequence_deviation;
        public string detail;
    }

    [Serializable]
    public class OutcomeRecord
    {
        public string log_id;
        public string trace_id;
        public string session_id;
        public string step_id;
        public bool resolved;
        public int steps_to_resolve;
        public float time_to_resolve_sec;
        public string final_action;
        public string status;
    }
}
