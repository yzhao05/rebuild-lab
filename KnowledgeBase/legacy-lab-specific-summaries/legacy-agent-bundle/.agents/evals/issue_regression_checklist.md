# Issue Regression Checklist

Use this when debugging a future lab. It catches problems already seen in Lab 1-3.

## Unity Workflow

- [ ] Did the agent avoid default batchmode?
- [ ] Was scene generation done through Editor menu unless explicitly requested otherwise?
- [ ] Did the builder refuse to run in Play Mode?
- [ ] Are Console errors separated from non-blocking Unity service warnings?

## Camera And Text

- [ ] Does Play Mode use the intended camera?
- [ ] Is text readable in Game view?
- [ ] Is text facing the camera?
- [ ] Are there no mirrored labels?
- [ ] Are text panels separated by purpose?

## Ray And Collider Problems

- [ ] Are transparent colliders disabled or filtered?
- [ ] Do draggable objects have generous hit areas?
- [ ] Do line-based visuals have colliders if interactive?
- [ ] Does ray selection find the intended object behind display volumes?

## Orthographic Depth Problems

- [ ] Does drop-zone logic match what the user sees on screen?
- [ ] Are objects snapped to the visualization plane when needed?
- [ ] Is hidden `z` depth avoided for screen-space interactions?

## State Feedback

- [ ] Does the scene show selected/active objects?
- [ ] Does the scene show applied charge/current/film/preset state?
- [ ] Can the user tell whether a drag or click was recognized?

## Input Problems

- [ ] Are shortcut keys handled once?
- [ ] Are old and new Unity input systems considered if needed?
- [ ] Is mouse testing aligned with the promised VR mapping?
- [ ] Are click actions preferred when drag-through-depth is unreliable?

## Documentation Drift

- [ ] Did README files change after interaction semantics changed?
- [ ] Does the guide say which objects are display-only?
- [ ] Does the guide describe the current preset meanings?
- [ ] Does the guide tell the user what screenshots to provide?
