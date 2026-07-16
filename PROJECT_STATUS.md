# Stonehold Project Status

Date: 2026-07-17

## Verified Baseline

- Repository: `forza6644/cattle-defense-AI`
- Active branch: `feature/hero-castle-defense-pivot`
- Remote baseline at qualification start: `d099439da47bfae171a8898d5a7306bf94015240`
- Local presentation baseline: `9130af948f43d7a0c82dc82fc15771c3a0fd7e7d`
- Local corrective code/test commit: `505a2c0e854b66ef53ab38466f789ddfd4fe9410`
- Unity: `6000.5.2f1`
- Current milestone: verified Hero Castle Defense baseline; Gameplay Expansion Vertical Slice is next.
- Scenes: `MainMenu` and `GameScene`.

Nothing from Task 12B has been pushed.

## Current Game

Stonehold is a portrait Hero Castle Defense Roguelite. Heroes occupy fixed castle positions, attack automatically, gain temporary run upgrades from level-up drafts, and defend the castle through a 10-wave stage ending in a Warlord boss.

Current hero roster:

1. Archer - fast single-target attacks.
2. Bombardier - splash attacks.
3. Frost Mage - slow/freeze control.
4. Fire Mage - burn and damage over time.
5. Electric Engineer - shock and chain attacks.
6. Sniper - slow, high-damage single-target attacks.

Current content:

- 39 card assets in `Resources/Cards`.
- Current card categories are Recruit Hero and Modifier.
- Current implemented rarities are Common, Rare, and Epic.
- Modifiers cover damage, fire rate, range, burn, slow, shock, ability cooldown/damage/radius, extra projectiles or chains, and critical stats.
- Five enemy data archetypes: Grunt, Runner, Brute, Armored, and Warlord Boss.
- Three authored stages, each referencing 10 waves.
- Stage 1's 10-wave run and boss were exercised by the automated qualification run.

## Progression And Save

- Save format version: 2.
- Persistence: PlayerPrefs through `SaveManager`.
- Saved data includes stage selection/unlocks, best wave, wins/losses/runs, selected starting hero, meta gold, account XP, core materials, hero levels, and meta upgrades.
- Current meta upgrades: castle HP, castle regeneration, hero damage, fire rate, and range.
- Run rewards have a one-claim guard. The full regression test verified that a second claim is rejected.

## Presentation Systems

- Pooled projectile and combat VFX paths.
- Hero-specific attack audio and procedural attack animation timing.
- Status visuals for Slow, Burn, Shock, Stun, and Freeze.
- Victory and Defeat sequences explicitly use unscaled playback while gameplay remains paused.
- Pooled particle hierarchies clear particles and trails, reset transform/time state, and reject stale or duplicate returns.
- Castle damage and healing now use separate events. Damage feedback no longer runs during regeneration.

## Automated Verification

Unity discovered and passed 13 tests:

- EditMode: 9 passed, 0 failed, 0 skipped.
- PlayMode: 4 passed, 0 failed, 0 skipped.
- Total: 13 passed, 0 failed, 0 skipped.

The PlayMode suite verifies result VFX at `Time.timeScale == 0`, duplicate subscription prevention, particle pool cleanup, all six heroes recording damage, 1x/1.5x/2x speed, pause/resume, drafts, 10 waves, boss victory, one-time rewards, and a clean restart with one instance of each critical manager.

Full-run batch evidence:

- Waves: 10/10.
- Draft choices applied: 8.
- Peak enemies observed: 16.
- Elapsed realtime: 77.1 seconds.
- Approximate batch rate: 60.0 frames/second.
- Peak recorded GC allocation in one sampled frame: 728,032 bytes. This includes scene/test harness and draft activity and is not a steady-state combat allocation measurement.

Earlier interactive Editor observation was approximately 57-61 FPS. Neither result is mobile-device evidence.

## Known Technical Debt

- `WaveManager` still instantiates enemies; the 60-100 enemy target requires enemy pooling.
- Some hero ability paths allocate temporary lists.
- First-time VFX creation instantiates objects before their pooled reuse.
- Hero recruitment instantiates hero objects, which is acceptable outside the dense per-frame path.
- Dense 60-100 enemy encounters have not been qualified.
- Android IL2CPP build and physical-device profiling remain unverified.
- The batch GC peak includes test and scene-transition allocations; use Unity Profiler on device before optimization claims.
- Imported Quaternius material customizations should later move to project-owned material copies.

## Protected Local Files

The following pre-existing changes remain preserved and unstaged:

- 10 Quaternius enemy material files.
- `ProjectSettings/ProjectSettings.asset`.
- `unity_launch_log.txt` remains untracked and must not be committed.
- External safety patches remain outside the Unity project.

## Next Approved Task

Build the Gameplay Expansion Vertical Slice: deeply test four active heroes, a curated 15-20 card pool, six enemy archetypes, one Elite, one Boss, two traps, and one battlefield defense. Begin with data/contracts and a narrow playable slice; do not start full paid-asset integration until that slice is fun, stable, and profiled.
