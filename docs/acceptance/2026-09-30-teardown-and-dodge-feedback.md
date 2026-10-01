# Teardown clock and dodge availability correction

The remaining `OnDisable` clock exception and misleading dodge availability
display are corrected. Native Unity verification passes 669 Edit Mode and 95
Play Mode cases; all 12 repository verifier tests pass. The operator's next
interactive playthrough and physical-device acceptance remain UNRUN.

## Operator observation and failure

The operator preliminarily passed the Editor control check: both dodges worked,
mouse swipes emulated sword actions, and the ability control responded without
an equipped ability. Rapid presses did not produce equally rapid dodges. One
remaining `deviceNowUs` exception originated in `CombatTimingDriver.OnDisable`.

Disable previously sampled the external time source again. A source that resets
or becomes unavailable during teardown can fail strict clock validation, aborting
pending-command and capture cleanup. The supplied trace identifies this path;
it does not establish the precise instant Unity reset its realtime source.

Disable now suspends at the clock's last accepted device timestamp. It preserves
combat time, discards committed and pending commands, and disables capture without
reading the expired source. Domain validation and live input timestamp rules
remain strict. There is no timestamp clamp or exception suppression.

## Dodge feedback and combat feel

Each accepted dodge spends one of three charges and occupies 360 ms. Additional
defensive presses during that action are rejected. One charge returns every
three seconds without an accepted dodge; rejected presses do not delay recharge.
Offense retains its existing single 120 ms input buffer and first-direction rule.

Both dodge controls now use the unavailable color unless charges remain, the
combat clock is Running, and the player is Ready or Guarding. Controls stay
active raycast targets, so unavailable presses remain owned by UI and cannot
become sword swipes. Action timings, resources, input admission, and buffering
are unchanged.

These changes correct cleanup and availability feedback. They do not establish
fluid animation or measured phone input latency. Task 10 qualifies device
performance; Task 12 connects authored animation, camera, audio, and effects.
The operator's Attack, Vitality, Endurance, and Speed ideas are recorded as
[Task 19 design candidates](../production/2026-09-30-progression-and-combat-feel-notes.md).
No new progression statistics or modifiers were implemented.

## Verification

| Check | Result | Evidence |
| --- | --- | --- |
| Teardown regression before correction | Three expected failures for zero, negative, and throwing sources | [Native RED XML](evidence/2026-09-30-teardown-red.xml) |
| HUD regression before correction | 28 cases, 18 pass and 10 expected availability failures | [Native RED XML](evidence/2026-09-30-dodge-hud-red.xml) |
| Final native Edit Mode | 669/669 pass, no skips | [Edit Mode XML](evidence/2026-09-30-teardown-dodge-editmode.xml) |
| Final native Play Mode | 95/95 pass, no skips; all prior 79 identities preserved | [Play Mode XML](evidence/2026-09-30-teardown-dodge-playmode.xml) |
| Baseline verifier tests | 12/12 pass | Recorded in the [machine receipt](2026-09-30-teardown-and-dodge-feedback.json) |
| Structural validator | PASS, 50 direct packages, 69 resolved packages, 155 asset GUIDs, native Git checks | Machine receipt |
| Independent focused review | No actionable P0/P1/P2 findings | Read-only teardown and final HUD review |

Three added teardown cases verify no source read, preserved accepted times,
suspension, pending-defense discard, and successful countdown after re-enable.
Thirteen added HUD cases cover admitted states, action lockouts, suspension and
countdown, exact 360 ms readiness, depleted charges, and native UI ownership.

Native tests used an isolated project copy while the operator's Editor remained
open. All 99 game C# sources matched that copy. Final logs contain zero
`deviceNowUs` exceptions, queued-event-limit warnings, deprecated lookup warnings,
or C# compiler errors. Only four C# files changed from the follow-up preflight.
Domain combat/input code, existing asset metadata and scenes, project settings,
the package manifest and lockfile, and the disabled Pipeline control were
preserved byte for byte. No AI credits, commit, or push were used.

## Interactive recheck

1. Stop Play and allow Unity to finish compiling the changed scripts.
2. Clear Console, enter Play, and exercise rapid dodge presses and sword swipes.
3. Confirm dodge availability reflects lockout and returns after recovery when
   charges remain. Exercise pause, resume, and encounter restart.
4. Stop Play, then enter and stop Play a second time. Check for fresh exceptions
   and queued-event-limit warnings.

Task 09 physical same-hand acceptance and Task 10 device qualification remain
UNRUN. The preliminary Editor observation is recorded separately from those
gates. The earlier [startup correction](2026-09-30-input-clock-fix.md) and its
source hashes remain historical evidence.
