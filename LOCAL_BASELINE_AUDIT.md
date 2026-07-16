# Local Baseline Audit - Task 12B

Date: 2026-07-17

## Decision

**QUALIFIED**

The local Stonehold baseline compiles, all 13 automated tests pass, and the automated 10-wave regression reaches Victory with all six heroes contributing damage. Task 12B did not push anything.

This qualification applies to the current Unity Editor/batch baseline. It is not Android-device performance approval and not approval for the future 60-100 enemy density target.

## Starting State

- Branch: `feature/hero-castle-defense-pivot`
- Remote commit: `d099439da47bfae171a8898d5a7306bf94015240`
- Starting local commit: `9130af948f43d7a0c82dc82fc15771c3a0fd7e7d`
- Unity: `6000.5.2f1`
- Starting relationship: local branch one commit ahead of remote.

Preserved initial local files:

- 10 modified Quaternius enemy materials.
- Modified `ProjectSettings/ProjectSettings.asset`.
- Untracked `LOCAL_BASELINE_AUDIT.md`.
- Untracked `unity_launch_log.txt`.

Safety patch:

- `C:\Users\forza\OneDrive\Desktop\td castle defence\stonehold_pre_task12b_uncommitted.patch`

## Root Causes And Corrections

### 1. Paused Result VFX

Root cause: Victory and Defeat set `Time.timeScale` to zero while result sequences and pool-return waits used scaled time.

Correction: result effects explicitly use unscaled particle playback and realtime waits. Ordinary combat VFX remains on scaled time. GameManager remains the authority for game state and time scale.

### 2. Healing Triggered Damage Feedback

Root cause: VFX and audio subscribed to the broad `Castle.HealthChanged` event, which fires for both damage and repair.

Correction: Castle now raises `DamageTaken`, `Healed`, and the existing `HealthChanged` with distinct semantics. VFX and audio subscribe only to `DamageTaken`; UI can continue using `HealthChanged`.

### 3. Status Effects Updated While Idle

Root cause: every controller ran Update and repeatedly scanned effect lists with `Exists`/`Find`, including enemies without active effects.

Correction: idle controllers disable themselves, enable only when an effect is applied, cache references and active-state flags, and process effects in one allocation-free pass. Burn uses a bounded loop so larger deltas do not silently drop expected ticks.

### 4. Pooled Particles Retained State

Root cause: return paths stopped only the root particle and did not reliably clear children, trails, transform state, time mode, or stale delayed returns.

Correction: `PooledParticleState` caches child particle/trail components, clears the hierarchy, resets transform and unscaled-time state, and uses activation tokens to reject stale or duplicate returns.

### 5. Zero Automated Tests

Root cause: the project had no runtime assembly boundary or test assemblies discoverable by Unity Test Framework.

Correction: added `Stonehold.Runtime`, EditMode test, and PlayMode test assemblies with 13 deterministic tests. Tests restore time scale, temporary objects, and PlayerPrefs state.

## Automated Results

EditMode:

- Discovered: 9.
- Passed: 9.
- Failed: 0.
- Skipped: 0.
- Duration: 0.046 seconds.

PlayMode:

- Discovered: 4.
- Passed: 4.
- Failed: 0.
- Skipped: 0.
- Duration: 88.477 seconds.

Total:

- Discovered: 13.
- Passed: 13.
- Failed: 0.
- Skipped: 0.

## Regression Matrix

| Check | Result |
|---|---|
| Main gameplay scene initialization | Pass |
| Fresh run starts | Pass |
| Archer recruited/attacks | Pass |
| Bombardier recruited/attacks | Pass |
| Frost Mage recruited/attacks | Pass |
| Fire Mage recruited/attacks | Pass |
| Electric Engineer recruited/attacks | Pass |
| Sniper recruited/attacks | Pass |
| Burn large-delta ticks and expiry | Pass |
| Slow expiry and speed restoration | Pass |
| Shock refresh and expiry | Pass |
| Draft selection and resume | Pass; 8 choices applied |
| 1x, 1.5x, and 2x | Pass |
| Pause/resume | Pass |
| Waves 1-10 | Pass |
| Warlord boss and Victory | Pass |
| Victory VFX at time scale zero | Pass |
| Defeat VFX at time scale zero | Pass |
| Damage report contains all six heroes | Pass |
| Rewards granted once | Pass; second claim rejected |
| Restart | Pass |
| Duplicate critical managers after restart | None; exactly one each |
| MissingReferenceException | None detected in final test log |
| Particle/trail leftovers after pool reuse | None in focused tests |

## Castle Feedback Verification

- Damage raises one `DamageTaken` event and one health update.
- Healing raises `Healed` and health update but no damage event.
- Health remains clamped.
- Defeat fires once.
- Audio scene re-hooking leaves one Castle damage subscription.

## Status And Pool Verification

- No effects: controller disables itself and performs no Update work.
- Active effects: controller enables and uses scaled delta time.
- Burn processes all expected ticks in the tested large delta.
- Slow and Shock expire and disable processing.
- Enemy death/reset clears state.
- Particle hierarchy and trails clear on return.
- Stale activation tokens and double returns are rejected.

## Performance Sanity

- Full run: 10 waves in 77.1 seconds realtime.
- Approximate batch frame rate: 60.0 FPS.
- Peak active enemies observed: 16.
- Peak sampled frame allocation: 728,032 bytes, including scene/test harness and draft work.
- Earlier interactive Editor observation: approximately 57-61 FPS.
- Status optimization eliminates Update work for idle controllers and repeated LINQ-style list predicates.

Remaining common-path allocation risks:

- Enemy spawning still uses `Instantiate` and enemy death can use `Destroy`.
- Some hero abilities create temporary lists.
- VFX instantiate on pool misses, then reuse pooled instances.
- Recruitment instantiates heroes outside the dense steady-state path.

No mobile performance claim is made.

## Defects By Priority

### P0

- None found.

### P1

- None remaining from Task 12A.

### P2

- Enemy pooling is required before validating 60-100 enemy encounters.
- Temporary hero ability collections should be pooled or reused.
- Android IL2CPP build and device profiling are not yet qualified.
- The measured GC peak needs on-device profiler attribution.

### P3

- Move customized Quaternius materials to project-owned copies.
- Remove or ignore `unity_launch_log.txt` after its evidence is no longer needed.
- Modernize two obsolete object-search calls in test-only code when convenient.

## Commits

- Presentation baseline: `9130af948f43d7a0c82dc82fc15771c3a0fd7e7d`.
- Corrective code and tests: `505a2c0e854b66ef53ab38466f789ddfd4fe9410`.
- Documentation: the commit containing this report.

## File Protection Confirmation

- The 10 Quaternius material changes remain preserved and unstaged.
- `ProjectSettings/ProjectSettings.asset` remains preserved and unstaged.
- Unity log files and external safety patches are not included.
- Unity-generated `TimeManager.asset` and solution-file changes from testing were removed before commit.
- Nothing was pushed.

## Exact Next Task

Implement the Gameplay Expansion Vertical Slice with four deeply differentiated heroes, a curated 15-20 card pool, six enemy archetypes, one Elite, one Boss, two traps, and one battlefield defense. Include enemy pooling and dense-wave profiling as enabling work before raising encounter density.
