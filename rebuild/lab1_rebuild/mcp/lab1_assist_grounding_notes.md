# Lab 1 Assist Grounding Notes

Source: `LabMCP.build_assist_grounding_template` for `lab1_electrostatics`.

## Gateway

- Route: `/v1/assist`
- Response root: `unity_output`

No host or port is fixed in this repository.

## Required Fields

- `session_id`
- `user_id`
- `lab_id`
- `step_id`
- `mode`
- `student_input_text`
- `selected_object_json`
- `objects_json`
- `recent_actions_json`
- `diagnostic_hints_json`
- `trigger_reason_json`
- `step_rubric_json`
- `session_memory_json`

## Entry Step

- `L1S1`: prepare a charged point object.

## Important Diagnostics

- `wrong_object_type`
- `object_not_charged`
- `field_direction_confusion`
- `density_strength_confusion`
- `superposition_confusion`

The rebuild should keep object IDs and status labels stable enough to map runtime actions to these diagnostic concepts in a later assist integration pass.
