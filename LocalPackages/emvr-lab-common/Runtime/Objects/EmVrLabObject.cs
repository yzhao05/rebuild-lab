using System;
using EMVR.Common.Contracts;
using UnityEngine;

namespace EMVR.Common.Objects
{
    /// <summary>
    /// Stable identity and default transform-state export for any contract-aware
    /// object in a lab scene. Domain components can subclass and override the
    /// JSON export when they need additional physical state.
    /// </summary>
    [DisallowMultipleComponent]
    public class EmVrLabObject : MonoBehaviour, IRestorableLabObjectStateProvider
    {
        [SerializeField] string objectId;
        [SerializeField] string objectType = "GenericLabObject";
        [SerializeField] string displayName;

        string initialStateJson;

        public string ObjectId
        {
            get { return objectId; }
        }

        public string ObjectType
        {
            get { return objectType; }
        }

        public string DisplayName
        {
            get { return string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName; }
        }

        public void ConfigureIdentity(string newObjectId, string newObjectType, string newDisplayName)
        {
            objectId = NormalizeIdentifier(newObjectId);
            objectType = string.IsNullOrWhiteSpace(newObjectType) ? "GenericLabObject" : newObjectType.Trim();
            displayName = string.IsNullOrWhiteSpace(newDisplayName) ? gameObject.name : newDisplayName.Trim();
        }

        public virtual string ExportStateJson()
        {
            TransformState state = new TransformState
            {
                object_id = ObjectId,
                object_type = ObjectType,
                display_name = DisplayName,
                active = gameObject.activeSelf,
                position = transform.position,
                rotation_euler = transform.eulerAngles,
                local_scale = transform.localScale
            };

            return JsonUtility.ToJson(state);
        }

        public virtual bool TryImportStateJson(string stateJson, out string error)
        {
            if (string.IsNullOrWhiteSpace(stateJson))
            {
                error = "State JSON is empty.";
                return false;
            }

            TransformState state;
            try
            {
                state = JsonUtility.FromJson<TransformState>(stateJson);
            }
            catch (Exception exception)
            {
                error = "State JSON is invalid: " + exception.Message;
                return false;
            }

            if (state == null)
            {
                error = "State JSON did not contain an object.";
                return false;
            }

            if (!string.Equals(state.object_id, ObjectId, StringComparison.Ordinal))
            {
                error = "State object ID '" + state.object_id
                    + "' does not match target '" + ObjectId + "'.";
                return false;
            }

            if (!string.Equals(state.object_type, ObjectType, StringComparison.Ordinal))
            {
                error = "State object type '" + state.object_type
                    + "' does not match target type '" + ObjectType + "'.";
                return false;
            }

            transform.position = state.position;
            transform.rotation = Quaternion.Euler(state.rotation_euler);
            transform.localScale = state.local_scale;
            gameObject.SetActive(state.active);
            error = string.Empty;
            return true;
        }

        public void CaptureInitialState()
        {
            initialStateJson = ExportStateJson();
        }

        public bool TryRestoreInitialState(out string error)
        {
            if (string.IsNullOrWhiteSpace(initialStateJson))
            {
                error = "Initial state has not been captured for '" + ObjectId + "'.";
                return false;
            }

            return TryImportStateJson(initialStateJson, out error);
        }

        protected virtual void Awake()
        {
            CaptureInitialState();
        }

        protected virtual void Reset()
        {
            objectId = NormalizeIdentifier(gameObject.name);
            objectType = "GenericLabObject";
            displayName = gameObject.name;
        }

        protected virtual void OnValidate()
        {
            objectId = NormalizeIdentifier(objectId);
            objectType = string.IsNullOrWhiteSpace(objectType) ? "GenericLabObject" : objectType.Trim();
            displayName = displayName == null ? string.Empty : displayName.Trim();
        }

        static string NormalizeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string normalized = value.Trim().ToLowerInvariant().Replace(' ', '_');
            return normalized.Replace('-', '_');
        }

        [Serializable]
        sealed class TransformState
        {
            public string object_id;
            public string object_type;
            public string display_name;
            public bool active;
            public Vector3 position;
            public Vector3 rotation_euler;
            public Vector3 local_scale;
        }
    }
}
