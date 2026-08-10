# Lab 1 PDF Requirements

Source: `E:/暑研/EM_VR_Labs/Docs/Handbook/Lab 1 Electrostatics.pdf`

## Learning Goals

- Explain point, line, and surface charge distributions.
- Observe electric field direction, density, and intensity.
- Connect attraction and repulsion to field-line patterns.
- Use superposition to reason about combined fields.
- Change charge amount and position in VR to observe field changes.

## Required Physics Models

- Point charge: electric field follows Coulomb-law direction and inverse-square falloff.
- Line charge: field is radial around the line and scales inversely with perpendicular distance.
- Surface charge: field is normal to the plane and approximately uniform for an ideal plane.
- Multiple sources: total field is the vector superposition of all active source fields.

## Required Scene Objects

- Draggable charge objects:
  - point charge
  - line charge
  - surface charge
- Red positive charger and blue negative charger.
- Observation / visualization cube where charged objects produce visible field lines or vector maps.
- Instruction panels for workflow, report questions, and formulas.
- Presets for the report scenes and tutorial states.
- Status feedback showing the currently selected object, charge state, active sources, and active preset.

## Interaction Requirements

- VR handle ray points at objects and buttons.
- Grab button grabs objects.
- OK/select activates buttons and instructions.
- Desktop fallback maps mouse movement to pointer direction and left mouse hold to grabbing/dragging.
- Charging should visibly change object charge.
- Field visualization should only use active charged objects in the observation zone.

## Report / Exercise Coverage

- Q1-Q2: identify the charge distribution and relative charge amount for Scene 1, then reproduce with a screenshot.
- Q3-Q4: identify charge distributions for Scene 2 and describe the field expression, then reproduce with a screenshot.
- Q5: reproduce top and front views for a field-line pattern from a specific charge distribution.
- Q6: identify the charge distribution for Scene 4 and reproduce with a screenshot.
- Q7: propose an alternative charge configuration approximating the region of interest.
- Q8 bonus: suggest future lab improvements.

## Implementation Priority

The minimum rebuild should first support:

1. point / line / plane source objects,
2. red / blue charging,
3. observation-zone field visualization,
4. visibly different presets,
5. readable labels and status feedback,
6. desktop testing before full VR polish.
