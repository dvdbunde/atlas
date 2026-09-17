# README

**N.B.** This README.md is located at `docs/README.md` and it is displayed in preference to the [README.md in the project root](../README.md). The [README preference order](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes) is described in the GitHub docs.

## Documents Directory

This directory contains all project-related documentation, organized into subdirectories based on its purpose.

## Index

- [Architecture](architecture/README.md)
- [ADRs (Architecture Decision Records)](ADRs/README.md)
- [Design](design/README.md)
- [Engineering](engineering/README.md)
- [PRDs (Product Requirement Documents)](PRDs/README.md)
- [Runbooks](runbooks/) — Environment setup and operational guidance
- [EARS Specifications](EARS-specs/README.md) — Structured requirements
- [Notes](notes/) — Backlog and working notes

## Writing Documentation Using AI

There are four examples of how to influence and use AI to write documentation in this repository:

- [Custom agents](../.github/agents/README.md) — Specialized AI behaviors for tasks like development, testing, and code review. As of October 2025, GitHub renamed "Chat Modes" to "Agents".
- [Custom chat modes](../.github/chatmodes/README.md) *(deprecated)* — Preset conversational configurations that shape Copilot's behavior and tone during interactive sessions and document generation.
- [Instructions](../.github/instructions/docs.instructions.md) — Repository-specific rules and constraints (coding standards, workflow, style) that the AI must follow when producing content.
- [Prompts](../.github/prompts/write-docs.prompt.md) — Reusable prompt templates for generating consistent artifacts (e.g., ADRs, docs, PRDs). See also [write-adr.prompt.md](../.github/prompts/write-adr.prompt.md) and [write-prd.prompt.md](../.github/prompts/write-prd.prompt.md).

## Documentation Status

Documentation in this repository serves several purposes: current-state reference, architectural decision records, product and engineering requirements, operational guidance, and historical planning. When a document describes a milestone or proposed capability, its scope and status should be interpreted from the document itself; current-state claims should be reconciled with the implementation and the active roadmap.
