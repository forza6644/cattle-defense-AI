# Stonehold Project Status

Date: 2026-07-17

## Verified Baseline

- Repository: `forza6644/cattle-defense-AI`
- Qualified remote baseline: `7f7101dfdf822f6e1b6c55845f59772003d5625e`
- Active branch: `feature/gameplay-expansion-foundation`
- Task 13A local commit: this commit, `Gameplay expansion data contracts and validation` (exact hash is reported after creation).
- Remote baseline at qualification start: `d099439da47bfae171a8898d5a7306bf94015240`
- Local presentation baseline: `9130af948f43d7a0c82dc82fc15771c3a0fd7e7d`
- Local corrective code/test commit: `505a2c0e854b66ef53ab38466f789ddfd4fe9410`
- Qualified baseline documentation commit: `c068fb5a6daf785e8150ef28f1b37d2a2fa7efa5`
- Unity: `6000.5.2f1`
- Current milestone: Task 13A gameplay-expansion data contracts and validation foundation complete; Enemy Pooling is next.
- Scenes: `MainMenu` and `GameScene`.

Task 12B performed local qualification only; Task 12C owns the remote handoff.

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

Unity discovered and passed 38 tests:

- EditMode: 34 passed, 0 failed, 0 skipped (9 previous and 25 new Task 13A tests).
- PlayMode: 4 passed, 0 failed, 0 skipped.
- Total: 38 passed, 0 failed, 0 skipped.

The PlayMode suite verifies result VFX at `Time.timeScale == 0`, duplicate subscription prevention, particle pool cleanup, all six heroes recording damage, 1x/1.5x/2x speed, pause/resume, drafts, 10 waves, boss victory, one-time rewards, and a clean restart with one instance of each critical manager.

Full-run batch evidence:

- Waves: 10/10.
- Draft choices applied: 8.
- Peak enemies observed: 14.
- Elapsed realtime: 76.3 seconds.
- Approximate batch rate: 60.0 frames/second.
- Peak recorded GC allocation in one sampled frame: 698,710 bytes. This includes scene/test harness and draft activity and is not a steady-state combat allocation measurement.

Earlier interactive Editor observation was approximately 57-61 FPS. Neither result is mobile-device evidence.
The Task 12A interactive Editor run also reached the end of the 10-wave stage; the Task 12B automated run supplied the final repeatable qualification evidence.

## Task 13A Data Foundation

Implemented contracts only:

- Card categories now preserve legacy `Modifier=0` and `RecruitHero=1`, then append Hero Upgrade, Global Upgrade, Trap, Battlefield Defense, Castle Upgrade, Legendary Modifier, and Reroll.
- Card rarity preserves Common, Rare, and Epic numeric values and appends Legendary.
- Behavior-upgrade data supports stable effect enums, hero or attack-family targets, integer/float/percentage/duration/count values, secondary values, and stack limits.
- Trap, battlefield-defense, castle-upgrade, and reroll ScriptableObject contracts are inspectable but have no runtime execution yet.
- Enemy data now exposes stable IDs, Normal/Elite/Boss classification, shield, dodge, elemental resistance, and crowd-control resistance contracts. Warlord is classified as Boss without changing its health, speed, armor, reward, or castle damage.
- A future-facing `DamageContext` carries damage type, source hero ID, critical state, and armor-piercing information; active attacks still use the proven legacy damage path.
- Save Version 2 is unchanged. Temporary run traps and defenses are not persisted.
- Read-only validation reports category/rarity counts, duplicate or missing IDs, target/execution errors, invalid values, enemy classification, defense ranges, and legacy Modifier migration warnings.

Validation result:

- 39 cards: 34 legacy Modifier and 5 Recruit Hero; 17 Common, 18 Rare, 4 Epic, 0 Legendary.
- 5 enemies: 4 Normal, 0 Elite, 1 Boss.
- 0 validation errors and 34 expected legacy Modifier warnings.

Not implemented in Task 13A:

- Runtime behavior-changing hero upgrades.
- Functional traps, battlefield defenses, castle-upgrade cards, rerolls, shields, dodge, elemental resistance, or crowd-control resistance.
- Sixth enemy, Elite content, Enemy Pooling, expanded card assets, UI changes, or balance changes.

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

Task 13B: implement Enemy Pooling without changing the current ten-wave behavior or balance. Preserve Enemy registration, health/status reset, path assignment, castle attacks, rewards, death animation/VFX, boss victory, restart lifecycle, and current test coverage. Runtime hero-upgrade behavior follows after pooling is qualified.
