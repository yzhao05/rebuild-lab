# Build experience

This directory contains normalized construction knowledge that is safe and
recommended for blind rebuilds. Gate 0 reads its lesson files recursively while
excluding files named `README.md`.

Allowed content includes verified cross-Lab constraints such as required API
initial values, Unity lifecycle placement, assembly/package ordering, input
ownership, collider/raycast behavior, scene-builder failure rules, and visual
acceptance checks. It must exclude old Lab answers, exact coordinates, class
names, object IDs, scene wiring, rubric logic, and copied implementation code.

`pre-workflow/` contains lessons extracted from work completed before Gate 10
existed. Root-level files in the parent `rebuild-lessons/` directory are later
Gate 10 outputs.
