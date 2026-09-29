# 7. Saving, progression, economy, and rebirth

## 7.1 Save envelope

Use one versioned envelope containing:

| Component | Contents |
|---|---|
| Profile | Level, XP, attributes, equipment ownership, upgrades, mastery |
| Expedition | ID, seed, selected tier, route state, completed nodes |
| Encounter checkpoint | Encounter ID, entry loadout/resources, content revision, reward seed |
| Reward ledger | Recently committed encounter transactions |
| Permanent claims | First-clear tier rewards and permanent unlocks |
| Settings | Controls, accessibility, audio, performance |
| Header | Schema version, generation, application/content versions, integrity data |

JSON serialization uses explicit DTOs. Disable polymorphic type-name loading.

### Storage behavior

- One serialized writer.
- Two recoverable save generations.
- Write the inactive generation through a temporary file.
- Flush, close, and validate before promoting it.
- Validate payload integrity before deserialization.
- Load the newest valid compatible generation.
- Preserve the previous valid generation during migration.
- Never silently replace a corrupt or unsupported profile with a new one.
- Preserve unknown content records for recovery; equip a safe fallback when necessary.

Checksums detect corruption. They do not make an offline economy cheat-proof.

## 7.2 Transaction boundaries

Create the pre-fight checkpoint **before** entering combat.

A victory commits all of the following together:

- Currency.
- Equipment grants.
- Character XP.
- Equipment mastery.
- Level changes.
- Node completion.
- Route advancement.
- Reward claim identifier.

Only then display the committed reward result.

Reward identity is based on expedition ID and encounter ID. Nodes cannot be replayed for another reward within the same expedition.

A storage failure leaves the game on a recoverable result screen with retry available. It does not continue with uncommitted purchases or progression.

Backgrounding saves the latest envelope where possible. It does not rebuild permanent state from an old encounter checkpoint.

## 7.3 Interruption policy

| Event | Result |
|---|---|
| Pause while process remains alive | Suspend the encounter; resume with fair warning |
| Process termination during combat | Restart that encounter from its pre-fight checkpoint |
| Termination after victory commit | Load the committed completed node and rewards |
| Termination before victory commit | Replay the encounter; no partial reward grant |
| Defeat | Commit a new expedition ID and reset the route |
| Storage corruption | Recover prior generation or show recovery UI |
| Unknown newer save schema | Preserve files and refuse destructive downgrade |

App termination can be used to retry an encounter. Accept that tradeoff for a premium offline game.

## 7.4 Equipment and mastery

Launch equipment budget:

- 12 sword definitions.
- 8 shield definitions.
- 4 helmet definitions.
- 4 armor definitions.
- 6 talisman definitions.

That is **34 equipment definitions**, not 34 entirely unrelated character production pipelines.

Use one owned record per equipment definition:

- Ownership.
- Upgrade level.
- Mastery progress.
- Mastery claimed flag.

Duplicate drops convert to currency. Mastery is keyed by equipment definition, preventing duplicate copies from repeatedly awarding first-mastery progression.

Each item has three upgrade levels. No randomized affix system is required at launch.

## 7.5 Character progression

- Character level cap: 20.
- One attribute point per level after the first.
- Four attributes: health, power, guard, and Focus generation.
- Maximum ten allocated points in any one attribute.
- Free reallocation at the refuge.
- Base encounter XP always applies.
- Mastery awards a one-time XP bonus and collection completion.

Initial reward values:

- Regular encounter: 50 XP.
- Boss: 150 XP.
- Each equipped unmastered item receives the encounter’s mastery XP.
- Character XP is not multiplied by the number of equipped slots.

Final thresholds and prices are tuned from encounter throughput and progression simulations before Beta.

## 7.6 Economy

Use two currencies:

| Currency | Sources | Uses |
|---|---|---|
| Ordinary currency | Encounter victories and duplicate equipment | Equipment purchases and upgrades |
| Legacy seals | One-time final-boss clear at each tier | Permanent legacy perk unlocks |

No crafting-material inventory is required for launch.

Keep the economy finite and inspectable:

- Fixed upgrade limits.
- Explicit reward tables.
- Guaranteed milestone equipment.
- No purchase necessary beyond the game itself.
- No escalating repair bill after defeat.
- No loss of previously committed ordinary currency on defeat.

## 7.7 Rebirth contract

Defeat and voluntary rebirth are separate operations.

| State | Defeat | Voluntary rebirth |
|---|---|---|
| Character level and attributes | Keep | Keep |
| Equipment and upgrades | Keep | Keep |
| Mastery | Keep | Keep |
| Committed currency | Keep | Keep |
| Permanent unlocks and settings | Keep | Keep |
| Current route completion | Reset | Reset |
| Encounter resources and temporary effects | Reset | Reset |
| Difficulty tier | Keep selected tier | Select next unlocked tier |
| Expedition ID and seed | Replace | Replace |
| Claimed tier rewards | Keep | Keep |

The first final-boss victory at each tier grants one Legacy seal. The claim key is permanent and independent of expedition IDs.

Grant the seal at victory commit, not when the player later chooses rebirth.

Six legacy perks are available across tiers 0–5. Only one may be equipped:

- Small health increase.
- Small power increase.
- Guard increase.
- Focus generation increase.
- Currency bonus.
- Mastery bonus.

This bounds accumulated power while preserving long-term choice.

---


[Return to development plan](../development-plan.md)
