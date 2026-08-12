# EM VR Labs Vision Memo

## Working Title
AI-Native VR Electromagnetics Learning Platform

## Core Narrative
EM VR Labs is evolving from a set of virtual reality demonstrations into a modular learning and research platform for electromagnetics education. The platform combines immersive visualization, grounded AI tutoring, agent-assisted lab authoring, and structured learner analytics in one reusable system.

The motivation is straightforward:
- Electromagnetic theory is foundational but abstract.
- Students struggle with invisible three-dimensional phenomena such as field distributions, superposition, polarization, and wave-material interaction.
- VR can make these concepts visible and interactive, but conventional VR labs remain largely scripted and non-adaptive.
- AI can make the environment responsive to individual learners, but only if the tutor is grounded in real lab state rather than generic text prompts.

The project therefore aims to build an embodied learning environment in which:
- students manipulate EM objects and observe the resulting physical phenomena in VR,
- AI tutors monitor learner state and provide timely, pedagogically appropriate support,
- new labs can be developed faster through modular interfaces and agent-assisted workflows,
- structured telemetry supports controlled studies on learning, cognition, and instructional design.

## Why This Work Matters
This work has value at four levels.

### 1. Instructional Impact
The platform supports conceptual understanding of difficult EM topics by turning invisible fields and wave behavior into observable and interactive phenomena.

### 2. Engineering Impact
A modular architecture reduces duplication, improves maintainability, and allows different contributors to work on runtime systems, lab content, AI assist, and analytics in parallel.

### 3. AI-in-Education Impact
The project advances beyond generic tutoring by grounding AI in embodied experimental context: step state, object state, recent actions, and local validation signals.

### 4. Research Impact
The platform can generate structured evidence about when VR helps, when AI helps, what kinds of interventions are effective, and how learner interaction patterns relate to concept gain, confusion, recovery, and transfer.

## Long-Term Vision
The long-term goal is not merely to maintain several VR labs. It is to establish a reusable infrastructure for AI-enhanced STEM experimentation and evidence-based education research.

In that future system:
- labs are built against a common framework,
- AI tutoring is configurable, grounded, and testable,
- lab development agents help author and review new labs,
- learner interaction traces become analyzable research artifacts,
- the platform supports both classroom deployment and publishable education studies.

## Strategic Thesis
The strongest version of this project is:

> a modular VR electromagnetics platform that unifies domain visualization, pedagogy, AI intervention, agent-assisted authoring, and research-grade telemetry.

That framing should guide all technical and organizational decisions.

## Immediate Program Goals
1. Refactor the current codebase into a modular platform with clear separation between shared systems and lab-specific content.
2. Generalize the current AI assist layer into a reusable tutor framework rather than a scene-specific add-on.
3. Build agent workflows that can scaffold, review, and test new labs inside the modular framework.
4. Prepare the platform for controlled educational studies with stable instrumentation and analyzable outputs.

## Success Criteria
The project will be considered successful if it achieves all of the following:
- at least one existing lab is cleanly migrated into the modular architecture,
- at least one new lab is developed using the modular framework and agent support,
- the AI tutor can issue grounded reactive and proactive interventions,
- the telemetry layer captures replayable learner traces,
- a pilot study can compare learning support conditions in a technically valid way.

# EM VR Labs Vision Memo

## Working Title
AI-Native VR Electromagnetics Learning Platform

## Core Narrative
EM VR Labs is evolving from a set of virtual reality demonstrations into a modular learning and research platform for electromagnetics education. The platform combines immersive visualization, grounded AI tutoring, agent-assisted lab authoring, and structured learner analytics in one reusable system.

The motivation is straightforward:
- Electromagnetic theory is foundational but abstract.
- Students struggle with invisible three-dimensional phenomena such as field distributions, superposition, polarization, and wave-material interaction.
- VR can make these concepts visible and interactive, but conventional VR labs remain largely scripted and non-adaptive.
- AI can make the environment responsive to individual learners, but only if the tutor is grounded in real lab state rather than generic text prompts.

The project therefore aims to build an embodied learning environment in which:
- students manipulate EM objects and observe the resulting physical phenomena in VR,
- AI tutors monitor learner state and provide timely, pedagogically appropriate support,
- new labs can be developed faster through modular interfaces and agent-assisted workflows,
- structured telemetry supports controlled studies on learning, cognition, and instructional design.

## Why This Work Matters
This work has value at four levels.

### 1. Instructional Impact
The platform supports conceptual understanding of difficult EM topics by turning invisible fields and wave behavior into observable and interactive phenomena.

### 2. Engineering Impact
A modular architecture reduces duplication, improves maintainability, and allows different contributors to work on runtime systems, lab content, AI assist, and analytics in parallel.

### 3. AI-in-Education Impact
The project advances beyond generic tutoring by grounding AI in embodied experimental context: step state, object state, recent actions, and local validation signals.

### 4. Research Impact
The platform can generate structured evidence about when VR helps, when AI helps, what kinds of interventions are effective, and how learner interaction patterns relate to concept gain, confusion, recovery, and transfer.

## Long-Term Vision
The long-term goal is not merely to maintain several VR labs. It is to establish a reusable infrastructure for AI-enhanced STEM experimentation and evidence-based education research.

In that future system:
- labs are built against a common framework,
- AI tutoring is configurable, grounded, and testable,
- lab development agents help author and review new labs,
- learner interaction traces become analyzable research artifacts,
- the platform supports both classroom deployment and publishable education studies.

## Strategic Thesis
The strongest version of this project is:

> a modular VR electromagnetics platform that unifies domain visualization, pedagogy, AI intervention, agent-assisted authoring, and research-grade telemetry.

That framing should guide all technical and organizational decisions.

## Immediate Program Goals
1. Refactor the current codebase into a modular platform with clear separation between shared systems and lab-specific content.
2. Generalize the current AI assist layer into a reusable tutor framework rather than a scene-specific add-on.
3. Build agent workflows that can scaffold, review, and test new labs inside the modular framework.
4. Prepare the platform for controlled educational studies with stable instrumentation and analyzable outputs.

## Success Criteria
The project will be considered successful if it achieves all of the following:
- at least one existing lab is cleanly migrated into the modular architecture,
- at least one new lab is developed using the modular framework and agent support,
- the AI tutor can issue grounded reactive and proactive interventions,
- the telemetry layer captures replayable learner traces,
- a pilot study can compare learning support conditions in a technically valid way.

# EM VR Labs Team Roles And Methodology

## 1. Team Structure
The current team has one supervising faculty lead, one master student leading the research thread, and three undergraduates supporting development.

The recommended working model is:
- faculty lead: strategy, technical review, research framing, and milestone oversight
- master student: technical integration lead and research lead
- undergraduate A: modular platform redesign
- undergraduate B: lab development agent workflow
- undergraduate C: agent-assisted new lab implementation and testing

## 2. Role Definition

### Faculty Lead
Responsibilities:
- set strategic direction
- review architecture and research design
- maintain alignment between software work and publishable outcomes
- resolve scope conflicts across workstreams

### Master Student
Role:
Platform and research thread lead

Responsibilities:
- own the architecture contracts across runtime, pedagogy, assist, telemetry, domain, and research
- coordinate the modularization and agent workstreams
- define the experiment methodology and evaluation metrics
- ensure that system changes support future controlled studies
- lead documentation, integration, and research reporting

Expected outputs:
- architecture specification
- data schema and experiment design
- integration plan across all workstreams
- pilot evaluation package

### Undergraduate A
Role:
Modular redesign engineer

Responsibilities:
- refactor common systems out of scene-specific scripts
- define reusable lab package structure
- migrate one existing lab into the modular framework
- prepare migration guidance for additional labs

Expected outputs:
- reorganized package structure
- runtime/pedagogy integration for the electrostatics pilot
- lab spec implementation support

### Undergraduate B
Role:
Agent workflow engineer

Responsibilities:
- define agent input/output contracts
- build the authoring agent workflow
- support scaffold generation for new labs
- design review checkpoints for generated outputs

Expected outputs:
- agent authoring pipeline
- lab generation templates
- generated scaffold for one target lab

### Undergraduate C
Role:
Agent review and lab implementation engineer

Responsibilities:
- build review/test automation around generated lab artifacts
- use the agent workflow to implement at least one new lab
- validate telemetry and assist integration in the new lab

Expected outputs:
- review/test agent pipeline
- one agent-assisted new lab implementation
- bug and test reports

## 3. Working Methodology

### 3.1 Platform-First Rule
No major new lab should be built outside the shared framework once the core contracts are defined. Otherwise technical debt will continue to grow.

### 3.2 Contract-First Rule
Before writing new lab code, the team must define:
- lab specification
- step graph
- object model
- telemetry fields
- assist hooks
- validation rules

### 3.3 Grounded-AI Rule
No AI feature should be added without structured state grounding. The tutor should operate on object states, step states, recent actions, and diagnostics, not only free-text prompts.

### 3.4 Research-Ready Logging Rule
All deployable labs should emit consistent event logs. If an interaction cannot be logged in a reusable format, the platform is not yet mature enough for controlled study use.

### 3.5 Reproducibility Rule
Every new lab must ship with:
- clear lab spec
- rubric definition
- telemetry mapping
- assist mapping
- test checklist

## 4. Cross-Team Coordination

### Weekly Synchronization
Recommended weekly structure:
- one architecture and integration meeting led by the master student
- one engineering check-in for modularization and agent work
- one short research-methodology review every two weeks

### Shared Review Artifacts
Every workstream should maintain:
- current scope
- blocked items
- decisions made
- pending interface changes
- risks to milestone timing

### Integration Policy
Changes affecting shared contracts must be reviewed centrally before other contributors build on them.

## 5. Deliverable Sequence
The work should proceed in this order:
1. architecture and platform contracts
2. electrostatics modular refactor
3. assist generalization
4. authoring agent
5. review/test agent
6. new lab through the modular framework and agents
7. pilot research deployment

## 6. Quality Criteria

### For Modularization
- shared systems are clearly separated from lab content
- step logic is configurable rather than hardcoded
- all common services are reusable across at least two labs

### For Agents
- generated outputs follow the lab-spec contract
- review/test pipeline catches missing hooks and broken assumptions
- agent usage reduces manual development friction rather than adding overhead

### For Research Readiness
- telemetry is stable and interpretable
- AI interventions are logged and attributable
- study conditions can be configured cleanly

## 7. Team Message
The objective is not simply to finish more VR scenes. The objective is to build a sustainable platform that allows:
- better labs,
- faster lab development,
- stronger AI support,
- and credible education research.

That is the standard the team should work toward.

# EM VR Labs Milestones And Roadmap

## Milestone Summary

| Milestone | Focus | Primary Owner | Key Deliverables |
|---|---|---|---|
| M1 | Platform specification | Master student | Architecture memo, contracts, lab-spec schema |
| M2 | Electrostatics modular refactor | Undergraduate A | Refactored pilot lab in shared framework |
| M3 | Assist generalization | Master student | Scene-agnostic assist interfaces and provider abstraction |
| M4 | Authoring agent prototype | Undergraduate B | Agent workflow that generates lab specs and scaffolds |
| M5 | Review/test agent prototype | Undergraduate C | Agent workflow for review, testing, and issue detection |
| M6 | New lab via agent workflow | B + C + A | One new lab built through modular framework and agents |
| M7 | Study infrastructure | Master student | Experiment conditions, telemetry export, metrics |
| M8 | Pilot evaluation | Master student | Small pilot study and technical validation report |

## Detailed Milestones

### M1. Platform Specification
Goals:
- define architectural boundaries
- freeze naming and contracts
- define the lab package format
- define telemetry and assist interfaces

Deliverables:
- technical blueprint
- event schema
- lab-spec schema
- physics backend interface definition
- AI assist I/O contract

Exit criteria:
- all team members can implement against the same interfaces

### M2. Electrostatics Modular Refactor
Goals:
- use electrostatics as the migration pilot
- separate shared services from scene logic
- make the current assist layer run through the new contract

Deliverables:
- migrated electrostatics lab
- reusable runtime and pedagogy services
- documented migration lessons

Exit criteria:
- electrostatics runs without depending on hardcoded lab-only assist assumptions

### M3. Assist Generalization
Goals:
- remove electrostatics-only assumptions from assist logic
- formalize intervention policies
- abstract the AI provider layer

Deliverables:
- generalized step and object interfaces
- provider abstraction for Dify or future models
- common intervention action format

Exit criteria:
- assist layer can be reused by at least one additional lab with minimal custom code

### M4. Authoring Agent Prototype
Goals:
- generate structured lab artifacts, not just scripts
- reduce manual specification burden for new labs

Deliverables:
- prompt and workflow design
- lab-spec generator
- object/step/rubric scaffold generator
- implementation scaffold output

Exit criteria:
- agent can generate a valid first-pass lab package proposal

### M5. Review/Test Agent Prototype
Goals:
- catch design and implementation errors earlier
- validate generated labs against the framework contract

Deliverables:
- review checklist workflow
- test-case generator
- issue-report generator

Exit criteria:
- agent can detect missing telemetry hooks, missing rubric links, and broken flow assumptions

### M6. New Lab Through Agent Workflow
Goals:
- prove the end-to-end platform value
- develop one new lab using the modular framework and agents

Candidate lab topics:
- arbitrary elliptical polarization
- reflection/transmission at a discontinuity
- layered media and standing waves

Deliverables:
- completed new lab
- authoring record showing how the agent contributed
- comparison of manual vs agent-assisted workflow

Exit criteria:
- the new lab runs in the framework, logs events, and supports assist hooks

### M7. Study Infrastructure
Goals:
- prepare the platform for controlled educational studies

Deliverables:
- participant/study condition model
- core metrics list
- exportable data format
- anonymization workflow

Recommended study conditions:
- traditional or non-VR baseline
- VR-only
- VR + direct-answer AI
- VR + Socratic grounded AI

Exit criteria:
- a valid pilot can be run without ad hoc instrumentation changes

### M8. Pilot Evaluation
Goals:
- validate the technical pipeline
- test the intervention logic and measurement plan

Deliverables:
- pilot deployment
- system behavior review
- telemetry quality review
- initial learning-process findings

Exit criteria:
- the team can justify a larger controlled study

## Suggested 12-Week Roadmap

### Weeks 1-2
- finalize architecture
- define contracts and schemas
- assign detailed responsibilities

### Weeks 3-4
- refactor electrostatics into the modular framework
- review assist integration points

### Weeks 5-6
- generalize assist and telemetry
- remove scene-specific assumptions where feasible

### Weeks 7-8
- build authoring agent workflow
- build review/test agent workflow

### Weeks 9-10
- create one new lab through the modular framework and agent support
- validate logging and assist behavior

### Weeks 11-12
- prepare pilot study deployment
- run technical pilot and evaluate instrumentation

## Key Risks
- modular refactor delayed by dependence on monolithic legacy scripts
- agent outputs not matching the framework contract
- telemetry inconsistency across labs
- AI intervention quality not stable enough for study use
- overbuilding model workflows before runtime interfaces are stable

## Risk Controls
- freeze contracts before broad implementation
- use electrostatics as the migration template
- validate every lab against the same telemetry and assist checklist
- keep the first tutor narrow and grounded
- postpone large studies until instrumentation is stable
