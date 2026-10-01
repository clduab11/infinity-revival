# Infinity Revival: Complete Development Plan

**Planning baseline: September 29, 2026**

**Engine: Unity 6000.6.0f1, revision f7f8ed4d1e24**

## Executive recommendation

**Build a premium, offline action RPG around portrait touch combat, directed exploration, and cinematic one-on-one encounters.** Use Unity 6000.6.0f1 with URP, a small C# gameplay core, authored combat timelines, and persistent equipment progression.

The approved launch scope is:

| Area | Decision |
|---|---|
| Reference | Infinity Blade I’s interaction and progression strengths |
| Intellectual property | Original world, characters, writing, equipment, environments, audio, and visual identity |
| Campaign | Two regions, branching routes, four bosses |
| Regular enemies | Four archetypes, with elite variations |
| Player combat | One one-handed melee-and-shield style: swords, axes, maces/warhammers |
| Phones | Portrait, playable with one thumb |
| iPad | Landscape presentation using the same combat rules |
| PC | Landscape, mouse/keyboard and controller support |
| Business model | Premium purchase, complete offline campaign |
| Production | Founder, AI agents, and MCP automation |
| Asset strategy | Original work supplemented by selectively licensed assets |
| Performance | Sustained 60 FPS target; optional qualified 120 FPS mode |
| Expansion | Additional encounters, equipment, regions, and difficulty tiers through a versioned content pipeline |

**The critical production constraint is animation throughput.** The first substantial investment must prove a complete boss encounter on physical phones, including controls, animation, saving, and sustained performance.

This plan covers development through release. Chris explicitly authorized the initial GitHub upload, full README, and GPT-Image concept prototype on September 29, 2026. Purchases, store publication, deployments, and infrastructure changes still require their own authorization.

---


## Current state and source

Tasks 01 through 09 provide the application and graybox combat foundations.
Task 11 adds a separate original prototype pair and shared humanoid import
pipeline. Task 12 connects its presentation; Task 13 supplies validated content,
immutable admission and three weapon-family pilots. Hands-on Task 09 acceptance,
Task 10 qualification and final pair review remain deferred at the
[checkpoint after Task 12](../acceptance/post-task-12-checkpoint.md). The operator
authorized Task 13 before that outstanding checkpoint; physical execution remains
UNRUN. Task 14 loading scopes and leases are next. Saves, progression, campaign
production and platform releases remain planned. The [repository README](../../README.md)
links dated evidence and the [parallel development contract](../handoffs/parallel-development-contract.md)
defines the separate technical and art conversations.

This is the supplied September 29 development plan split into chapters for navigation. The full product/design scope is retained. Chapter 5 corrects the obsolete nested project layout to the verified single-root checkout. Original research citations are retained from the supplied plan, not newly verified for this publication.

Source document SHA-256: `0b997c408096dbf9c648f147589002dddefa9dfdee8e8c864976f2260a5c808e`.

## Chapters

| Chapter | Topic |
| --- | --- |
| 01 | [Research findings and design boundaries](development-plan/01-research-findings-and-design-boundaries.md) |
| 02 | [Product design and gameplay loops](development-plan/02-product-design-and-gameplay-loops.md) |
| 03 | [Combat specification](development-plan/03-combat-specification.md) |
| 04 | [AI and boss architecture](development-plan/04-ai-and-boss-architecture.md) |
| 05 | [Technical architecture and Unity project](development-plan/05-technical-architecture-and-unity-project.md) |
| 06 | [Unity systems and package decisions](development-plan/06-unity-systems-and-package-decisions.md) |
| 07 | [Saving, progression, economy, and rebirth](development-plan/07-saving-progression-economy-and-rebirth.md) |
| 08 | [Content production pipeline](development-plan/08-content-production-pipeline.md) |
| 09 | [Mobile performance and platform delivery](development-plan/09-mobile-performance-and-platform-delivery.md) |
| 10 | [Networking, expansion, and release operations](development-plan/10-networking-expansion-and-release-operations.md) |
| 11 | [Production roadmap](development-plan/11-production-roadmap.md) |
| 12 | [Codex development sequence](development-plan/12-codex-development-sequence.md) |
| 13 | [Acceptance plan and development readiness](development-plan/13-acceptance-plan-and-development-readiness.md) |
