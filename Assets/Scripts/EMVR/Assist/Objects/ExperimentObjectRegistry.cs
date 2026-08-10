using System.Collections.Generic;
using EMVR.Assist.Data;
using UnityEngine;

namespace EMVR.Assist.Objects
{
    public static class ExperimentObjectRegistry
    {
        private static readonly Dictionary<string, ExperimentObject> _byId
            = new Dictionary<string, ExperimentObject>();
        private static readonly List<ExperimentObject> _all
            = new List<ExperimentObject>();

        public static void Register(ExperimentObject obj)
        {
            if (obj == null) return;
            if (!_all.Contains(obj)) _all.Add(obj);
            if (!string.IsNullOrEmpty(obj.objectId)) _byId[obj.objectId] = obj;
        }

        public static void Unregister(ExperimentObject obj)
        {
            if (obj == null) return;
            _all.Remove(obj);
            if (!string.IsNullOrEmpty(obj.objectId) && _byId.TryGetValue(obj.objectId, out var existing) && existing == obj)
            {
                _byId.Remove(obj.objectId);
            }
        }

        public static ExperimentObject FindById(string objectId)
        {
            if (string.IsNullOrEmpty(objectId)) return null;
            _byId.TryGetValue(objectId, out var obj);
            return obj;
        }

        public static List<ExperimentObject> FindHighlightTargets(string target)
        {
            var results = new List<ExperimentObject>();
            if (string.IsNullOrWhiteSpace(target)) return results;

            string token = target.Trim();
            string normalized = Normalize(token);

            if (normalized == "selected"
                || normalized == "current_selected"
                || normalized == "current_object"
                || normalized == "selected_object")
            {
                AddIfActive(results, SelectionTracker.Instance != null ? SelectionTracker.Instance.CurrentSelected : null);
                return results;
            }

            if (normalized == "all" || normalized == "all_objects")
            {
                AddAllActive(results);
                return results;
            }

            if (normalized == "charges"
                || normalized == "charged_objects"
                || normalized == "charge_objects"
                || normalized == "all_charges")
            {
                AddByType(results, ExperimentObjectType.Sphere);
                AddByType(results, ExperimentObjectType.Wire);
                AddByType(results, ExperimentObjectType.Plane);
                return results;
            }

            AddIfActive(results, FindById(token));
            if (results.Count > 0) return results;

            ExperimentObjectType? aliasType = ResolveTypeAlias(normalized);
            if (aliasType.HasValue)
            {
                AddByType(results, aliasType.Value);
                return results;
            }

            for (int i = 0; i < _all.Count; i++)
            {
                var obj = _all[i];
                if (!IsActive(obj)) continue;

                if (MatchesExact(obj.objectId, token)
                    || MatchesExact(obj.displayName, token)
                    || MatchesExact(obj.objectType.ToString(), token)
                    || MatchesExact(obj.gameObject.name, token))
                {
                    AddIfMissing(results, obj);
                }
            }

            if (results.Count > 0) return results;

            for (int i = 0; i < _all.Count; i++)
            {
                var obj = _all[i];
                if (!IsActive(obj)) continue;

                if (ContainsToken(obj.objectId, token)
                    || ContainsToken(obj.displayName, token)
                    || ContainsToken(obj.gameObject.name, token))
                {
                    AddIfMissing(results, obj);
                }
            }

            return results;
        }

        public static List<ObjectSnapshot> GetAllSnapshots()
        {
            var list = new List<ObjectSnapshot>(_all.Count);
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i] == null) continue;
                if (!IsActive(_all[i])) continue;
                list.Add(_all[i].ToSnapshot());
            }
            return list;
        }

        public static int Count => _all.Count;

        public static void ClearAll()
        {
            _all.Clear();
            _byId.Clear();
        }

        private static void AddAllActive(List<ExperimentObject> results)
        {
            for (int i = 0; i < _all.Count; i++)
            {
                AddIfActive(results, _all[i]);
            }
        }

        private static void AddByType(List<ExperimentObject> results, ExperimentObjectType type)
        {
            for (int i = 0; i < _all.Count; i++)
            {
                var obj = _all[i];
                if (IsActive(obj) && obj.objectType == type)
                {
                    AddIfMissing(results, obj);
                }
            }
        }

        private static void AddIfActive(List<ExperimentObject> results, ExperimentObject obj)
        {
            if (!IsActive(obj)) return;
            AddIfMissing(results, obj);
        }

        private static void AddIfMissing(List<ExperimentObject> results, ExperimentObject obj)
        {
            if (obj == null || results.Contains(obj)) return;
            results.Add(obj);
        }

        private static bool IsActive(ExperimentObject obj)
        {
            return obj != null && obj.isActiveAndEnabled && obj.gameObject.activeInHierarchy;
        }

        private static bool MatchesExact(string value, string token)
        {
            return !string.IsNullOrEmpty(value)
                && value.Equals(token, System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsToken(string value, string token)
        {
            return !string.IsNullOrEmpty(value)
                && !string.IsNullOrEmpty(token)
                && value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        }

        private static ExperimentObjectType? ResolveTypeAlias(string normalized)
        {
            switch (normalized)
            {
                case "sphere":
                case "point_charge":
                case "charge":
                case "point":
                case "ball":
                case "点电荷":
                case "小球":
                case "球":
                    return ExperimentObjectType.Sphere;

                case "wire":
                case "line_charge":
                case "line":
                case "cylinder":
                case "线电荷":
                case "圆柱体":
                case "圆柱":
                    return ExperimentObjectType.Wire;

                case "plane":
                case "plane_charge":
                case "surface_charge":
                case "surface":
                case "面电荷":
                case "平面":
                    return ExperimentObjectType.Plane;

                default:
                    return null;
            }
        }
    }
}
