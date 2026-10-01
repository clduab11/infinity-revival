# Chivalry II melee reference, 2026-09-30

**Recommendation:** adopt readable anticipation, body-led momentum, distinct
attack shapes, coherent recovery, and weapon-specific timing. Keep Forever We
Reign's anchored one-thumb defense/opening rhythm and original cursed-throne
story. The [design direction](../design/melee-combat-direction.md) translates
the findings into the current project and Task 13 authoring requirements.

**Evidence boundary:** official Torn Banner/Epic written sources were inspected
on 2026-09-30. No Chivalry animation curves, joint degrees, motion-capture files,
or newly measured frame timings were obtained. Historical patch values show
design intent and independent tuning dimensions; they are not current balance
recommendations or proposed Forever We Reign values.

## Primary sources inspected

| Source | Date/type | What it directly supports |
| --- | --- | --- |
| [Chivalry II official FAQ](https://chivalry2.com/faq/) | Official current FAQ; combat and cosmetics sections | Swing/stab/overhead, flowing combos, fast riposte after blocking, readable actions, momentum/hip torque, and cosmetic skins separate from gameplay |
| [Advanced Combat developer recap](https://chivalry2.com/2022/03/24/advanced-combat-twitch-stream-recap/) | 2022-03-24; Torn Banner technical-designer stream recap | Windup feints, opposite-side alternate attacks, directional parrying, evasion, queued counter/riposte, and shield-vs-weapon block expenditure |
| [Weapon Balance patch 2.4.2](https://chivalry2.com/2022/03/28/patch-2-4-2/) | 2022-03-28; official patch notes | Weapon/attack-specific windup, release, and combo adjustments; explicit readability intent; damage-type resource differences |
| [House Aberfell patch 2.4](https://chivalry2.com/2022/01/26/house-aberfell-patch-notes/) | 2022-01-26; official animation notes | Third-person slash windup changes to distinguish slash from overhead while looking downward |
| [Reinforced patch 2.6](https://chivalry2.com/2022/10/04/reinforced-update-patch-notes/) | 2022-10-04; official balance notes | Attack-specific windups and weapon turn limits are separate tuning dimensions |
| [Winter War public-test notes](https://chivalry2.com/2022/11/14/update-2-7-public-testing/) | 2022-11-14; official test notes | Counter/riposte UI aligned to actual active-parry duration and weapon-dependent block efficiency |
| [Infinity Blade official manual](https://cdn3.unrealengine.com/Infinity%20Blade%2FIB1%20Feature%20Images%2FIB_Classic_Manual-df04617915af60774705f5739b3abfcf50b2580b.pdf) | 2012; Epic/ChAIR manual | Touch attacks/parries, shield/dodge defenses, enemy openings, directional combos, item mastery, and progression |

The official FAQ links Torn Banner's Combat Guide video. Its YouTube target
could not be opened through the research web tool. Findings here therefore do
not claim a frame-by-frame analysis of that video. Community wikis, unofficial
weapon tables, player posts, and extracted game data were not used as evidence.

## What the evidence establishes

### Readable momentum and attack identity

Torn Banner's FAQ describes attacks and blocks as weighty, with clear action
indication. Its stated goal balances visible physical momentum with control.
Player swing adjustment simulates hip torque; the commitment of a swing limits
arbitrary reversal. These are suitable art-direction principles for authored
clips, even when our game does not let players drag attacks in real time.

The same FAQ identifies swing, overhead, and stab inputs, plus combos and
ripostes. House Aberfell's animation notes make attack silhouette an explicit
readability issue: a third-person slash windup was improved so that slash and
overhead remain distinguishable when looking down. This supports distinct
preparation poses, not four recolored copies of one arm rotation.

Sources: [official FAQ, Combat](https://chivalry2.com/faq/),
[House Aberfell, Animation](https://chivalry2.com/2022/01/26/house-aberfell-patch-notes/).

**Our interpretation:** stage lateral attacks with body coil/unwind, descending
attacks with a raised preparation and downward finish, and rising attacks with
a low chamber and upward arc. Coordinate chest, shoulder, elbow, wrist, and
weight shift; continue past contact before recovering. These specific joint
instructions are original animation choices, not published Torn Banner curves.

### Weapon timing has several dimensions

Official balance notes independently change attack windup, release, combo pace,
turn limits, damage, and resource effects. Patch 2.4.2 links windup/release
adjustments with readability. Patch 2.6 adjusts individual normal/heavy attack
windups and a weapon's turning restriction separately.

Sources: [patch 2.4.2](https://chivalry2.com/2022/03/28/patch-2-4-2/),
[patch 2.6](https://chivalry2.com/2022/10/04/reinforced-update-patch-notes/).

**Our interpretation:** each family needs an authored path and a validated
start/contact/recovery contract. A faster clip multiplier alone cannot represent
different handling. Preserve integer logical timing, with contactSeconds stored
as presentation data and sampled to the authoritative impact. Mesh shape alone
does not derive damage, reach, or timing.

The FAQ explicitly separates cosmetic armor/weapon skins from gameplay effects.
That does not prohibit our equipment statistics. It clarifies our terminology:
a cosmetic skin changes appearance; a distinct weapon definition changes its
authored statistics and approved handling profile. Do not attach a covert speed
bonus to an otherwise cosmetic skin. [Official FAQ, Cosmetics](https://chivalry2.com/faq/)

### Defense and counterattack flow

The FAQ identifies a fast attack after blocking as a riposte. The developer
recap documents attack feints, alternate attacks, directional parrying, and
counter/riposte queuing. It also states that shields spend less stamina blocking
than weapon parries. Winter War test notes tie counter/riposte text to the actual
active-parry duration, illustrating that UI must represent the real rule.

Sources: [official FAQ](https://chivalry2.com/faq/),
[developer recap](https://chivalry2.com/2022/03/24/advanced-combat-twitch-stream-recap/),
[Winter War notes](https://chivalry2.com/2022/11/14/update-2-7-public-testing/).

The inspected official written material does not supply a complete, current
formal counter specification with all matching-input, stamina, immunity, and
window edge cases. Do not fill that gap with unverified player descriptions.

**Our interpretation:** keep held guard and a visibly different timed parry.
Successful defense should communicate earned advantage. Future riposte/counter
rules can express that advantage in the next owned opening, without requiring
two fingers or borrowing Chivalry's multiplayer active-parry behavior. Until
implemented, they remain reserved concepts, not hidden gameplay flags.

### Infinity Blade supplies the touch rhythm

Epic/ChAIR's 2012 manual documents swipe attack/parry, shield and dodge controls,
defensive success creating attack opportunities, directional combos, and item
mastery. Its developer tips even describe sliding from shield into a swipe.
Those facts support the reference game's touch-first interaction, not our exact
pointer-ownership contract or one-handed phone reach.

The manual includes post-launch modes. It cannot establish only the December
2010 launch feature set. The original reference's slide-off-shield technique is
also **not current Forever We Reign behavior**: our guard touch remains UI-owned
and the player starts a new touch for an arena swipe. Multiplayer and the
manual's purchase/reset rules are not adopted into this premium offline game.

Source: [official manual, Combat, HUD and Controls, Tips from the Developers](https://cdn3.unrealengine.com/Infinity%20Blade%2FIB1%20Feature%20Images%2FIB_Classic_Manual-df04617915af60774705f5739b3abfcf50b2580b.pdf).

## Adaptation matrix

| Reference behavior or principle | Forever We Reign decision | Why |
| --- | --- | --- |
| Lateral slash and opposite-side attack | Four current cuts with distinct arcs | Preserves gesture literacy and cheap shared-rig content |
| Overhead | CutDown descending motion | Clear tall silhouette without an extra control |
| Stab/thrust | Deferred explicit direction rebind or later style | CutUp currently means rising cut; silently replacing it breaks teaching and replay meaning |
| Hip torque and swing momentum | Author body-led preparation/release/follow-through | Captures handling while keeping one-thumb input and deterministic admission |
| Freeform drags/acceleration | No player-controlled arc or camera-driven damage | Gesture surface cannot also be a look/attack-manipulation surface without ownership conflicts |
| Held block | Current finite guard plus visible shield bracing/recoil | Familiar safe defense with a meaningful existing cost |
| Counter and riposte | Current parry/earned opening now; explicit optional evolution later | Avoids unimplemented active defense or stale-phase attacks |
| Feints | Readable authored enemy preparation; player replacement rules deferred | AI commitment and one-command buffering stay intact |
| Heavy attack | Future explicit one-thumb mapping, disabled in Task 13 | Hold duration already interacts with gesture recognition; no implicit extra mode |
| Footwork/evasion | Bounded dodge, lunge, recoil | Keeps arena framing and reduces content/input scope |
| Jabs/kicks/crouch/throws | No required launch player controls | Existing grammar already consumes reachable controls and combat teaching budget |
| Weapon cosmetics vs handling | Separate cosmetic skin and weapon definition | Appearance must not secretly change timing or undermine loadout comparison |
| Camera readability | Bounded event-driven orientation/offset, stable screen-space directions | Reveal motion/contact while maintaining the opponent's tell and HUD |
| Stamina | Keep guard/dodge/Focus, no new unified bar | Avoids a duplicate economy before existing resources are qualified |

## Evidence and production consequences

The first useful asset batch is one sword, one one-handed axe, and one
one-handed mace/warhammer. Validate original geometry, appearance, grips,
equipment sockets, four cuts, contact pose, recovery, and shared-rig retargeting.
It is a handling pilot, not the delivery of all twelve finished launch weapons.

The proposed microsecond durations and clip contact positions in the design
document are project fixtures and future tuning hypotheses. No primary source
is cited as evidence that those values are balanced. Compare complete opening
output, repeated input, guard release travel, and opponent readability on
physical phones before accepting them.

The supplied CombatPrototype recording is feedback about our present game. It
does not measure Chivalry handling or prove native phone latency. The README
image remains a concept reference; original procedural prototype materials and
the technical Humanoid pipeline are not equivalent to finished production art.

The practical tradeoff is deliberate: improve authored motion and a small
weapon data contract, retaining combat authority and one-thumb reach. Full
freeform Chivalry combat would require a different input product and a much
larger animation, collision, and balancing workload.
