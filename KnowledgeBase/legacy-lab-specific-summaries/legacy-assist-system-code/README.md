# Conditional Assist-system archive

This directory contains generic Assist transport, DTO, session, selection,
logging, UI, validation, proactive, and outcome candidates. It is Category B,
not mandatory reuse and not a source of target-Lab requirements.

Before inspecting a file, first check whether the validated Common package
already owns that responsibility. If Common owns it, use Common and do not read
or adapt this archive. Otherwise:

1. select the smallest generic responsibility;
2. verify that the file contains no target formula, answer, object ID, step
   order, scene wiring, credential, or Lab-specific class dependency;
3. copy only an adapted version into the current Lab's declared output;
4. record provenance and tests in
   `BuildEvidence/<lab_id>/reuse-report.md`;
5. leave this archive unchanged.

The old domain-specific binder and setup guide are retained only in the
restricted legacy archive and are unavailable during blind rebuilds.
