using System.Collections.Generic;
using EMVR.Assist.Core;
using EMVR.Assist.Data;
using EMVR.Assist.Objects;
using UnityEngine;

namespace EMVR.Assist.Validation
{
    public class LocalValidationManager : MonoBehaviour
    {
        public static LocalValidationManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public DiagnosticHints BuildHints()
        {
            string stepId = StepProgressManager.Instance != null
                ? StepProgressManager.Instance.CurrentStepId
                : "unknown";

            string expected = StepProgressManager.Instance != null
                ? StepProgressManager.Instance.ExpectedAction
                : "";

            var hints = new DiagnosticHints
            {
                current_step = stepId,
                expected_action = expected ?? "",
                rule_results = new List<RuleResult>(),
                sequence_deviation = false,
                local_error_candidate = "none"
            };

            ApplyElectrostaticsRules(hints);
            return hints;
        }

        private void ApplyElectrostaticsRules(DiagnosticHints hints)
        {
            if (SelectionTracker.Instance == null) return;
            var selected = SelectionTracker.Instance.CurrentSelected != null
                ? SelectionTracker.Instance.CurrentSelected
                : SelectionTracker.Instance.LastInteracted;
            if (selected == null) return;

            switch (hints.current_step)
            {
                case "E1":
                case "step_1":
                    if (selected.objectType == ExperimentObjectType.ChargeZone) break;
                    if (selected.isCharged)
                    {
                        hints.rule_results.Add(new RuleResult
                        {
                            rule_id = "E1_object_should_start_uncharged",
                            passed = false,
                            severity = "low",
                            message = "E1 期望选择未带电对象，当前选中对象已带电",
                            related_objects = new[] { selected.objectId }
                        });
                        hints.sequence_deviation = true;
                        hints.local_error_candidate = "operation_error";
                    }
                    break;

                case "E2":
                case "step_3":
                    if (selected.objectType == ExperimentObjectType.Sphere
                        || selected.objectType == ExperimentObjectType.Charge)
                    {
                        if (!selected.isCharged)
                        {
                            hints.rule_results.Add(new RuleResult
                            {
                                rule_id = "E2_charge_object_should_be_charged",
                                passed = false,
                                severity = "medium",
                                message = "E2 需要带电对象进入观察区，当前选中对象未带电",
                                related_objects = new[] { selected.objectId }
                            });
                            hints.sequence_deviation = true;
                            hints.local_error_candidate = "operation_error";
                        }
                    }
                    break;

                case "E3":
                case "step_4":
                    if (selected.objectType != ExperimentObjectType.FieldLine
                        && selected.objectType != ExperimentObjectType.ObservationZone
                        && selected.objectType != ExperimentObjectType.Sphere
                        && selected.objectType != ExperimentObjectType.Charge)
                    {
                        hints.rule_results.Add(new RuleResult
                        {
                            rule_id = "E3_focus_should_be_on_field_lines_or_charges",
                            passed = false,
                            severity = "low",
                            message = "E3 期望关注电场线或带电对象，但当前选中无关对象",
                            related_objects = new[] { selected.objectId }
                        });
                        hints.sequence_deviation = true;
                        hints.local_error_candidate = "attention_error";
                    }
                    break;
            }
        }
    }
}
