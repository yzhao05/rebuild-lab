using System;

namespace EMVR.Assist.Data
{
    [Serializable]
    public class InteractionEvent
    {
        public string event_id;
        public long timestamp_ms;
        public string step_id;
        public string action_type;
        public string object_id;
        public string object_type;
        public string result;
        public float[] position;
    }
}
