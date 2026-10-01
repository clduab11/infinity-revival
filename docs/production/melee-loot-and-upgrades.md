# Melee equipment, earned loot, and upgrades

**Status: design specification, not implemented delivery or gameplay acceptance.**
This document records the operator's requested melee variety, rewarding loot,
weapon power-ups, and gacha-style reward reveals for *Forever We Reign*. It
extends the existing offline progression contract without increasing the launch
catalog, adding a combat style, or establishing the proposed character stats.

The numerical family limits and duplicate-protection threshold below are initial
tuning hypotheses for Task 34. They have not been established by playtesting.

## Launch catalog and weapon families

| Category | Definitions | Launch role |
| --- | ---: | --- |
| Swords | 4 | Balanced health damage, balance pressure, and Focus generation |
| One-handed axes | 4 | More health damage with lower Focus generation |
| One-handed maces/warhammers | 4 | More balance pressure with lower health damage |
| Shields | 8 | Existing guard equipment |
| Helmets | 4 | Existing head equipment |
| Armor | 4 | Existing body equipment |
| Talismans | 6 | Existing accessory equipment |
| **Total** | **34** | **12 melee weapon definitions plus 22 other equipment definitions** |

All launch weapons use one one-handed player combat style with a shield and one
shared humanoid skeleton family. Each family uses fixed item definitions and
the approved combat statistics. No randomized affix system, extra equipment
slot, ammunition inventory, or new crafting currency is added.

**Ranged weapons are excluded.** There are no player bows, firearms, thrown
weapon families, or ranged weapon progression.

Sword, axe, and mace compatibility remains unvalidated until the actual assets
pass Task 11 and presentation acceptance. Sharing a skeleton does not prove
that grips, swing arcs, contact, recovery, or apparent weapon weight work. The
validated size envelope and hand sockets must support each family without
changing combat hit-resolution rules.

| Later family | Additional work before player support |
| --- | --- |
| Spear/polearm | Two-hand contact, thrust and sweep clips, direction mapping, recovery, defensive masks, and portrait framing |
| Greatsword or large two-handed hammer | Two-hand grips, heavy attack/recovery clips, shield replacement and defense rules, timing, and input acceptance |
| Paired daggers | Additional hand contact, paired attack clips, shield replacement and defense rules, buffering, and direction acceptance |

These are deferred player styles, not additions to the launch commitment.
Existing polearm and hammer enemies do not establish player support. Any later
style needs its own content, combat, and physical-device acceptance.

## Item identity and permanent progression

Each weapon needs a recognizable silhouette and icon, a short description of
its role, and an equipment comparison that exposes its strength and concession.
Family identity should produce a useful loadout decision rather than a higher
number that replaces every previous item.

For the first family pilot, keep health damage, balance pressure, and Focus
generation within approximately **15% above or below a comparable sword
baseline**. Give the axe and mace families one advantage and one concession as
shown in the catalog. This is a bounded starting point for tuning, not a finding
that those values are balanced. Task 34 may revise the values after comparison
across opponents, upgrades, and tiers.

Permanent progression retains the existing rules:

- One owned record per equipment definition.
- Three upgrade levels per item, with fixed costs and predictable improvements.
- Mastery keyed by definition, with one completion reward and one claimed flag.
- Duplicate equipment converts to ordinary currency; it does not create another
  inventory copy, reset mastery, or repeat a first-mastery award.
- Character XP continues to accrue when equipped items are mastered.
- Ordinary currency buys equipment and upgrades; Legacy seals unlock the
  existing bounded legacy perks.

Upgrade progression must preserve family tradeoffs. There is no unlimited
upgrade ladder or random reroll of an item's properties. Upgrade prices,
conversion values, and acquisition costs remain Task 34 tuning work.

## Earned rewards and player choice

Every normal encounter victory grants the specified ordinary currency and
character XP, plus eligible equipment mastery. Authored caches and selected
boss rewards grant equipment from a displayed pool. Random equipment is an
additional discovery path; baseline progression does not require a lucky drop.

First-clear equipment milestones offer a choice across eligible melee families.
The first such milestone should show one authored starter item from each of the
three families. Select the concrete item before the encounter checkpoint, so
the checkpoint records the choice and victory can commit the grant atomically.
For a cache milestone, make the choice before the cache claim transaction.
If every offered item is already owned, show its duplicate conversion clearly.

The collection screen also provides a direct acquisition path: fixed equipment
purchases using ordinary currency at the refuge. Task 34 sets costs against
encounter throughput so a target loadout does not depend on random rewards.
Milestones and purchases do not consume Legacy seals or introduce another
currency.

This emphasis on choice and visible progress is an inference from research that
associated perceived autonomy and competence with game enjoyment. It does not
prove the correct drop rate, protection threshold, or progression pace for this
game. [Ryan, Rigby, and Przybylski, published study, 2006](https://selfdeterminationtheory.org/SDT/documents/2006_RyanRigbyPrzybylski_MandE.pdf)

Fixed family properties and thematic item identities also have a developer
design precedent. Blizzard described distinct weapon characteristics and items
with fixed thematic properties during Diablo IV development. That 2020 post is
a design reference, not evidence of current Diablo rules or this game's
accepted balance. [Blizzard itemization design post, 2020](https://news.blizzard.com/en-us/article/23583664/diablo-iv-quarterly-updatedecember-2020)

## Gacha-style equipment reveals

Use the reveal as presentation for an earned equipment grant. Opening costs no
currency. There are no paid draws, paid rerolls, expiring pools, store timers,
near-miss displays, or attendance obligations.

Before the encounter or cache claim, show:

- The eligible item pool and its stable identity.
- Exact current item probabilities, including any active protection.
- Which items are already owned and their duplicate conversion values.
- The duplicate-protection counter and what the next protected grant does.
- Any first-clear guarantee or chosen milestone item.

The initial random-pool design uses equal weights among eligible definitions.
For an ordinary draw from a pool of N definitions, each definition has
probability 1/N. This includes owned definitions, which convert to currency.

Initial duplicate protection:

1. Each random equipment grant uses its authored pool's saved counter.
2. An unowned grant resets that counter to zero.
3. A duplicate grant increments the counter.
4. After two consecutive duplicates, the next equipment grant is uniformly
   selected from that pool's eligible unowned definitions and resets the counter.
5. If the eligible pool is complete, all grants convert to currency and the UI
   shows completion instead of promising an unowned reward.

This proposed threshold is a Task 34 tuning hypothesis. The counter counts
**equipment grants, not victories**. Fixed milestone grants and direct purchases
can change ownership, but do not artificially advance the random pool's counter.
Changing an authored pool's revision must not silently erase protection.

On a protected draw with U eligible unowned definitions, each of those
definitions has probability 1/U and owned definitions have probability zero.
The preview uses the same frozen pool and protection state as the reward
transaction, rather than displaying a generic base rate.

For a **fixed twelve-item pool**, starting with **no ownership**, collection is
bounded by **34 equipment grants**: the first grant is new, then each of the
eleven remaining items requires at most three grants. This bound assumes no
other acquisition path, eligibility change, or pool change during those grants.
It does not describe victories, playtime, the full 34-item catalog, or a campaign
with progressively unlocked pools.

Random drops plus a deterministic earned acquisition fallback have a practical
precedent in Warframe's Lua's Prey release, where guaranteed earned resources
could be exchanged for the same rewards available through random drops. This
game uses its existing ordinary currency for the fallback.
[Warframe official patch notes, 2022](https://www.warframe.com/en/patch-notes/pc/32-2-0)

A published survey found an association between paid loot-box spending and
problem gambling. Its correlational design did not establish causality, and it
explicitly did not establish effects of unpaid openings. That is a reason to
avoid importing paid gacha mechanics, not evidence that this earned reward
design is proven safe or harmful.
[Zendle and Cairns, PLOS ONE, 2018](https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0206767)

## Deterministic rewards and interruption

Keep one serialized save writer and the existing recoverable save generations.
The pre-fight checkpoint records the reward seed, selection algorithm version,
pool ID and revision, eligible definitions, relevant ownership and protection
snapshot, and any fixed milestone choice. Cache claims use the same reward
contract at their node transaction boundary.

Use a stable, versioned deterministic selection algorithm. Do not use the wall
clock, animation frame, reveal input, or a global presentation random stream to
select equipment. Reloading the checkpoint preserves its reward parameters.

Victory commits one transaction containing:

- Currency and equipment grants or duplicate conversion.
- Character XP, mastery, and any associated level changes.
- Updated protection counters and permanent milestone claims.
- Node completion, route advancement, and the reward claim identifier.
- The concrete committed reward result, pool/revision, and protection change in
  the reward ledger.

Reward identity remains tied to expedition and encounter or node identity.
Repeated processing of a claim returns its committed result and cannot grant
another reward or advance protection again. The implementation must distinguish
separate authored grants within one result without making them replayable.

Only display a reward reveal after its transaction is committed. Reveal skipping,
reduced effects, and termination during presentation read the same committed
result. A failed save stays on the recoverable result screen and retries the
same transaction. No progression continues with an uncommitted reward.

Task 16 must preserve saved pool revisions, counters, and unknown content records
through explicit migrations or recovery. Missing content must not trigger a
fresh random draw. These contracts prevent ordinary reload rerolls and duplicate
claims. Checksums and deterministic selection do not make editable offline saves
cheat-proof.

## Optional temporary weapon effects

Temporary weapon effects are **Task 22 design candidates**, not implemented
features or an automatic addition to the three-ability launch budget. Adopt an
effect only if it fits that budget and the existing combat-event model.

| Candidate | Benefit | Concession |
| --- | --- | --- |
| Damage temper | More health damage for the encounter | Lower Focus generation |
| Guard temper | Lower guard expenditure for the encounter | Lower outgoing health damage |

If adopted, allow at most one such effect at a time. Define its magnitude,
activation, duration, and expiration explicitly; end it at encounter exit,
defeat, or rebirth. It does not grant permanent mastery or add a material
inventory, random affix engine, or extra resource.

Attack, Vitality, Endurance, and Speed remain candidates in the existing
[Task 19 progression notes](2026-09-30-progression-and-combat-feel-notes.md).
This specification does not implement those stats, change player action timings,
or modify enemy telegraphs and parry windows. Input responsiveness remains a
baseline requirement, independent of character progression.

## Implementation and acceptance map

| Task | Required integration |
| --- | --- |
| 11 and 12 | Validate one-handed family assets, hand contact, portrait framing, impact, and recovery presentation |
| 13 | Stable item/pool IDs, revisioned reward definitions, fixed traits, and authoring validation |
| 15 to 17 | Saved protection state, frozen checkpoints, migrations, atomic claims, committed results, and recovery |
| 18 | Ownership, three upgrades, duplicate conversion, family statistics, and one-time definition mastery |
| 19 | Resolve the separately documented character-stat candidates before any timing modifier |
| 20 | Item comparison, direct purchase, exact probabilities, protection progress, and reveal skipping |
| 21 and 22 | Authored cache/milestone rewards; optional effects only within the existing ability budget |
| 33 | Populate exactly 34 validated equipment definitions, including the twelve melee weapons |
| 34 | Tune family limits, duplicate protection, acquisition costs, and progression through simulations and playtesting |
| 38 | Verify interruption, upgrade compatibility, recovery, and full progression across supported devices |

| Acceptance scenario | Required result |
| --- | --- |
| Equip each melee family | Validated contact and framing; one-handed controls and defense still satisfy the combat contract |
| Compare equally upgraded families | Strength and concession are visible; no family dominates every tested opponent and resource situation |
| Draw twice into owned items | Third grant is unowned when eligible unowned items remain; preview matches actual selection probabilities |
| Exhaust a pool | Duplicate conversion is explicit; protection does not promise nonexistent unowned items |
| Simulate the fixed twelve-item example | Collection completes within 34 equipment grants under the stated assumptions |
| Replay a pre-fight checkpoint | Reward parameters, milestone choice, and protection snapshot remain fixed |
| Terminate around victory commit | No duplicate item, currency, mastery award, or protection increment |
| Skip or terminate the reveal | The same committed result is available after resume |
| Fail the save write | Recovery/retry preserves the transaction and blocks uncommitted progression |
| Load after a pool/content revision | An explicit migration or recovery preserves claims and protection without rerolling rewards |
| Duplicate or repurchase a mastered item | No new first-mastery reward or extra ownership copy |
| Acquire a target item directly | Ordinary-currency transaction is atomic; no random draw or new currency is required |
| Equip mastered items throughout a route | Character XP and normal rewards continue |
| Apply a proposed temporary effect | One effect maximum, explicit tradeoff, and expiration at the specified boundary |

Task 34 records collection completion, target-item acquisition, duplicate streaks,
upgrade affordability, and family performance across opponents and tiers. Neither
these acceptance scenarios nor the research references establish completed
implementation, measured retention, or physical-device acceptance.

[Return to development plan](development-plan.md)

## Original narrative and handling proposals

The [lore bible](../design/forever-we-reign-lore-bible.md) proposes equipment names
and earned pattern reproductions within this existing 34-definition envelope.
The [melee direction](../design/melee-combat-direction.md) maps richer anchored
handling to the existing resolver and Task 13 sword/axe/mace validation pilot.
Neither document activates paid draws, new currencies, abilities, or additional
weapon families. Weapon timing and contact bindings must pass Task 13 validation
before use; full catalog production remains Task 33.
