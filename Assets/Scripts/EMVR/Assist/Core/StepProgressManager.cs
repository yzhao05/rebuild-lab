using System;
using System.Collections.Generic;
using EMVR.Assist.Data;
using EMVR.Assist.Logging;
using UnityEngine;

namespace EMVR.Assist.Core
{
    [Serializable]
    public class LabStep
    {
        public string stepId;
        public string description;
        public string expectedAction;
        public string[] expectedObjectIds;
        public float allowedIdleSec = 60f;
    }

    public class StepProgressManager : MonoBehaviour
    {
        public static StepProgressManager Instance { get; private set; }

        [SerializeField] private string currentStepId = "step_1";
        [SerializeField] private List<LabStep> steps = new List<LabStep>();
        [SerializeField] private bool useDefaultLab1Steps = true;
        [SerializeField] private bool autoAdvanceFromActions = true;

        public string CurrentStepId
            => string.IsNullOrWhiteSpace(currentStepId) ? "unknown" : currentStepId;

        public LabStep CurrentStep
        {
            get
            {
                if (steps == null) return null;
                for (int i = 0; i < steps.Count; i++)
                {
                    if (steps[i] != null && steps[i].stepId == currentStepId) return steps[i];
                }
                return null;
            }
        }

        public string ExpectedAction
            => CurrentStep != null ? CurrentStep.expectedAction : "";

        public int StepCount => steps == null ? 0 : steps.Count;

        public int CurrentStepIndex
        {
            get
            {
                if (steps == null) return -1;
                for (int i = 0; i < steps.Count; i++)
                {
                    if (steps[i] != null && steps[i].stepId == currentStepId) return i;
                }
                return -1;
            }
        }

        public event Action<string> OnStepChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureDefaultSteps();
        }

        private void Start()
        {
            if (InteractionEventLogger.Instance != null)
            {
                InteractionEventLogger.Instance.OnActionLogged += HandleActionLogged;
            }
        }

        private void OnDestroy()
        {
            if (InteractionEventLogger.Instance != null)
            {
                InteractionEventLogger.Instance.OnActionLogged -= HandleActionLogged;
            }
        }

        public LabStep GetStepAt(int index)
        {
            if (steps == null || index < 0 || index >= steps.Count) return null;
            return steps[index];
        }

        public void SetStepByIndex(int index)
        {
            LabStep step = GetStepAt(index);
            if (step == null) return;
            SetCurrentStep(step.stepId);
        }

        public void SetCurrentStep(string stepId)
        {
            if (string.IsNullOrWhiteSpace(stepId)) return;
            if (currentStepId == stepId) return;

            currentStepId = stepId;
            Debug.Log("[StepProgressManager] step -> " + currentStepId);
            OnStepChanged?.Invoke(currentStepId);
        }

        public void AdvanceStep()
        {
            if (steps == null || steps.Count == 0) return;

            int idx = -1;
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] != null && steps[i].stepId == currentStepId)
                {
                    idx = i;
                    break;
                }
            }

            int nextIdx = Mathf.Min(idx + 1, steps.Count - 1);
            if (nextIdx < 0 || steps[nextIdx] == null) return;
            SetCurrentStep(steps[nextIdx].stepId);
        }

        private void EnsureDefaultSteps()
        {
            if (!useDefaultLab1Steps) return;

            if (steps == null) steps = new List<LabStep>();
            if (steps.Count == 0)
            {
                steps.Add(new LabStep
                {
                    stepId = "step_1",
                    description = "Choose a point, line, or plane charge from the source area on the right.",
                    expectedAction = "grab_object",
                    allowedIdleSec = 20f
                });
                steps.Add(new LabStep
                {
                    stepId = "step_2",
                    description = "Move the copied charge object into the charging area until its charge changes.",
                    expectedAction = "charge_object",
                    allowedIdleSec = 20f
                });
                steps.Add(new LabStep
                {
                    stepId = "step_3",
                    description = "Place the charged object into the observation area to create electric field lines.",
                    expectedAction = "enter_observation_zone",
                    allowedIdleSec = 20f
                });
                steps.Add(new LabStep
                {
                    stepId = "step_4",
                    description = "Observe how the electric field lines change for this charge geometry.",
                    expectedAction = "observe_field_lines",
                    allowedIdleSec = 30f
                });
                steps.Add(new LabStep
                {
                    stepId = "step_5",
                    description = "Repeat the experiment with another geometry and compare the field pattern.",
                    expectedAction = "grab_object",
                    allowedIdleSec = 30f
                });
            }

            if (string.IsNullOrWhiteSpace(currentStepId) || currentStepId.StartsWith("E", StringComparison.OrdinalIgnoreCase))
            {
                currentStepId = steps[0].stepId;
            }
        }

        private void HandleActionLogged(InteractionEvent action)
        {
            if (!autoAdvanceFromActions || action == null) return;

            switch (CurrentStepId)
            {
                case "step_1":
                    if (IsChargeObject(action) && IsAny(action.action_type, "grab_object", "select_object"))
                    {
                        SetCurrentStep("step_2");
                    }
                    break;

                case "step_2":
                    if (IsChargeObject(action) && action.action_type == "charge_object")
                    {
                        SetCurrentStep("step_3");
                    }
                    break;

                case "step_3":
                    if (IsChargeObject(action) && action.action_type == "enter_observation_zone")
                    {
                        SetCurrentStep("step_4");
                    }
                    break;

                case "step_4":
                    if (IsChargeObject(action) && action.action_type == "grab_object")
                    {
                        SetCurrentStep("step_5");
                    }
                    break;

                case "step_5":
                    if (IsChargeObject(action) && action.action_type == "charge_object")
                    {
                        SetCurrentStep("step_3");
                    }
                    break;
            }
        }

        private static bool IsChargeObject(InteractionEvent action)
        {
            if (action == null || string.IsNullOrEmpty(action.object_type)) return false;
            return IsAny(action.object_type, "Sphere", "Wire", "Plane", "Charge");
        }

        private static bool IsAny(string value, params string[] candidates)
        {
            if (string.IsNullOrEmpty(value) || candidates == null) return false;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (value.Equals(candidates[i], StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
