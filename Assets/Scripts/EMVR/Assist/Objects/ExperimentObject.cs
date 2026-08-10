using UnityEngine;
using EMVR.Assist.Data;

namespace EMVR.Assist.Objects
{
    public enum ExperimentObjectType
    {
        Charge,
        Wire,
        Coil,
        Plane,
        Sphere,
        ChargeZone,
        ObservationZone,
        FieldLine,
        UI,
        Other
    }

    public class ExperimentObject : MonoBehaviour
    {
        [Tooltip("唯一 ID，发送给 Dify。留空时使用 gameObject.name。")]
        public string objectId;

        [Tooltip("展示名（中文也可），便于 LLM 理解。")]
        public string displayName;

        public ExperimentObjectType objectType = ExperimentObjectType.Other;

        [Header("Physics State (optional — only fill what is relevant)")]
        public float chargeCoulomb;
        public float currentAmpere;
        public bool isCharged;
        public bool isSelected;
        public bool isGrabbable = true;

        [Tooltip("如果挂载在 ChargedObjects.Sphere/Wire/Plane 同一 GameObject 上，可启用以自动同步 electric_mag → chargeCoulomb。")]
        public bool autoSyncChargeFromChargedObject = true;

        private ChargedObjects.Sphere _sphere;
        private ChargedObjects.Wire _wire;
        private ChargedObjects.Plane _plane;

        private void Awake()
        {
            if (string.IsNullOrEmpty(objectId)) objectId = gameObject.name;
            if (string.IsNullOrEmpty(displayName)) displayName = gameObject.name;

            CacheChargedComponents();
        }

        private void OnEnable()
        {
            ExperimentObjectRegistry.Register(this);
        }

        private void OnDisable()
        {
            if (SelectionTracker.Instance != null && SelectionTracker.Instance.CurrentSelected == this)
            {
                SelectionTracker.Instance.ClearSelection();
            }

            ExperimentObjectRegistry.Unregister(this);
        }

        private void OnDestroy()
        {
            ExperimentObjectRegistry.Unregister(this);
        }

        public void Configure(string newObjectId, string newDisplayName, ExperimentObjectType newObjectType)
        {
            ExperimentObjectRegistry.Unregister(this);

            if (!string.IsNullOrWhiteSpace(newObjectId)) objectId = newObjectId;
            if (!string.IsNullOrWhiteSpace(newDisplayName)) displayName = newDisplayName;
            objectType = newObjectType;

            CacheChargedComponents();
            ExperimentObjectRegistry.Register(this);
        }

        private void CacheChargedComponents()
        {
            if (autoSyncChargeFromChargedObject)
            {
                _sphere = GetComponent<ChargedObjects.Sphere>();
                _wire = GetComponent<ChargedObjects.Wire>();
                _plane = GetComponent<ChargedObjects.Plane>();
            }
        }

        public void SetSelected(bool selected) => isSelected = selected;

        public ObjectSnapshot ToSnapshot()
        {
            SyncFromChargedComponents();

            Vector3 p = transform.position;
            Quaternion r = transform.rotation;

            return new ObjectSnapshot
            {
                object_id = objectId,
                name = displayName,
                object_type = objectType.ToString(),
                position = new[] { p.x, p.y, p.z },
                rotation = new[] { r.x, r.y, r.z, r.w },
                charge_coulomb = chargeCoulomb,
                current_ampere = currentAmpere,
                is_charged = isCharged,
                is_selected = isSelected,
                is_grabbable = isGrabbable
            };
        }

        private void SyncFromChargedComponents()
        {
            if (!autoSyncChargeFromChargedObject) return;

            if (_sphere != null)
            {
                chargeCoulomb = _sphere.electric_mag;
                isCharged = Mathf.Abs(_sphere.electric_mag) > 0.0001f;
            }
            else if (_wire != null)
            {
                chargeCoulomb = _wire.electric_mag;
                isCharged = Mathf.Abs(_wire.electric_mag) > 0.0001f;
            }
            else if (_plane != null)
            {
                chargeCoulomb = _plane.electric_mag;
                isCharged = Mathf.Abs(_plane.electric_mag) > 0.0001f;
            }
        }
    }
}
