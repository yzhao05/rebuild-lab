# Summer26 Common Code Summary

This folder summarizes the shared code and Unity scene structure across the three Summer26 labs. It does not move runtime assets or scripts. Moving Unity files should be done later inside Unity or with `.meta` files preserved, otherwise scene references can break.

## Three-Lab Scene Map

| Lab | Scene | Entry script | Main domain scripts | Main concept |
|---|---|---|---|---|
| Lab 1 | `Assets/Scenes/Static_Electric_Field.unity` | `Assets/Scene1.cs` | `Assets/Scripts/ElectricField.cs`, `Sphere.cs`, `Wire.cs`, `Plane.cs`, `ChargingBox.cs` | Static electric fields from charge geometries |
| Lab 2 | `Assets/Scenes/Static_Magnetic_Field.unity` | `Assets/Scene2.cs` | `Assets/Scripts/MagneticField.cs`, `Wire.cs`, `Solenoid.cs` | Static magnetic fields from current sources |
| Lab 3 | `Assets/Scenes/Wave_Polarization.unity` | `Assets/Scene3.cs` | `Assets/Scripts/WaveField.cs`, `Polarizer.cs`, `LinearPolarizer.cs`, `QuarterPlate.cs` | Wave polarization through optical elements |

Shared or launcher scenes:

- `Assets/Scenes/start.unity`
- `Assets/Scenes/startVR.unity`

Scene loading is handled by small scripts:

- `Assets/Scene1.cs` loads `Static_Electric_Field`
- `Assets/Scene2.cs` loads `Static_Magnetic_Field`
- `Assets/Scene3.cs` loads `Wave_Polarization`
- `Assets/LoadScene.cs` loads a configured `Scene_name` when the attached object moves beyond `moverange`
- `Assets/Scripts/Back.cs` and `Assets/QUIT.cs` provide simple navigation / application exit behavior

## Common Runtime Patterns

### 1. XR Locomotion And Hands

Reusable across all labs:

- `Assets/Continuous_Movement.cs`: reads XR joystick axis and moves the `XROrigin` with a `CharacterController`
- `Assets/HandPresence.cs`: spawns hand prefabs and maps XR trigger / grip values into hand animation parameters
- `Assets/GrabDetect.cs`, `GrabDetect_Left.cs`, `GrabDetect_Right.cs`: track grabbed objects and expose current grabbed references
- `Assets/Scripts/Respawn.cs`: resets objects to a spawn position
- `Assets/Trashcan.cs`: supports object cleanup behavior

These are strong candidates for a future `Assets/Scripts/EMVR/Common/XR` module.

### 2. Step And Instruction Flow

Current reusable pieces:

- `Assets/Scripts/Step.cs`: drives previous / next step UI, and can bind to `EMVR.Assist.Core.StepProgressManager`
- `Assets/Scripts/Start_Game.cs`: shows or hides instruction and game objects after a start button click
- `Assets/Scripts/Game_1_*.cs`, `Game_2_*.cs`, `Game_3_*.cs`: detect object arrangements inside a volume and advance instruction UI

The `Game_*` scripts repeat the same pattern for `Sphere`, `Wire`, and `Plane`:

- check whether the start button has been pressed
- run `Physics.OverlapBox`
- count or compare objects by tag
- inspect `electric_mag`
- hide current instructions
- show feedback and then show the next instruction

They are shared in structure, but still hardcoded to electrostatics object types. A later common implementation could use configurable target object type, charge/current sign rule, required count, and next UI targets.

### 3. AI Assist Framework

The most modular common code is already under:

`Assets/Scripts/EMVR/Assist`

Main groups:

- `Core`: `AssistConfig`, `AssistPayloadBuilder`, `LabSessionManager`, `StepProgressManager`
- `Data`: payload, response, diagnostic hint, object, and interaction event models
- `Logging`: `InteractionEventLogger`
- `Network`: `AssistGatewayClient`
- `Objects`: `ExperimentObject`, `ExperimentObjectRegistry`, `SelectionTracker`, `AssistSelectionReporter`, `ElectrostaticsAssistBinder`
- `Outcome`: `OutcomeTracker`
- `Proactive`: `BehaviorMonitor`
- `UI`: `AssistUIController`, `AssistResponseExecutor`
- `Validation`: `LocalValidationManager`

This should become the shared assist layer for lab1, lab2, and lab3. The only lab-specific piece in this set is `ElectrostaticsAssistBinder`, which should later be generalized or replaced by one binder per lab.

Legacy assist DTOs also exist in:

- `Assets/Scripts/AssistantRequest.cs`
- `Assets/Scripts/AssistantResponse.cs`
- `Assets/Scripts/BridgeResponse.cs`

These overlap conceptually with the newer `EMVR/Assist/Data` models.

### 4. Domain Object Model

Several lab objects use the `ChargedObjects` namespace and expose simple public state that field systems read every frame.

Shared object pattern:

- object is a Unity `MonoBehaviour`
- object has public endpoints, magnitude fields, or orientation state
- field renderer detects objects in a trigger volume
- field renderer stores matching objects in a list
- field renderer recomputes visual lines or vectors during `Update`

Examples:

- `Sphere.cs`, `Wire.cs`, `Plane.cs`: charge/current-like source objects with `electric_mag`
- `Solenoid.cs`: magnetic source with endpoint references and `electric_mag`
- `Polarizer.cs`, `LinearPolarizer.cs`, `QuarterPlate.cs`: optical elements driven by transform orientation or fixed phase behavior

The shared abstraction should be something like:

- source object identity
- domain object type
- physical magnitude
- transform / endpoint geometry
- telemetry roles
- assist-readable state

### 5. Field And Visualization Systems

Each lab has one main visual simulation controller:

- `ElectricField.cs`: tracks charge objects and draws electric field lines
- `MagneticField.cs`: tracks wires / solenoids and draws magnetic field lines, with optional GPU compute path
- `WaveField.cs`: instantiates repeated arrow/vector prefabs and updates field magnitude / orientation through polarizers

Common behavior:

- keep a list of active source or optical objects
- detect objects entering / exiting an interaction volume
- recompute visualization in `Update`
- use Unity transforms as the source of geometry and orientation
- expose visual artifacts as lines, arrows, or vector objects

Future common interfaces could separate:

- object detection
- field sampling
- visualization rendering
- telemetry emission
- assist state export

## Existing Shared Assets

Common visual / interaction dependencies:

- `Assets/Prefab/Arrow.fbx`
- `Assets/Prefab/LinearPolarizer.fbx`
- `Assets/Prefab/LinearPolarizer (1).prefab`
- `Assets/Samples/XR Interaction Toolkit/2.6.5`
- `Assets/TextMesh Pro`
- `Assets/Oculus Hands`

Lighting data is stored per scene or folder:

- `Assets/Scenes/Static_Electric_FieldSettings.lighting`
- `Assets/Scenes/Static_Magnetic_FieldSettings.lighting`
- `Assets/Scenes/start/LightingData.asset`
- `Assets/Scenes/Wave_Polarization/LightingData.asset`

## Recommended Common Module Split

Suggested future target layout:

```text
Assets/Scripts/EMVR/Common/
  XR/
    ContinuousMovement.cs
    HandPresence.cs
    GrabTracker.cs
  SceneFlow/
    SceneLoadTrigger.cs
    StartLabButton.cs
    StepNavigator.cs
  Objects/
    ExperimentObjectState.cs
    PhysicalSourceObject.cs
    OpticalElementObject.cs
  Visualization/
    FieldVisualizerBase.cs
    LineFieldVisualizer.cs
    VectorWaveVisualizer.cs
  Validation/
    PlacementRule.cs
    VolumeConditionRule.cs
```

Suggested lab-specific layout:

```text
Assets/Scripts/EMVR/Labs/
  Electrostatics/
  Magnetostatics/
  WavePolarization/
```

## Refactor Notes

The safest next step is not to move code immediately. First, define interfaces and adapters around the repeated behaviors:

- `ILabObjectStateProvider` for object state export
- `IFieldSource` for electric, magnetic, and wave sources
- `IPlacementCondition` for instruction progression rules
- `ILabTelemetryEmitter` for shared event logging
- `IAssistGroundingProvider` for AI payload grounding

Once these interfaces exist, scene-specific scripts can be migrated one lab at a time while keeping current scenes functional.

