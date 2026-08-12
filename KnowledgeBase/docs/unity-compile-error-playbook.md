# Unity compile-error playbook for blind Lab builds

Use this playbook before writing Lab-specific C# and whenever Unity reports a
compiler error. It is reusable debugging knowledge, not a source of target Lab
answers and not permission to inspect Gold Reference code.

## Required compile checkpoints

1. Gate 4: compile only the generated runtime/editor scaffold. Clear the
   Console, force one clean recompile, and require zero compiler errors.
2. Gate 5: add only one smallest end-to-end domain slice. Clear and recompile;
   require both domain evidence and zero compiler errors.
3. Gate 6: compile the integrated Lab, build the scene, run Common validation,
   then inspect the cleared Console and Game View.
4. Treat prior compile evidence as stale after any Lab-specific C# edit in the
   current gate.

Do not generate more code while a checkpoint is failing.

## Diagnose the earliest root error

1. Stop Play Mode.
2. Clear the Console.
3. Trigger one recompile and wait for it to finish.
4. Capture the earliest error and the first 20 relevant messages in order.
5. Identify the owning file, assembly, package, and API version.
6. Fix one root cause, then Clear and recompile again.

Later missing-type and missing-namespace messages are commonly cascades. Do not
count the number of messages as the number of independent defects.

## Ownership and allowed response

| Earliest error belongs to | Typical evidence | Required response |
|---|---|---|
| UPM/package resolution | Missing `package.json`, manifest/lock mismatch | Stop and report a protected-foundation defect |
| Common/shared assembly | Common asmdef fails or source itself does not compile | Stop and report a protected-foundation defect |
| Lab-specific asmdef | Missing reference or Editor/runtime assembly mix | Fix the current Lab output and recompile |
| Lab-specific C# | Syntax, type, event, overload, accessibility, or lifecycle error | Fix the current Lab output and recompile |
| Unity/XRI API version | Real installed declaration differs from the call | Correct the Lab call after recording the actual signature; report only if the required package itself is defective |
| Scene/runtime behavior | Compile passes but Play Mode throws | Fix current Lab code or report protected ownership using the same boundary rule |

The Skill does not prevent correction of current Lab output. It prevents an
Agent rebuilding a Lab from silently editing Common, Packages, Policies,
ProjectSettings, shared assets, Skills, or workflow tools.

## API signature audit

Before using a Common, Unity, or XRI member, record:

```text
API responsibility:
Source path or installed package/version:
Assembly/asmdef:
Namespace and type:
Exact constructor/method/property/event signature:
Lifecycle or threading constraint:
Planned Lab call site:
```

Read the real source or package declaration. Do not infer overloads, event
delegate shapes, serialized fields, or lifecycle behavior from names, memory,
old tutorials, or a different XRI version.

## Recurring failure patterns

### Namespace or type flood

Find the first assembly or package error. If Common failed to resolve, preserve
otherwise-correct `EMVR.Common` calls and fix/report the owning dependency
instead of rewriting every later call.

### Assembly definition mismatch

Check whether runtime code references an Editor-only assembly, whether the Lab
asmdef names the required Common/XRI assembly, and whether duplicate asmdefs
split types unexpectedly. Keep Editor builders under an Editor assembly.

### Guessed API or wrong package version

Open the declaration installed in this Builder Pack, record its signature, and
change only the Lab call. Do not rely on an online example or an API from a
different Unity/XRI release.

### Unity native-backed object created too early

Do not create `MaterialPropertyBlock`, `Material`, `Mesh`, `Texture2D`,
`RenderTexture`, or similar Unity native-backed objects in a `MonoBehaviour`
constructor or instance-field initializer. Initialize in `Awake`, `OnEnable`,
or a guarded lazy method and check the result before use.

### Event/delegate mismatch

Read the exact event type and callback signature. Verify parameter order,
generic arguments, accessibility, and unsubscribe behavior. Do not add overloads
until the installed declaration is known.

### Editor/runtime boundary error

Keep `UnityEditor` APIs out of runtime assemblies and player code. Put scene
builders and menu commands under `Assets/Editor` or a matching Editor-only
asmdef.

### Duplicate generated type

Search current Lab output for duplicate class names, namespaces, menu paths,
and stale generated scripts before renaming APIs or assemblies.

### Builder reports success with errors

Treat Error, Exception, or Assert during scene construction as failure. Abort
before saving, discard the partial scene, and print success only after clean
validation and save. A success log does not override an earlier Console error.

## Incident entry template

Add a new reusable entry only after the root cause is verified:

```text
Error signature:
Unity/XRI/Common versions:
Earliest root error:
Owning layer:
Verified cause:
Allowed fix location:
Fix pattern in prose:
Forbidden workaround:
Prevention check and Gate:
Evidence that the fix worked:
```

Store general failure patterns here. Keep target formulas, expected answers,
rubric logic, and old Lab implementation details out of this knowledge base.

## Gate 10 capture and later reuse

After Gate 9 user acceptance, use Gate 10 to create the run-specific record at
`BuildEvidence/retrospectives/<lab_id>.md` and the normalized reusable lesson at
`KnowledgeBase/rebuild-lessons/<lab_id>.md`. Include only verified causes and
prevention checks. At Gate 0 of every later rebuild, review all prior lesson
files and record their applicability in
`LabSpecs/<lab_id>/prior-lessons-review.md` before writing Lab code.
