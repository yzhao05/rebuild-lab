# Playbook: Debug Visual Layout

Use this when text, labels, camera view, scene scale, or visualization clarity is wrong.

## Inputs To Request

- Game view screenshot in Play Mode.
- Scene view screenshot if object placement is unclear.
- Console screenshot if warnings/errors appear.
- Whether the problem appears before Play Mode, after Play Mode, or both.

## Checklist

- Confirm which camera renders the Game view.
- Confirm text faces the runtime camera.
- Check whether mirrored text comes from viewing the back side of TextMesh.
- Check whether both text and camera were flipped, hiding the true issue.
- Reduce text density before shrinking text too far.
- Split text into workflow, result/status, formula/model, and report/task areas.
- Ensure labels do not overlap buttons or visualization objects.
- Make display-only objects clearly labeled as display-only.
- Move result/status panels away from controls if they compete visually.
- For orthographic cameras, tune camera size after scene scale is stable.

## Proven Fixes

- Keep labels short: use `LP`, `QWP`, `axis 45`, `fast 45` style text where possible.
- Use a status bar for dynamic state instead of long static explanations.
- Prefer object-adjacent labels over large instruction panels.
- If text is blurry, adjust TextMesh font settings, camera size, and text distance together.
- If scene appears too cramped, expand object spacing and retune the camera view.

## Done When

- The user can identify what to drag, what to click, what is display-only, and what changed.
- Text is readable in the Game view screenshot.
- No important controls or labels overlap.
- The main phenomenon is visible without reading implementation notes.
