using System.Collections.Generic;
using EMVR.Common.Interaction;
using EMVR.Common.Objects;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace EMVR.Common.Editor.Validation
{
    /// <summary>
    /// Read-only validation for scene problems repeatedly observed in EMVR labs.
    /// </summary>
    public static class EmVrSceneValidator
    {
        const string MenuPath = "Tools/EMVR/Common/Validate Active Scene";

        [MenuItem(MenuPath)]
        public static void ValidateActiveSceneMenu()
        {
            List<ValidationIssue> issues = ValidateActiveScene();
            if (issues.Count == 0)
            {
                Debug.Log("[EMVR Common] Active scene validation passed.");
                return;
            }

            int errors = 0;
            int warnings = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                ValidationIssue issue = issues[i];
                if (issue.isError)
                {
                    errors++;
                    Debug.LogError("[EMVR Common] " + issue.message, issue.context);
                }
                else
                {
                    warnings++;
                    Debug.LogWarning("[EMVR Common] " + issue.message, issue.context);
                }
            }

            Debug.Log("[EMVR Common] Validation finished: " + errors + " error(s), " + warnings + " warning(s).");
        }

        public static List<ValidationIssue> ValidateActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            List<ValidationIssue> issues = new List<ValidationIssue>();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                issues.Add(new ValidationIssue(true, "There is no loaded active scene.", null));
                return issues;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            List<AudioListener> listeners = CollectActive<AudioListener>(roots);
            List<EventSystem> eventSystems = CollectActive<EventSystem>(roots);
            List<EmVrLabObject> labObjects = Collect<EmVrLabObject>(roots);
            List<EmVrPlanarMouseDraggable> draggables = Collect<EmVrPlanarMouseDraggable>(roots);

            if (listeners.Count != 1)
            {
                issues.Add(new ValidationIssue(
                    true,
                    "Expected exactly one AudioListener in the active scene, found " + listeners.Count + ".",
                    listeners.Count > 0 ? listeners[0] : null));
            }

            if (eventSystems.Count > 1)
            {
                issues.Add(new ValidationIssue(
                    true,
                    "Expected at most one EventSystem in the active scene, found " + eventSystems.Count + ".",
                    eventSystems[0]));
            }

            Dictionary<string, EmVrLabObject> objectsById = new Dictionary<string, EmVrLabObject>();
            for (int i = 0; i < labObjects.Count; i++)
            {
                EmVrLabObject labObject = labObjects[i];
                if (string.IsNullOrWhiteSpace(labObject.ObjectId))
                {
                    issues.Add(new ValidationIssue(true, "Lab object has an empty object ID.", labObject));
                    continue;
                }

                EmVrLabObject existing;
                if (objectsById.TryGetValue(labObject.ObjectId, out existing))
                {
                    issues.Add(new ValidationIssue(
                        true,
                        "Duplicate lab object ID '" + labObject.ObjectId + "'.",
                        labObject));
                }
                else
                {
                    objectsById.Add(labObject.ObjectId, labObject);
                }
            }

            for (int i = 0; i < draggables.Count; i++)
            {
                EmVrPlanarMouseDraggable draggable = draggables[i];
                if (draggable.GetComponentInChildren<Collider>(true) == null
                    && draggable.GetComponentInParent<Collider>() == null)
                {
                    issues.Add(new ValidationIssue(
                        true,
                        "Draggable object '" + draggable.name + "' has no collider in its hierarchy.",
                        draggable));
                }

                if (draggable.GetComponent<EmVrLabObject>() == null)
                {
                    issues.Add(new ValidationIssue(
                        false,
                        "Draggable object '" + draggable.name + "' has no EmVrLabObject identity component.",
                        draggable));
                }
            }

            return issues;
        }

        static List<T> Collect<T>(GameObject[] roots) where T : Component
        {
            List<T> results = new List<T>();
            for (int i = 0; i < roots.Length; i++)
            {
                results.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return results;
        }

        static List<T> CollectActive<T>(GameObject[] roots) where T : Behaviour
        {
            List<T> all = Collect<T>(roots);
            List<T> active = new List<T>();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].isActiveAndEnabled)
                {
                    active.Add(all[i]);
                }
            }

            return active;
        }

        public sealed class ValidationIssue
        {
            public readonly bool isError;
            public readonly string message;
            public readonly Object context;

            public ValidationIssue(bool isError, string message, Object context)
            {
                this.isError = isError;
                this.message = message;
                this.context = context;
            }
        }
    }
}
