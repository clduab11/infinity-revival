# 4. AI and boss architecture

## 4.1 AI implementation

Use a hierarchical state machine with authored pattern selection.

Do not introduce behavior-tree middleware, machine learning, runtime language models, or NavMesh navigation for anchored duels.

Each opponent selects from a weighted pattern deck using:

- Current phase.
- Pattern cooldowns.
- Recent pattern history.
- Player resource state exposed through approved gameplay observations.
- Difficulty tier.
- Encounter seed.

AI may react to completed player actions. It cannot inspect an unfinished swipe or change an already committed attack to defeat the selected response.

## 4.2 Enemy classes

| Class | Implementation |
|---|---|
| Regular | Small pattern deck, one phase, clear teaching purpose |
| Elite | Existing archetype plus additional sequence combinations and modifiers |
| Boss | Multiple phases, authored transitions, signature mechanics, bespoke presentation |

Four regular archetypes:

1. Sword guard: basic direction recognition.
2. Shield guard: patient defense and recovery punishment.
3. Polearm guard: safe dodge-side recognition.
4. Hammer guard: guard conservation and heavy attacks.

Elite variants reuse the same skeleton and animation family.

## 4.3 Four-boss campaign

| Boss role | Primary lesson | Production constraint |
|---|---|---|
| Armored captain | Guard management and basic parry chains | Shared humanoid skeleton |
| Polearm champion | Dodge direction and delayed strikes | Shared humanoid skeleton |
| Counter-fencer | Readable feints and varied recovery | Shared humanoid skeleton |
| Humanoid weather-engine guardian | Combine established mechanics across phases | Humanoid rig with rigid mechanical attachments |

All bosses use original silhouettes, equipment, animation assembly, effects, and narrative roles.

**Launch excludes bespoke quadruped, winged, and multi-limbed skeletons.** Those would materially change the production estimate.

## 4.4 Boss authoring contract

A boss definition contains:

- Stable content ID.
- Combatant statistics.
- Phase definitions.
- Pattern decks.
- Attack definitions.
- Transition thresholds.
- Introduction and finisher presentation.
- Encounter camera profile.
- Reward table.
- Accessibility tell variants.
- Performance classification.

Phase transitions:

- Trigger once.
- Wait for a safe boundary unless explicitly authored otherwise.
- Cannot override death.
- Cannot create unavoidable damage.
- Do not change the active content revision mid-encounter.

## 4.5 Difficulty

Launch has difficulty tiers **0 through 5**.

Initial scaling:

- Health: +12% per tier, maximum +60%.
- Damage: +8% per tier, maximum +40%.
- Currency rewards: +10% per tier, maximum +50%.
- New combinations use existing approved attacks.
- Defensive windows remain unchanged.
- Telemetry and playtesting can reduce these values before release.

Accessibility assists are separate settings: wider parry windows, reduced incoming damage, stronger tell indicators, and reduced effects. There are no competitive leaderboards at launch.

---


[Return to development plan](../development-plan.md)
