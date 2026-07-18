# Task 13G Balanced Expansion Run Qualification

Date: 2026-07-18

## Baseline And Scope

- Source baseline: `8372d06e7a7e4c2784d027e492825640a68f7540` from qualified Task 13F.
- Branch: `feature/balanced-expansion-run`.
- Adds one isolated ten-wave expansion stage, one exact twenty-card pool, and a fixed two-Trap/one-Defense anchor fixture.
- Production waves, the default 39-card pool, and `VerticalSlice18` remain unchanged.

## Expansion Stage

Stable ID: `expansion_vertical_slice_01`. Starting hero: Archer. Exact wave counts are enabled.

| Wave | Label | Enemies |
|---:|---|---:|
| 1 | Foundation | 12 |
| 2 | Speed Pressure | 20 |
| 3 | Durable Frontline | 18 |
| 4 | Ranged Introduction | 20 |
| 5 | Mixed Midpoint | 26 |
| 6 | Elite Introduction | 19 |
| 7 | Combined Counterplay | 39 |
| 8 | Heavy Pressure | 36 |
| 9 | Peak Encounter | 70 |
| 10 | Warlord Finale | 22 |

The Crossbow Raider first appears in Wave 4, the War Shaman in Wave 6, no wave has more than two Shamans, and exactly one Warlord appears in Wave 10.

## ExpansionRun20

- 3 Recruit Hero cards: Bombardier, Frost Mage, Electric Engineer.
- 8 qualified Task 13C Hero Upgrade cards.
- 6 support Modifier cards.
- 2 Trap cards: Caltrops and Burning Oil.
- 1 Battlefield Defense card: Wooden Barricade.
- Recruit choices are guaranteed while a valid hero and slot remain.
- Battlefield choices are filtered when their required fixed anchors are unavailable.

## Balance Simulation

The deterministic 100-seed simulation produced 75 wins (75%), zero invalid drafts, zero soft locks, median Wave 10, average Wave 9.75, and average projected duration 556.6 seconds. Every one of the twenty cards was offered and all three recruit heroes were selected across the simulation.

## Peak Encounter Profiling

Wave 9 was exercised with all 70 Enemy runtime objects at both speeds:

- 1x: 70 alive, peak sampled GC allocation 268,345 bytes.
- 2x: 70 alive, peak sampled GC allocation 270,069 bytes.
- Cleanup returned the active targetable registry to zero after each profile.

These Editor measurements qualify lifecycle behavior and provide a comparative baseline. They do not replace physical Android profiling.

## Controlled Run And Regression

The real isolated stage completed all ten waves at 2x, reached Victory, processed 10 level-up drafts, peaked at 21 simultaneously active targetable enemies, and finished in 82.0 seconds of realtime. All seven enemy pools ended with zero active objects and zero invalid returns. Restart recreated one GameManager, one CardDraftManager, and one RunModifierManager, with traps and defense cleared.

The unchanged production ten-wave Warlord run also passed inside the complete PlayMode suite.

## Automated Qualification

- EditMode: 130 passed, 0 failed, 0 skipped.
- PlayMode: 96 passed, 0 failed, 0 skipped.
- Total: 226 passed, 0 failed, 0 skipped.
- New Task 13G coverage: 42 EditMode and 11 PlayMode tests.
- Validator: 50 cards, 7 enemies, 2 traps, 1 defense, 3 card pools, 0 errors, 34 documented legacy warnings.

## Protected Local State

Ten Quaternius materials, `ProjectSettings/EditorSettings.asset`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/TimeManager.asset`, and `TD catle defence.slnx` remain local and unstaged.

## Decision

The isolated stage, exact card pool, deterministic balance window, peak encounter lifecycle, full controlled run, production regression, restart safety, and complete automated suite passed.

**QUALIFIED**

Physical Android profiling, final production art, and final audio remain non-blocking follow-up work.
