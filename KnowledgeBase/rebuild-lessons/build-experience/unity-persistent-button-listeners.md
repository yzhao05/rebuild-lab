# Unity persistent button listeners in EditMode

## Failure pattern

A Unity UI button may look correctly wired and report one or more persistent
listeners, yet `Button.onClick.Invoke()` in an EditMode test does nothing. A
listener added through `UnityEventTools.AddPersistentListener` defaults to
`RuntimeOnly`, so Unity skips it while the Editor is not in Play Mode.

## Prevention rule

- For protected Gate 5 buttons, call the protected Foundation button helper;
  do not recreate or patch its listener wiring in Lab code.
- A maintainer creating a persistent listener that must be exercised in
  EditMode must explicitly set the newly added listener to
  `UnityEventCallState.EditorAndRuntime`.
- Wiring validation must inspect the persistent listener state, not only the
  listener count and `Button.interactable`.
- The regression test must invoke the real persistent button in EditMode and
  verify the resulting object or state change; a direct method call is not an
  equivalent test.

## Ownership

During a blind rebuild, a failure in the protected helper or protected generic
test is a Foundation defect. Stop the gate and report it. Only the workflow
maintainer may repair the protected source, manifest, immutable copy, and
import evidence. The rebuild Agent must not add a Lab-side bypass.
