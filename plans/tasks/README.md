# Task Files

This directory contains focused implementation task files derived from project plans, milestones, or approved changes.

## Purpose

A task file is the persistent handoff between planning and implementation:

>**ChatGPT / planning → task file → VS Code + GitHub Copilot → implementation and validation**

The task file should contain the information needed to implement one discrete piece of work. It should not duplicate repository-wide engineering rules; those remain authoritative in the existing `.github` instructions and agents.

## Creating a Task

1. Start with `task-template.md`.
2. Give the task a unique milestone/sequence identifier, such as `M12-001`.
3. Describe the objective and requirements precisely.
4. Identify relevant files or areas when known.
5. Define concrete acceptance criteria.
6. Specify the expected testing and documentation work.
7. Record important constraints or architectural decisions.
8. Keep the task focused enough to implement and validate as one unit of work.

## Naming

Use:

`<milestone>-<number>.md`

Examples:

- `M12-001.md`
- `M12-002.md`

## Implementation

In VS Code, GitHub Copilot should use the task file together with the repository's existing `.github` instructions and agents.

A typical implementation request is:

> Implement `plans/tasks/M12-001.md` according to the repository instructions.

The task file is the implementation specification; the existing repository instructions remain the source of truth for engineering practices, testing, review, and delivery workflow.

## Implementation summaries

After completing a task, the implementation agent creates an implementation summary in:

`plans/tasks/reports/`

The filename uses the task identifier:

`plans/tasks/reports/<TASK-ID>-implementation-summary.md`

For example:

`plans/tasks/reports/M12-001-implementation-summary.md`

The implementation summary is the formal feedback from the implementation agent to the planning/architecture process.

It records:

- what was implemented;
- which files changed;
- tests and validation performed;
- acceptance criteria results;
- documentation changes;
- deviations from the task;
- remaining issues, risks, or follow-up work.

Use `reports/implementation-summary-template.md` as the starting point for implementation summaries.

The implementation summary can be supplied to ChatGPT for implementation review without requiring the full implementation conversation to be reproduced.

## Task lifecycle

1. ChatGPT and the product owner define the task.
2. The task is saved under `plans/tasks/`.
3. VS Code + GitHub Copilot implements the task using the task file and repository instructions.
4. The implementation agent creates the implementation summary under `plans/tasks/reports/`.
5. The implementation summary is supplied to ChatGPT for review when architectural or implementation review is required.
6. Follow-up work is captured as a new or updated task.
