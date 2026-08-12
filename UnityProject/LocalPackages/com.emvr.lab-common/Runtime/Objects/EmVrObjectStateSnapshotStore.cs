using System;
using System.Collections.Generic;
using UnityEngine;

namespace EMVR.Common.Objects
{
    /// <summary>
    /// Captures and restores a deterministic JSON envelope for a group of
    /// contract-aware lab objects. Object IDs, rather than scene names or array
    /// positions, are the stable join key.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmVrObjectStateSnapshotStore : MonoBehaviour
    {
        const int CurrentSchemaVersion = 1;

        public EmVrLabObject[] explicitObjects = new EmVrLabObject[0];
        public bool autoDiscoverWhenEmpty = true;

        public string CurrentSnapshotJson { get; private set; }

        public bool TryCaptureSnapshot(out string snapshotJson, out string error)
        {
            Dictionary<string, EmVrLabObject> targets;
            if (!TryBuildTargetMap(out targets, out error))
            {
                snapshotJson = string.Empty;
                return false;
            }

            SnapshotEnvelope envelope = new SnapshotEnvelope
            {
                schema_version = CurrentSchemaVersion,
                objects = new List<SnapshotEntry>(targets.Count)
            };

            foreach (KeyValuePair<string, EmVrLabObject> target in targets)
            {
                envelope.objects.Add(new SnapshotEntry
                {
                    object_id = target.Key,
                    object_type = target.Value.ObjectType,
                    state_json = target.Value.ExportStateJson()
                });
            }

            envelope.objects.Sort((left, right) =>
                string.CompareOrdinal(left.object_id, right.object_id));
            snapshotJson = JsonUtility.ToJson(envelope);
            CurrentSnapshotJson = snapshotJson;
            error = string.Empty;
            return true;
        }

        public bool TryRestoreCurrentSnapshot(out string error)
        {
            return TryRestoreSnapshot(CurrentSnapshotJson, out error);
        }

        public bool TryRestoreSnapshot(string snapshotJson, out string error)
        {
            SnapshotEnvelope envelope;
            if (!TryParseSnapshot(snapshotJson, out envelope, out error))
            {
                return false;
            }

            Dictionary<string, EmVrLabObject> targets;
            if (!TryBuildTargetMap(out targets, out error))
            {
                return false;
            }

            if (envelope.objects.Count != targets.Count)
            {
                error = "Snapshot contains " + envelope.objects.Count
                    + " object(s), but the store has " + targets.Count + ".";
                return false;
            }

            Dictionary<string, string> rollbackStates = new Dictionary<string, string>();
            HashSet<string> snapshotIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < envelope.objects.Count; i++)
            {
                SnapshotEntry entry = envelope.objects[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.object_id))
                {
                    error = "Snapshot entry " + i + " has no object ID.";
                    return false;
                }

                if (!snapshotIds.Add(entry.object_id))
                {
                    error = "Snapshot contains duplicate object ID '" + entry.object_id + "'.";
                    return false;
                }

                EmVrLabObject target;
                if (!targets.TryGetValue(entry.object_id, out target))
                {
                    error = "Snapshot object '" + entry.object_id + "' is not registered in the store.";
                    return false;
                }

                if (!string.Equals(entry.object_type, target.ObjectType, StringComparison.Ordinal))
                {
                    error = "Snapshot type '" + entry.object_type + "' for '" + entry.object_id
                        + "' does not match target type '" + target.ObjectType + "'.";
                    return false;
                }

                rollbackStates.Add(entry.object_id, target.ExportStateJson());
            }

            for (int i = 0; i < envelope.objects.Count; i++)
            {
                SnapshotEntry entry = envelope.objects[i];
                EmVrLabObject target = targets[entry.object_id];
                string importError;
                if (target.TryImportStateJson(entry.state_json, out importError))
                {
                    continue;
                }

                foreach (KeyValuePair<string, string> rollback in rollbackStates)
                {
                    string ignored;
                    targets[rollback.Key].TryImportStateJson(rollback.Value, out ignored);
                }

                error = "Could not restore '" + entry.object_id + "': " + importError;
                return false;
            }

            CurrentSnapshotJson = snapshotJson;
            error = string.Empty;
            return true;
        }

        public bool TryResetObjects(out string error)
        {
            Dictionary<string, EmVrLabObject> targets;
            if (!TryBuildTargetMap(out targets, out error))
            {
                return false;
            }

            foreach (KeyValuePair<string, EmVrLabObject> target in targets)
            {
                string restoreError;
                if (!target.Value.TryRestoreInitialState(out restoreError))
                {
                    error = "Could not reset '" + target.Key + "': " + restoreError;
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public bool TryRoundTripJson(
            string snapshotJson,
            out string roundTrippedJson,
            out string error)
        {
            SnapshotEnvelope envelope;
            if (!TryParseSnapshot(snapshotJson, out envelope, out error))
            {
                roundTrippedJson = string.Empty;
                return false;
            }

            roundTrippedJson = JsonUtility.ToJson(envelope);
            error = string.Empty;
            return true;
        }

        bool TryBuildTargetMap(out Dictionary<string, EmVrLabObject> targets, out string error)
        {
            EmVrLabObject[] sources = explicitObjects;
            if ((sources == null || sources.Length == 0) && autoDiscoverWhenEmpty)
            {
                sources = FindObjectsOfType<EmVrLabObject>(true);
            }

            targets = new Dictionary<string, EmVrLabObject>(StringComparer.Ordinal);
            if (sources == null || sources.Length == 0)
            {
                error = "No lab objects are registered for state snapshots.";
                return false;
            }

            for (int i = 0; i < sources.Length; i++)
            {
                EmVrLabObject source = sources[i];
                if (source == null)
                {
                    error = "Registered lab object " + i + " is missing.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(source.ObjectId))
                {
                    error = "Lab object '" + source.name + "' has an empty object ID.";
                    return false;
                }

                if (targets.ContainsKey(source.ObjectId))
                {
                    error = "Duplicate lab object ID '" + source.ObjectId + "'.";
                    return false;
                }

                targets.Add(source.ObjectId, source);
            }

            error = string.Empty;
            return true;
        }

        static bool TryParseSnapshot(
            string snapshotJson,
            out SnapshotEnvelope envelope,
            out string error)
        {
            envelope = null;
            if (string.IsNullOrWhiteSpace(snapshotJson))
            {
                error = "Snapshot JSON is empty.";
                return false;
            }

            try
            {
                envelope = JsonUtility.FromJson<SnapshotEnvelope>(snapshotJson);
            }
            catch (Exception exception)
            {
                error = "Snapshot JSON is invalid: " + exception.Message;
                return false;
            }

            if (envelope == null || envelope.schema_version != CurrentSchemaVersion)
            {
                error = "Snapshot schema version is missing or unsupported.";
                return false;
            }

            if (envelope.objects == null || envelope.objects.Count == 0)
            {
                error = "Snapshot contains no object states.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        [Serializable]
        sealed class SnapshotEnvelope
        {
            public int schema_version;
            public List<SnapshotEntry> objects;
        }

        [Serializable]
        sealed class SnapshotEntry
        {
            public string object_id;
            public string object_type;
            public string state_json;
        }
    }
}
