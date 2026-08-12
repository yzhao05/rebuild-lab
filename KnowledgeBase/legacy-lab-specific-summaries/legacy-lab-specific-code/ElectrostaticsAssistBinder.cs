using UnityEngine;
using UnityEngine.SceneManagement;

namespace EMVR.Assist.Objects
{
    public static class ElectrostaticsAssistBinder
    {
        public static ExperimentObject EnsureLab1Object(
            GameObject target,
            ExperimentObjectType objectType,
            string displayName)
        {
            if (target == null) return null;
            if (!IsLab1ElectrostaticsScene()) return null;

            var experimentObject = target.GetComponent<ExperimentObject>();
            if (experimentObject == null)
            {
                experimentObject = target.AddComponent<ExperimentObject>();
            }

            string objectId = BuildRuntimeObjectId(target, objectType);
            experimentObject.Configure(objectId, displayName, objectType);

            if (target.GetComponent<AssistSelectionReporter>() == null)
            {
                target.AddComponent<AssistSelectionReporter>();
            }

            return experimentObject;
        }

        private static string BuildRuntimeObjectId(GameObject target, ExperimentObjectType objectType)
        {
            string typePart = objectType.ToString().ToLowerInvariant();
            return "lab1_" + typePart + "_" + Mathf.Abs(target.GetInstanceID());
        }

        private static bool IsLab1ElectrostaticsScene()
        {
            return SceneManager.GetActiveScene().name == "Static_Electric_Field";
        }
    }
}
