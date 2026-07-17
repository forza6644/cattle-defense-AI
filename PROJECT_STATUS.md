# Stonehold Project Status

Date: 2026-07-17

## Verified Baseline

- Repository: `forza6644/cattle-defense-AI`
- Qualified remote baseline: `7f7101dfdf822f6e1b6c55845f59772003d5625e`
- Task 13B source baseline: `21a10784dd4d7349534f90be8eebed82c8b81699`.
- Active branch: `feature/enemy-pooling`
- Task 13A local commit: this commit, `Gameplay expansion data contracts and validation` (exact hash is reported after creation).
- Remote baseline at qualification start: `d099439da47bfae171a8898d5a7306bf94015240`
- Local presentation baseline: `9130af948f43d7a0c82dc82fc15771c3a0fd7e7d`
- Local corrective code/test commit: `505a2c0e854b66ef53ab38466f789ddfd4fe9410`
- Qualified baseline documentation commit: `c068fb5a6daf785e8150ef28f1b37d2a2fa7efa5`
- Unity: `6000.5.2f1`
- Current milestone: Task 13B qualified Enemy Pooling lifecycle complete; four-hero behavior upgrades are next.
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

Unity discovered and passed 58 tests:

- EditMode: 34 passed, 0 failed, 0 skipped (9 previous and 25 new Task 13A tests).
- PlayMode: 24 passed, 0 failed, 0 skipped (4 previous and 20 new Task 13B tests).
- Total: 58 passed, 0 failed, 0 skipped.

The PlayMode suite verifies result VFX at `Time.timeScale == 0`, duplicate subscription prevention, particle pool cleanup, all six heroes recording damage, 1x/1.5x/2x speed, pause/resume, drafts, 10 waves, boss victory, one-time rewards, and a clean restart with one instance of each critical manager.

Full-run batch evidence:

- Waves: 10/10.
- Draft choices applied: 9.
- Peak targetable enemies observed: 13.
- Elapsed realtime: 84.3 seconds.
- Approximate batch rate: 60.0 frames/second.
- Peak recorded GC allocation in one sampled frame: 687,451 bytes. This includes scene/test harness and draft activity and is not a steady-state combat allocation measurement.

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
- Sixth enemy, Elite content, expanded card assets, UI changes, or balance changes.

## Task 13B Enemy Pooling

- Source baseline: `21a10784dd4d7349534f90be8eebed82c8b81699`.
- Local commit: this commit, `Implement qualified enemy pooling lifecycle` (exact hash is reported after creation).
- One scene-owned `EnemyPoolManager` maintains a pool per stable EnemyData ID under an inactive runtime root.
- Current keys: `grunt`, `runner`, `brute`, `armored`, and `warlord_boss`.
- Normal pools prewarm 3 instances; Boss pools prewarm 1. Pools expand on demand and record created, active, inactive, peak, expansion, reuse, and invalid-return counts.
- `Enemy` now has explicit prepare, activate, and despawn phases. Health, death/reward flags, castle-arrival state, paths, status effects, visuals, animation state, colliders, rigidbodies, health bars, and registrations reset between activations.
- Activation IDs protect reused enemies from stale projectiles and delayed death returns.
- Combat death grants rewards once and returns after death presentation. Castle arrival damages once, grants no kill reward, and returns immediately.
- `EnemyManager` contains only active targetable enemies. Result and restart boundaries despawn every active enemy.
- The deterministic stress test completed 100 status-bearing spawn/despawn cycles without new instances after prewarm, expansions, invalid returns, registry mismatches, or exceptions.
- Ten-wave pool diagnostics: Grunt peak 10/created 10, Runner peak 17/created 17, Brute peak 7/created 7, Armored peak 9/created 9, Warlord peak 2/created 2; all ended active 0 with invalid returns 0.
- The observed 17 pooled Runner activations include enemies in death presentation; the active targetable registry peaked at 13.
- This qualifies current content only. It does not qualify a real 60-100-enemy encounter, Android, or physical-device performance.

## Known Technical Debt

- Dense 60-100 enemy gameplay still requires a separate production encounter and physical-device profile; Task 13B only qualifies lifecycle reuse.
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

Implement behavior-changing upgrade branches for four active heroes using the Task 13A contracts. Preserve the qualified pooling lifecycle and current balance, then curate the 15-20 card test pool.
