# Plans Directory

This directory is for project planning documents. Plans are created to outline the work required to complete a feature or a set of tasks.

## The Role of Plans

Plans are used to:

- Define the scope of work.
- Break down work into smaller, manageable tasks.
- Estimate the effort required for each task.
- Identify dependencies between tasks.
- Define success criteria and acceptance tests.

### Examples

- Small feature plan: `plans/examples/plan-small.md`

## Current Planning Documents

- [Roadmap](ROADMAP.md) — Milestone roadmap and sequencing
- [ATLAS Foundation Plan](atlas-foundation-plan.md) — Foundational project plan
- [M12 Plan](M12-plan.md) — UI restyling and usability improvements
- [Plan Template](plan-template.md) — Template for new plans

## Tasks

Implementation tasks derived from plans are maintained in `plans/tasks/`.

Task files are the persistent handoff between planning and implementation. They capture the specific objective, requirements, relevant areas, acceptance criteria, testing, documentation, and constraints needed to implement a discrete change.

Use `plans/tasks/task-template.md` as the starting point for new task files.

Task files should:

- Be small and implementation-focused.
- Reference the relevant plan or milestone when applicable.
- Contain enough context for implementation without duplicating repository-wide engineering guidance.
- Use the repository's existing `.github` instructions and agents for engineering standards, testing, review, and workflow.
- Be updated when requirements or acceptance criteria change during implementation.

### Naming

Use the format:

`plans/tasks/<milestone>/<number>.md`

For example:

`plans/tasks/M12/M12-001.md`

## Archive

Completed plans are moved from this directory into the `plans/archive` directory.
