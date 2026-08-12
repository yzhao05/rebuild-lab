# Unity beginner handoff standard

Use this standard every time the user must perform an action in Unity. Assume
the user has never used Unity, even when earlier messages suggest otherwise.

## Required structure

Never give only a shorthand instruction such as `run Test Runner`, `Clear the
Console`, `open Game View`, `run the Builder`, or `enter Play Mode`. Give one
numbered, self-contained procedure that includes:

1. **Starting state**: whether Unity should be in Edit Mode or Play Mode, which
   project/scene must be open, and whether script compilation must finish.
2. **Exact location**: the complete top-menu path, tab name, toolbar/button
   label, and where it normally appears in the window.
3. **Exact action**: what to click, select, expand, type, or wait for, in order.
4. **Target identity**: the exact assembly, test suite, scene, menu item, object,
   or Console filter the user must choose; never leave `<placeholder>` text.
5. **Success result**: the visible count, label, color, log line, scene state, or
   other observation that proves success.
6. **Failure evidence**: which window to screenshot and which text/count to
   copy. Ask for the earliest error, not an unbounded log dump.
7. **Do not do**: say explicitly whether the user must avoid Play Mode, the
   scene Builder, saving, clearing evidence, or another nearby action.

If a menu may differ by Unity version, state the primary path first and then a
single fallback path or search method. Do not wait for the user to ask where a
window is located.

## Required Test Runner procedure

When asking the user to run EditMode tests in Unity 2022.3, instantiate this
procedure with the current Lab's exact test assembly and expected tests:

1. Confirm the triangular Play button at the top is **not blue**. If it is blue,
   click it once to leave Play Mode.
2. Wait until the lower-right compilation/progress spinner disappears. Open the
   Console using `Window > General > Console` and confirm there are no red
   compiler-error entries before testing.
3. Open Test Runner from the Unity top menu:
   `Window > General > Test Runner`. The **Test Runner** tab normally docks in
   the same lower panel as Project and Console; it may open as a floating window.
4. In the Test Runner window, click the **EditMode** tab at the top. Do not
   select **PlayMode** unless the current instruction explicitly asks for it.
5. Use the search box in the upper-right of Test Runner and type the exact test
   assembly name supplied by the Agent, for example
   `EMVR.Labs.<CurrentLab>.Tests`.
6. Expand the matching assembly and suite using the small disclosure arrows.
   State the exact number and names of tests the user should see.
7. Click the assembly/suite once, then click **Run Selected** in the Test Runner
   toolbar. Use **Run All** only when the Agent explicitly asks to run every
   project test.
8. Wait for execution to finish. Success means every requested test has a green
   check mark and the summary shows the exact expected passed count with zero
   failures.
9. If tests are missing, click Test Runner's refresh button, run
   `Assets > Refresh` from Unity's top menu, wait for compilation, clear the
   search box, select **EditMode** again, and re-enter the exact assembly name.
10. If any test is red or Unity reports compilation errors, send one screenshot
    containing the Test Runner tree/summary and one screenshot of the Console.
    Select the first red Console entry so its full message and stack trace are
    visible. Do not enter Play Mode or run the scene Builder during this test.

Do not claim a test count, test name, or assembly name unless it was read from
the current Lab's `.asmdef` and test source. If the exact expected list is not
known, inspect those files before handing the procedure to the user.

## Other common Unity locations

Use these exact primary paths when relevant:

- Console: `Window > General > Console`. **Clear** is at the Console toolbar's
  upper-left. After clearing, use `Assets > Refresh` to request a reimport and
  wait for compilation to finish. Do not claim this is a full clean rebuild if
  no source/import change occurred; describe exactly what was observed.
- Game view: click the **Game** tab beside **Scene** in the central editor area.
  If missing, open it with `Window > General > Game`.
- Scene view: click **Scene** beside **Game**; fallback `Window > General > Scene`.
- Project window: usually lower-left; fallback `Window > General > Project`.
- Hierarchy: usually upper-left; fallback `Window > General > Hierarchy`.
- Inspector: usually right side; fallback `Window > General > Inspector`.
- Play Mode: click the triangular Play button centered in Unity's top toolbar.
  State whether it should become blue (enter) or gray (exit).
- Generated Lab scene Builder: use the exact menu path printed by `labflow
  scaffold`, normally `Tools > EMVR > Labs > Build <Lab Title>`.
- Common validator: `Tools > EMVR > Common > Validate Active Scene`.

## Evidence wording

End every Unity handoff with exactly what the user should report, for example:

```text
完成后请回复：
1. Test Runner 显示的 Passed/Failed 数量；
2. Console 红色错误数量；
3. 如果失败，附 Test Runner 和 Console 两张截图。
```

Do not ask only for `结果如何` or `告诉我是否通过` when a precise observation
can be named.
