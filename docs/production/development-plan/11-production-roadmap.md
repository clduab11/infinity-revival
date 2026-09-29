# 11. Production roadmap

## 11.1 Staffing and estimation model

Assume one founder with AI tooling and selectively licensed production inputs.

Use **founder hours**, not agent counts, to measure capacity. AI output still consumes review, integration, art direction, and device-validation time.

For planning only, use 25 focused founder hours per week. The initial envelope is approximately **900–1,350 hours**, plus 25% contingency. That corresponds to roughly **11–16 months** at that capacity.

This is a provisional capacity model. Reforecast after the representative boss establishes actual animation and encounter-authoring throughput.

## 11.2 Milestones

| Milestone | Deliverables | Success criteria | Main technical risks |
|---|---|---|---|
| **Prototype** | Graybox duel, portrait controls, combat clock, four-direction defense, basic enemy patterns, interruption handling | One-thumb play works on physical phones; recorded inputs behave consistently across presentation rates; tells remain readable | Gesture ambiguity, thumb reach, input latency |
| **Vertical Slice** | One finished boss, short exploration route, representative equipment, save/reward/rebirth loop, final-quality art sample, audio, Addressables | Complete experience runs offline; save interruption cases pass; sustained 60 FPS demonstrated; boss production effort measured | Animation contact, thermal limits, persistence defects |
| **Alpha** | Both regions, four bosses, four archetypes, equipment catalog, all progression and difficulty tiers | Entire campaign playable from a new profile to ending and rebirth; no missing required systems | Content throughput, repeated encounters, balance |
| **Beta** | Content complete, device profiles, migrations, accessibility, store assets, licenses, release builds | Device matrix passes; no known critical progression/save defects; upgrade and clean-install paths pass | Driver variation, memory peaks, store/toolchain changes |
| **Launch** | Signed mobile releases, support process, release archive, compatible save/content packages; Steam candidate | Release checklist passes; recovery procedures documented; operational ownership assigned | Submission problems, undiscovered device failures |

Indicative effort allocation:

- Prototype: 4–6 founder weeks.
- Vertical Slice: 8–12.
- Alpha: 16–24.
- Beta: 6–8.
- Launch preparation: 2–4.

## 11.3 Platform sequence

1. Portrait phone combat establishes the interaction baseline.
2. iPad framing and layout enter during the slice.
3. Android device qualification runs alongside mobile production.
4. Steam controls and presentation enter after slice acceptance.
5. Mobile release leads the schedule; Steam publication follows its own acceptance gate.

No platform may be declared supported solely because it builds.

## 11.4 Stop and reduce scope when

- One-thumb input remains unreliable after the prototype iteration budget.
- The representative boss exceeds the sustained device budget.
- A new boss requires an additional skeleton family.
- Save failures remain unresolved.
- Measured content throughput exceeds the production envelope.
- Supporting another device family requires disproportionate maintenance.

Reduce cosmetic variation and optional route content before weakening combat readability or save integrity.

---


[Return to development plan](../development-plan.md)
