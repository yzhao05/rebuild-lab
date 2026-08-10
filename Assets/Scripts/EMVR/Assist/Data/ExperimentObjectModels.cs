using System;

namespace EMVR.Assist.Data
{
    [Serializable]
    public class ObjectSnapshot
    {
        public string object_id;
        public string name;
        public string object_type;
        public float[] position;
        public float[] rotation;
        public float charge_coulomb;
        public float current_ampere;
        public bool is_charged;
        public bool is_selected;
        public bool is_grabbable;
    }
}
