# Task 08: seeded regular-enemy patterns

## Work order and intent

Implement regular-enemy pattern decks, cooldowns, selection history, encounter
seeds and safe phase boundaries. Exit: AI never changes a committed attack in
response to unfinished input. Preserve Task 07's 488 Edit Mode/56 Play Mode/12
Python baseline and canonical checkout. The approved game remains Forever We Reign.

The supplied AI specification requires authored weighted selection using phase,
cooldowns, history, approved resources, difficulty and seed; a hierarchical state
machine; small single-phase regular decks. No behavior-tree middleware, runtime
language models, machine learning, navigation or new dependencies.

## Bounded prototype choices

- Four immutable code-authored decks: sword, shield, polearm and hammer guards,
  each with three prototype patterns and the authored teaching purpose. Finished
  opponents are Tasks 26-29; ScriptableObject authoring/validators are Task 13.
- One to three immutable steps per pattern, up to 16 patterns per deck. Stable
  content IDs and captured content/balance revision strings. Copy collections.
- A common first teaching pattern uses Task 06 strike 1 at 10000us: Up parry,
  safe left dodge, impact660000us and recovery1160000us. Subsequent choices use
  the deck's weights, current approved observations, history, tier and seed.
- Prototype pattern cooldowns 0/4000000/6000000us, starting on successful sequence
  admission. They use frozen integer combat time, not device or frame time.
- Keep the last two committed pattern IDs. Suppress an immediate repeat when
  another legal candidate exists; halve weight for the other recently used ID,
  minimum one. If one legal candidate remains, allow it.
- Local xorshift32 with zero seed normalized to0x6D2B79F5; unbiased bounded integer
  draws. No Unity/global/System.Random. Preview uses a copy of RNG state.
- Approved immutable observations: player health, guard, dodge charges, Focus,
  last resolved defense outcome and last resolved player attack direction.
  Eligibility can require minimum guard/dodges/Focus and tiers0-5; an authored
  preferred completed-defense outcome doubles weight. No touch, contact, pending
  gesture, buffer direction, view or active dodge-side references enter AI.
- All prototype telegraphs are at least650000us. Steps cannot overlap; each next
  tell begins at/after previous recovery, with authored inter-step gaps. Existing
  defense windows are unchanged. Tier input changes eligibility, with progression,
  reward and stat scaling deferred to Task 22.
- Prepare a choice once per safe-boundary admission cycle. Cache it on rejection.
  RNG, cooldowns and history advance only after atomic timeline admission.
- When no candidate is off cooldown, schedule a combat-time wake at the earliest
  eligible deadline. No draw/history change. Resource-ineligible custom decks may
  wait for approved observations; built-in decks have an unconditional fallback.

## Commitment and phase ownership

One committed sequence owns all3*N enemy milestones plus one final opening
boundary. CombatEncounter admits the batch atomically, including the controller's
boundary marker. Definitions are immutable; no selected attack is rerolled,
shortened, canceled or replaced in response to input or later resource changes.
Balance break upgrades the opening after the final committed step; the entire
pattern is already committed and keeps its noninterruptible attacks.

CommitStrike remains the one-step API for legacy callers. Multi-step admission
validates IDs, timestamps, arithmetic, ordering, duplicates and capacity before
mutation. The final live milestone supplies the resume-retimed opening boundary.
Nonoverlap guarantees no conflicting tell and earliest-impact target. Inclusive
defense still resolves before a tied zero-recovery opening.

The application enemy director has hierarchical states under the duel: Waiting,
Prepared, Executing, PlayerOpening, Terminal. It subscribes to safe sequence-ready
boundaries, completed outcomes, timeline and frame advancement. Initial admission,
cooldown wake and capacity retry require a running clock and enemy phase with no
committed sequence. Retry uses the cached definition and a future telegraph time.
Suspend freezes cooldowns and retains commitment; terminal/dispose cancel owned
wakes and release subscriptions. Restart resets seed, history and cooldowns.

## Verification and preservation

Tests first, native Unity RED before production implementation. Cover literal
seeded draws, exact cooldown boundaries, repeat/history policy, resource/tier
eligibility, malformed definitions, collection immutability, preview/commit
ownership and overflow. Integration covers atomic multi-strike admission, one
final opening, pause/resume retiming, capacity retries without RNG/history drift,
terminal cleanup, incomplete raw gestures and source-time phase boundaries.
Record actual patterns/strikes/resources at30/60/120 FPS and jitter.

Preserve Editor6000.6.0f1, package pins, existing GUIDs, scenes, alternate project,
historical receipts, narrative and concept provenance. Root alone runs Unity.
No art generation, purchase, commit, push, memory write or external service change.
