# Lab Issue Catalog From Summaries

This catalog preserves concrete issues and final effective fixes from Lab 1, Lab 2, and Lab 3. Use it when a future lab resembles an earlier problem. The main agent rules and common lessons are intentionally compact; this file is the detailed issue-to-solution reference.

## Lab 1: Electrostatics

### L1-T01 Unity Generation Flow

Issue:
- Silent batchmode generation took too long.
- The user could not tell whether Unity was generating, compiling, loading, or stuck.
- The scene builder could be triggered in Play Mode, causing Editor API errors.

Final fix:
- Avoid long invisible batchmode work by default.
- Split the task into document parsing, script generation, scene generation, Play Mode testing, and fixes.
- Add generation logs.
- Trigger generation through a Unity Editor menu.
- Make the builder refuse to run in Play Mode and tell the user to exit Play Mode.

### L1-T02 Scene Camera And Audio Listener Setup

Issue:
- The scene had two `AudioListener` components.
- Desktop preview camera and XR camera responsibilities were unclear.
- The runtime Game view camera differed from the edit-time Scene view.

Final fix:
- Keep exactly one active `AudioListener`.
- Separate Desktop Preview Camera and XR Camera responsibilities.
- Fix the main runtime camera position and view.
- Prevent XR rig or preview camera from overriding the expected view.

### L1-T03 Unity API Usage

Issue:
- `renderer.material` was accessed in edit mode, causing material instancing warnings.
- Editor-only scene APIs could be called during Play Mode.
- Dragging code could set velocity on a kinematic rigidbody.

Final fix:
- Use `sharedMaterial` for edit-time shared material access.
- Use `MaterialPropertyBlock` for dynamic per-renderer color changes.
- Restrict Editor generation logic to non-Play Mode.
- Check rigidbody kinematic state before writing physics velocity.

### L1-T04 Text Readability And Mirroring

Issue:
- Initial text was too large.
- Text panels overlapped heavily.
- Text became mirrored in Play Mode.
- Hover annotations faced the wrong direction.
- Some text and hover annotations were blurry.

Final fix:
- Reduce TextMesh size and rebuild layout.
- Split information into Workflow, Report, and Models sections.
- Increase spacing and contrast between text and backing panels.
- Make text face the runtime camera.
- Distinguish Scene view from Play Mode Game view.
- Tune camera distance and text scale together.

### L1-T05 Camera View

Issue:
- The scene faced the camera diagonally.
- Text direction fixes created mirrored text.
- Text looked correct in Scene view but wrong in Play Mode.

Final fix:
- Make the lab bench face the camera directly.
- Fix the Play Mode main camera position.
- Avoid flipping both camera and text in ways that cancel each other.
- Use the Game view result as the final visual truth.

### L1-T06 XR And Desktop Testing

Issue:
- XR Device Simulator panel was too large and blocked the scene.
- Controller ray was invisible.
- Installed XR plugins did not provide the lab-specific interaction by default.
- VR interaction was hard to test without a headset.

Final fix:
- Hide or shrink XR Device Simulator UI where possible.
- Add XR fallback ray visualization.
- Keep desktop mouse simulation for VR controller testing.
- Map mouse movement to pointer direction and left mouse hold to grab/drag.
- Share a common drag interface between VR and desktop paths.

### L1-T07 Mouse, Button, And Hover Interaction

Issue:
- Only some `1-5` buttons responded.
- Button hover annotation faced the wrong direction.
- Transparent Field Cube could block rays to objects behind it.
- Only buttons `1`, `2`, and `5` were important draggable/test targets, but this was not obvious.

Final fix:
- Bind all `1-5` number keys and preset buttons correctly.
- Add `ElectrostaticPresetPad` logic to preset buttons.
- Use `Physics.RaycastAll` and select the nearest valid draggable object.
- Extract the common drag contract as `IEmVrMouseDraggable`.
- Visually highlight buttons `1`, `2`, and `5`.

### L1-T08 Charging And Physical State Feedback

Issue:
- Charging zones looked unresponsive.
- Dragging objects into red/blue areas did not visibly change charge.
- The user could not tell charges from chargers.

Final fix:
- Red region assigns positive charge.
- Blue region assigns negative charge.
- Charge immediately when a draggable object enters a charge region.
- Add a distance fallback instead of relying only on trigger collisions.
- Use color, labels, and instructions to distinguish charge, charger, and preset objects.

### L1-T09 Scene Scale

Issue:
- The initial scene was too cramped.
- Text, buttons, and visualization regions pressed into each other.
- The user requested a larger experiment space.

Final fix:
- Expand the lab scene dimensions by about two times.
- Adjust camera distance and view after scaling.
- Increase spacing among buttons, panels, and experiment objects.
- Keep the full subject visible in Game view after scaling.

### L1-T10 Code Organization

Issue:
- Lab 1 code was initially mixed together.
- Lab 2 and Lab 3 needed to reuse hover, XR ray, and interaction logic.
- Without separation, later labs would duplicate code.

Final fix:
- Create `Assets/Scripts/Common/`.
- Create lab-specific directories:
  - `Assets/Scripts/Lab1_Electrostatics/`
  - `Assets/Scripts/Lab2_Magnetostatics/`
  - `Assets/Scripts/Lab3_WavePolarization/`
- Separate common interaction helpers from lab-specific physics.
- Preserve `.meta` files during moves.

### L1-D01 Task Scope Too Large

Issue:
- The original request asked to build a full Unity physics 3D/VR lab from scratch.
- This included handbook reading, physics modeling, Unity scene generation, interaction, VR support, visualization, and documentation.

Final fix:
- Split future work into stages:
  - read the experiment document;
  - design the scene;
  - build the minimum runnable version;
  - add VR interaction;
  - optimize visuals and interaction.

### L1-D02 Unity Runtime Environment Not Clear

Issue:
- Unity path and current Editor state were not known initially.
- An already open Unity Editor can affect project locks and batchmode.

Final fix:
- Ask for Unity version, Unity Editor path, whether the Editor is already open, whether compilation is in progress, and whether batchmode is allowed.

### L1-D03 VR Priority Not Clear Initially

Issue:
- Early implementation leaned toward desktop 3D.
- The user later clarified this was a VR lab with XR plugins installed.

Final fix:
- Clarify at the start whether real VR devices, XR Device Simulator, desktop testing, and mouse-to-controller mapping are required.

### L1-D04 Desktop Test Mapping Not Clear Initially

Issue:
- The user later specified that mouse movement represents controller direction and left mouse hold represents drag.

Final fix:
- Define the test input mapping before implementation:
  - real VR controller;
  - XR Device Simulator;
  - mouse and keyboard;
  - hover, click, drag, and hold semantics.

### L1-D05 Visual Priority Not Clear Initially

Issue:
- Initial scene tried to show too much handbook text.
- The user then reported text that was too large, overlapping, or unreadable.

Final fix:
- Clarify whether text targets desktop or VR.
- Decide language and density up front.
- Limit visible text per screen and prefer short labels plus guides.

### L1-D06 Interactive Object Roles Not Clear

Issue:
- The user asked where the charges were.
- The user needed buttons `1`, `2`, and `5` highlighted.
- The difference between charge, charger, preset, observation object, and decoration was not obvious.

Final fix:
- Before scene design, list which objects are draggable, clickable, display-only, state-changing, and preset-changing.
- Use labels and color to make object roles obvious.

### L1-D07 Screenshots Are Critical Feedback

Issue:
- Mirrored text, overlapping fonts, simulator obstruction, Console warnings, and Game/Scene view mismatches were only visible through screenshots.

Final fix:
- After major changes, ask for Game view, Scene view, Console screenshots, and Play Mode state.

### L1-D08 Handbook Must Be Translated Into Interaction

Issue:
- A PDF describes lab questions and theory, but Unity needs objects, buttons, labels, interactions, and visualizations.

Final fix:
- Decide which questions are fully implemented, which are text prompts, which formulas are displayed, and whether report screenshot positions are needed.

### L1-D09 Multi-Lab Planning Should Start Early

Issue:
- Lab 1 was built first, then Lab 2 and Lab 3 common-code needs appeared later.

Final fix:
- For a lab series, plan `Common`, per-lab directories, shared interaction interfaces, README templates, and scene builder naming from the first lab.

### L1-S01 Curl Error 60

Issue:
- Unity reported `Curl error 60` certificate verification warnings.

Final fix:
- Treat this as a Unity service, network, proxy, Package Manager, or certificate issue, not a lab code bug.
- It usually does not block local lab scene behavior.

### L1-S02 Experiment README

Issue:
- Lab 1 needed a README explaining interfaces and how to operate the experiment.

Final fix:
- Add a Lab 1 README with scene entry, desktop test path, VR operations, `1-5` presets, Lab 1 scripts, Common scripts, recommended testing flow, and caveats.

## Lab 2: Magnetostatics

### L2-T01 Batchmode Is Not Suitable

Issue:
- Batchmode generation provided poor observability.
- The user could not tell whether Unity was compiling, importing assets, loading the project, or locked by an open Editor.
- Visual problems required Game screenshots and Console feedback.

Final fix:
- Stop using batchmode for Lab 2 by default.
- Use the Editor menu:
  - `Tools > Magnetostatics Lab > Build Lab 2 Scene`
- Require the user to exit Play Mode and wait for compilation.
- Keep generation logs at `tmp/magnetostatics-lab-build-progress.log`.

### L2-T02 Null MaterialPropertyBlock

Issue:
- Scene generation failed with `Value cannot be null. Parameter name: dest`.
- The stack pointed to `MagneticCurrentSource.UpdateVisuals()` and `Renderer.GetPropertyBlock(...)`.

Final fix:
- Lazy-create `MaterialPropertyBlock`.
- Ensure `propertyBlock != null` before `GetPropertyBlock`.
- Skip `TextMesh` renderers when applying current-source color logic.

### L2-T03 Text Too Small Or Blurry

Issue:
- Top instructions, button text, and bottom hints were small or blurry.
- Text overlapped panels/buttons.
- Raising resolution with different scale settings made visual size wrong.

Final fix:
- Reuse the Lab 1 text strategy.
- Use `TextMesh fontSize = 384`.
- Do not use the `768 + smaller characterSize` scheme.
- Tune scene scale and camera before text settings.
- Use `legibleOrthographicSize = 5.6`.
- Let `MagneticScenePresentation` fix old scene text settings at runtime.
- Call `RequestCharactersInTexture(...)`.
- Use `FilterMode.Trilinear` and `anisoLevel = 8` for font textures.
- Use short top instructions and layered bottom buttons.

### L2-T04 Transparent Visualization Cube Blocks Dragging

Issue:
- Current sources could be dragged near screen edges.
- After moving into the central transparent visualization cube, they could not be selected again.

Final fix:
- Disable the transparent visualization cube collider.
- Builder disables the stage collider for new scenes.
- `MagneticScenePresentation` disables it at Play Mode runtime for old scenes.

### L2-T05 Visualization Depth Mismatch

Issue:
- A solenoid visually appeared inside the transparent area but did not show the magnetic field.

Final fix:
- On drag start, find `MagneticFieldVisualizer`.
- Fix dragged object `z` to the visualization plane.
- Future dragging changes only screen-plane position.

### L2-T06 Drag Sensitivity And Coordinate Jump

Issue:
- Dragging was too sensitive at first, then too sluggish after tuning.
- Old ray-plane intersection logic caused jumps when changing depth planes.

Final fix:
- Use orthographic-camera screen displacement dragging.
- Convert mouse screen delta into world delta.
- Stop using old depth-plane initial hit calculations.
- Use `dragSensitivity = 2f`.

### L2-T07 Solenoid Hard To Select

Issue:
- Straight wire could drag, but solenoid was unreliable or impossible to select.
- Solenoid was drawn with `LineRenderer`, making hit tests unstable.

Final fix:
- Add stable colliders for solenoids.
- Runtime identifies solenoids and adds a larger `BoxCollider`.
- Enlarge existing `CapsuleCollider`.
- Builder creates these colliders for new scenes.

### L2-T08 Current Assignment Interaction

Issue:
- Dragging a source through red/blue current loader looked correct but did not assign current.
- The source and loader were at different depths.

Final fix:
- Change assignment to a click workflow:
  - select or drag a current source;
  - place it near screen center;
  - click red/blue current loader;
  - loader applies current to the selected source or nearest source near screen center.
- `MagneticDesktopDraggable` records selected `MagneticCurrentSource`.
- `MagneticCurrentEnergizer.OnMouseDown()` performs assignment.

### L2-T09 Shortcut Key Handling

Issue:
- `C` worked.
- `F`, `V`, and `X` seemed ineffective.
- `F` and `V` were handled in both controller and visualizer, toggling twice.
- `X` only used old input APIs.

Final fix:
- Centralize shortcuts in `MagneticLabController`.
- Stop reading keyboard input in `MagneticFieldVisualizer`.
- Handle `C/F/V/X/+/-`.
- Support old Input and new Input System where needed.

### L2-T10 Experiment Documentation

Issue:
- Original README was more like a script interface document.
- It did not directly tell students/testers how to run Q1-Q6, screenshots, or current loader operations.

Final fix:
- Add `Assets/Scripts/Lab2_Magnetostatics/Lab2_Experiment_Guide_README.md`.
- Include scene opening, desktop testing, VR controls, current loader use, Scene `1-5` to Q1-Q6 mapping, Q7 bonus, screenshot advice, and common issues.

### L2-D01 Task Scope Too Large

Issue:
- Lab 2 required document parsing, physics modeling, scene generation, interaction, magnetic visualization, README, and debugging.

Final fix:
- Continue staged development:
  - parse document;
  - build minimum scene;
  - add interaction and visualization;
  - finish visuals, operation flow, and docs.

### L2-D02 Reuse Lab 1 Conclusions

Issue:
- The user said not to ask again when Lab 1 summary already had a solution.
- Lab 2 repeated batchmode, font clarity, and transparent collider issues.

Final fix:
- Read previous lab summaries before new labs.
- Directly reuse:
  - manual Editor menu generation;
  - TextMesh clarity strategy;
  - disabling transparent blocking colliders;
  - desktop mouse testing path.

### L2-D03 Interaction Should Follow User Testing

Issue:
- Initial current assignment assumed drag-through-loader behavior.
- User testing showed click-to-assign was more stable.

Final fix:
- Prefer user-stable operations.
- Avoid hidden 3D depth triggers for critical state changes.
- Use explicit click/button triggers for important assignments.

### L2-D04 Visual Debugging Needs Screenshots

Issue:
- Blurry, small, or overlapping text was hard to judge from code.

Final fix:
- Use Game view screenshots as the final judge for visual issues.
- Store confirmed visual strategies in summaries for reuse.

### L2-D05 Hit Area Must Match Visual Expectation

Issue:
- The user saw the solenoid on screen but could not drag it because the hit area did not match the visual object.

Final fix:
- Add interaction colliders to `LineRenderer`, thin-line, and transparent/visual objects when needed.
- Make hit areas match user expectation, not just raw geometry.

### L2-D06 README Must Be Operation-Oriented

Issue:
- The user needed a "how to run the experiment" README, not just script notes.

Final fix:
- Every lab should have an experiment operation document with scene path, dragging, physical quantity assignment, preset-to-question mapping, observation, and screenshot instructions.

### L2-D07 Maintain A Multi-Lab Issue Library

Issue:
- Lab 2 repeated Lab 1 issues and added new ones.

Final fix:
- After each lab, keep both a process summary and a categorized issue summary.
- Record final effective solutions for direct reuse.

### L2-2-T01 New Lab Prototype Must Preserve Donor Scene Roles

Issue:
- The second Lab 2 attempt was a new-lab prototype, not a direct rebuild.
- Broadly hiding donor objects caused regressions:
  - hiding `_Level` removed the room/background;
  - hiding `Chargebox`, `Visualization_Cube`, and buttons removed reusable UI;
  - hiding `Wire` and `Solenoid_*` removed current sources needed by donor visualization.

Final fix:
- Classify objects before changing active state: shared environment, reusable UI, donor source objects, generated prototype objects, and debug-only objects.
- Preserve room/background roots and reusable Lab 2 UI by default.
- Add recovery functions such as `EnsureSharedEnvironmentVisible()`, `EnsureLab2InteractiveObjectsVisible()`, and `EnsureGeneratedPrototypeVisible()`.

### L2-2-T02 Duplicate XR/Event Systems Cause Lag And Warnings

Issue:
- Generated scenes could contain both the donor `XR Interaction Setup` and generated `TryNewLab_XR_Interaction_Setup`.
- Adding XRI prefabs could create two `EventSystem` objects.
- Unity warned about duplicate EventSystems and AudioListeners.
- Movement felt laggy even when overall hardware usage looked low.

Final fix:
- Add `EnsureSingleXriSetup()`.
- Add `EnsureOneEventSystem()` and prefer the EventSystem using `InputSystemUIInputModule`.
- Add `EnsureOneAudioListener()`.
- Provide `Tools > TryNewLab > Lab 2 > Optimize Current Scene Performance` to patch current scenes.

### L2-2-T03 Magnetic Field Lines Invisible In Play Mode

Issue:
- `Wire.Start()` and `Solenoid.Start()` reset `electric_mag` to `0`.
- Visible sources therefore had no effective current in Play Mode.
- `MagneticField` source registration relied on triggers and could miss active sources.
- `MagneticField.Start()` tried to initialize compute shader buffers even when CPU mode was intended; if `shader` was missing, startup could fail before line creation.

Final fix:
- Add `NewMagnetostaticsCurrentPreset` to wire and solenoid sources to keep nonzero startup current during Play Mode.
- Add `NewMagnetostaticsFieldSeeder` to register active wires and solenoids every second.
- Make `MagneticField.Start()` skip compute-shader initialization when `shader == null` and force CPU mode.
- Add null checks before disposing compute buffers.
- Make generated field lines white and visibly thick enough for debugging.

### L2-2-T04 Runtime Visualization Appears Only After Play Mode

Issue:
- `MagneticField.Start()` creates `Magnetic Field Line` GameObjects at runtime, so the edit-time scene can look correct while Play Mode becomes visually cluttered.
- Generated experiment content was initially too close to the XR camera, making field lines and text appear inside the user's view.

Final fix:
- Move generated Lab 2 content forward with `GeneratedLabOffset`.
- Add `LayoutGeneratedLab2Layer()` to patch already-generated scenes.
- Parent runtime field-line objects under the observation zone.
- Keep Play Mode screenshots as the final visual truth.

## Lab 3: Wave Polarization

### L3-T01 Batchmode Was Tried Again

Issue:
- Lab 2 already established that Codex should not proactively run Unity batchmode in this project.
- Batchmode was still attempted briefly.

Final fix:
- Stop proactive batchmode.
- Keep the Editor menu:
  - `Tools > Wave Polarization Lab > Build Lab 3 Scene`
- Let the user generate, enter Play Mode, and provide screenshots/Console feedback.

### L3-T02 Observation Stage Used 3D Bounds

Issue:
- The user dragged a film into the visual box, but output light did not change.
- Initial code used 3D `Bounds.Contains`, which failed under orthographic visual judgment.

Final fix:
- Use screen-plane `x/y` checks for whether a film is inside the stage.
- Do not depend on hidden `z` depth.
- Show active recognized films in a status bar:
  - `active films: LP 45, QWP 0 -> LHCP`

### L3-T03 Drag Hit Box Too Small

Issue:
- Blue/orange films were hard to select or seemed undraggable.
- Collider size was too close to the visual block size.

Final fix:
- Increase hit box:

```csharp
box.size = new Vector3(1.35f, 1.35f, 0.48f);
```

### L3-T04 Drag Sensitivity

Issue:
- `dragSensitivity = 2f` did not feel right for the scene and camera.

Final fix:

```csharp
dragSensitivity = 3f;
```

### L3-D01 Object Roles Were Unclear

Issue:
- The user did not know what the center box did.
- The user did not know what blue/orange blocks did.
- The user thought the sine waves might be draggable.

Final fix:
- Rename center stage label to `Drop films here`.
- Label blue rack as `drag blue LP films`.
- Label orange rack as `drag orange QWP films`.
- Label waves as `input wave display` and `output wave display`.
- Update README to say waves are display-only and not draggable.

### L3-D02 Too Much Text And Overlap

Issue:
- Top instructions, result text, element labels, and preset button text overlapped.
- The first view was hard to understand.

Final fix:
- Move top instruction panel upward.
- Move spare element racks downward.
- Move result panel to upper right.
- Shorten labels to `LP/QWP`, `axis 45`, and `fast 45`.

### L3-D03 Output Light Differences Were Not Obvious

Issue:
- Pressing `1-5` did not make output changes clear.
- A single yellow sine wave was insufficient for polarization differences.

Final fix:
- Add white E-field vector sample lines.
- Add instantaneous E vector in the polarization trajectory panel.
- Add arrowheads to vectors.
- Adjust presets so default effects differ clearly.

### L3-D04 Presets Were Not Pedagogically Visible

Issue:
- Presets were reasonable from the handbook but visually too subtle.

Final fix:
- Use visibly distinct default presets:
  - `1`: `LP 45` -> linear, `I = 1.00`
  - `2`: `QWP 0` -> circular
  - `3`: `QWP 0 + LP 90` -> linear, `I about 0.5`
  - `4`: `QWP 90` -> opposite circular handedness
  - `5`: `LP 0 + QWP 30` -> elliptical

### L3-D05 Animation Too Fast

Issue:
- The user wanted E-field vector rotation to slow by half.

Final fix:

```csharp
animationSpeed = 1.9f;
```

### L3-M01 Dragged Into Box But No Change

Issue:
- Technical cause: stage detection used hidden 3D depth.
- Design cause: there was no feedback showing whether the film was recognized.

Final fix:
- Use screen-plane `x/y` detection.
- Add status feedback:
  - `active films: LP 45, QWP 0 -> LHCP`

### L3-M02 Sine Waves Looked Static And Confusing

Issue:
- Technical cause: initial wave display did not show obvious animation/difference.
- Design cause: the user could not tell whether waves were display objects or draggable objects.

Final fix:
- Animate the wave display.
- Label waves as input/output displays.
- Add E-field vectors and arrowheads.

### L3-P01 Clarify Object Roles First

Rule:
- Every scene object should belong to one role:
  - draggable object;
  - clickable button;
  - display object;
  - calculation region;
  - instructional text;
  - result feedback.

### L3-P02 Visualization Must Show Core Differences

Rule:
- For polarization, do not show only one projected sine wave.
- Show propagation wave, transverse E-field vectors, polarization orbit, output classification, and phase difference.

### L3-P03 Status Bar Must Show Recognized State

Rule:
- Dragging, assignment, and preset changes need status feedback that shows what the system actually recognized.

### L3-P04 Orthographic Camera Should Avoid Hidden Depth

Rule:
- If the user judges by screen position, code should prefer screen-plane checks.

### L3-P05 Do Not Proactively Run Batchmode

Rule:
- Unless explicitly requested, do not use Unity batchmode as the default generation or compile-check workflow.

### L3-S01 Documentation Must Track Interaction Changes

Issue:
- README files could become misleading as scene labels and interactions changed.

Final fix:
- Update:
  - `Assets/Scripts/Lab3_WavePolarization/README.md`
  - `Assets/Scripts/Lab3_WavePolarization/Lab3_Experiment_Guide_README.md`
- Document that waves are display-only, the center box is the active observation stage, only films in the box participate in the Jones-vector cascade, blue blocks are linear polarizers, and orange blocks are quarter-wave plates.

### L3-2-T01 Donor WaveField Clones Masked The New Prototype

Issue:
- The second Lab 3 prototype copied `Assets/Scenes/Wave_Polarization.unity`.
- In Play Mode, the user saw a wave, but it was primarily the old donor `WaveField` spawning `Arrow (1)(Clone)` objects.
- This made the new generated prototype layer ambiguous or invisible.

Final fix:
- Preserve the shared room/background and XR setup.
- Disable donor experiment-specific objects such as `Arrow (1)`, old polarizers, old `QuarterPlate`, old stage `Cube`, and donor `WaveField`.
- Keep new content under `NewLab3_WavePolarization_Generated`.

### L3-2-T02 Generated Layer Was At The Wrong Lab 3 Coordinate Offset

Issue:
- After the old donor experiment layer was hidden, the user saw only the room and controllers.
- The new wave, plates, and controls were not visible.

Root cause:
- Old Lab 3 places the wave at `x = -28`.
- `WaveField.Start()` uses `new Vector3(-28, 1, 0)`.
- The new generated layer was initially placed near `x = 0`, outside the active Lab 3 view.

Final fix:
- Add `GeneratedLabOffset = new Vector3(-28f, 0f, 0f)`.
- Route generated object placement through `Lab3WorldPosition(...)`.
- Make the repair menu rebuild the generated layer at the corrected offset.

### L3-2-T03 QWP Reuse Must Not Inherit Old Rotation Lock

Issue:
- The new prototype reused `ChargedObjects.Polarizer` for quarter-wave plates.
- The legacy `Polarizer.Update()` forces non-linear polarizers to rotation `(0,0,0)`.
- This conflicts with the new prototype's fast/slow-axis labels and phase-orientation behavior.

Final fix:
- Keep `ChargedObjects.Polarizer` as a semantic marker.
- Disable the legacy component's runtime behavior for quarter-wave plates.
- Keep it enabled only for linear polarizers where its rotation constraint is useful.

### L3-2-P01 Reuse Scene Coordinates, Not Just Scene Assets

Rule:
- Before adding a generated layer to a copied donor scene, inspect the donor lab's actual experiment-object coordinates.
- Do not assume the active experiment space is centered at world origin.
- For Lab 3, remember the donor experiment area is around `x = -28`.

## Coverage Note

The compact agent files intentionally summarize these issues as reusable rules. This catalog is the place to check specific issue/fix pairs before implementing or debugging a future lab.
