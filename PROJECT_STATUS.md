# Stonehold Project Status

Date: 2026-07-18

## Verified Baseline

- Repository: `forza6644/cattle-defense-AI`
- Qualified remote baseline: `7f7101dfdf822f6e1b6c55845f59772003d5625e`
- Task 13C source baseline: `5f5810479268d8619506e4db2f3c6cb5bfff1741`.
- Task 13D source baseline: `8f34d660a584bd24f169dbeeaaa97a1f90276a31`.
- Task 13E source baseline: `92b28022da2b73eccd0bb89fe8a0272537e6a28d`.
- Active branch: `feature/enemy-roster-expansion`.
- Original Task 13C implementation commit: `7e483ac8a6eacd47caa8d58a7af15e260fe71bbf`.
- Task 13C corrective qualification commit: `3acfff4cf87c620e0f8348fa719a696c0310af89`.
- Task 13A local commit: this commit, `Gameplay expansion data contracts and validation` (exact hash is reported after creation).
- Remote baseline at qualification start: `d099439da47bfae171a8898d5a7306bf94015240`
- Local presentation baseline: `9130af948f43d7a0c82dc82fc15771c3a0fd7e7d`
- Local corrective code/test commit: `505a2c0e854b66ef53ab38466f789ddfd4fe9410`
- Qualified baseline documentation commit: `c068fb5a6daf785e8150ef28f1b37d2a2fa7efa5`
- Unity: `6000.5.2f1`
- Current milestone: Task 13E ranged normal enemy and healing Elite are locally qualified and remain isolated from production waves.
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
- Seven enemy data archetypes: five production archetypes plus the isolated Crossbow Raider and War Shaman qualification content.
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

Task 13E qualification passes 141 automated tests:

- EditMode: 75 passed, 0 failed, 0 skipped.
- PlayMode: 66 passed, 0 failed, 0 skipped.
- Previous Task 13D baseline: 103 tests.
- New Task 13E coverage: 38 tests (12 EditMode and 26 PlayMode).
- Total: 141 passed, 0 failed, 0 skipped.

The Task 13E suite covers stable IDs and classification, ranged stand-off/wind-up/projectile safety, healing selection and boss exclusion, pool reuse, activation-ID wrap, all six hero damage paths, status and reward reset, 700 pooled expansion-enemy activations, controlled ranged/healing combat, cleanup, and the unchanged production ten-wave regression.

Unity discovered and passed 78 tests after Task 13C:

- EditMode: 39 passed, 0 failed, 0 skipped.
- PlayMode: 39 passed, 0 failed, 0 skipped.
- Previous Task 13B baseline: 58 tests.
- New Task 13C coverage: 20 tests (5 EditMode and 15 PlayMode).
- Total: 78 passed, 0 failed, 0 skipped.

The PlayMode suite verifies result VFX at `Time.timeScale == 0`, duplicate subscription prevention, particle pool cleanup, all six heroes recording damage, 1x/1.5x/2x speed, pause/resume, drafts, 10 waves, boss victory, one-time rewards, and a clean restart with one instance of each critical manager. Task 13C adds enabled-HeroAttack integration, all eight prototype behaviors, stack rejection, activation reuse/wrap, stale-target protection, projectile reset, cluster attribution, and delayed-echo restart invalidation.

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

## Task 13C Hero Behavior Upgrades

- Selected heroes: Archer, Bombardier, Frost Mage, and Electric Engineer.
- Eight prototype upgrades: Twin Volley, Piercing Arrows, Cluster Shells, Wide Blast, Shard Volley, Echoing Nova, Extended Circuit, and Forked Current.
- Prototype cards remain outside `Resources/Cards`; the production draft remains exactly 39 cards.
- Runtime behavior is applied through `RunModifierManager.AddCard` and the enabled `HeroAttack` fire/update paths.
- Piercing always reaches its selected target and adds bounded 80%-damage pierced targets.
- Cluster shells are bounded, cannot recursively split, and retain `bombardier` DamageTracker attribution.
- Echoing Nova is invalidated when run modifiers are cleared or the run restarts.
- Enemy activation IDs are non-zero and unique across simultaneous enemies, pool reuse, static reset/wrap, stale projectile checks, and delayed despawn callbacks.
- Projectile reuse clears target activation, source, status, piercing, cluster, trail, and ability state before reuse.
- The final 39-test PlayMode run repeated the complete 10-wave Warlord victory, rewards, result, pooling, and restart regression after the Task 13C correction.

## Task 13D Curated Card Pool

- `VerticalSlice18` is a project-owned `CardPoolDefinition` at `Assets/_Game/ScriptableObjects/CardPools/VerticalSlice18.asset`.
- The production default remains the unchanged 39 cards in `Resources/Cards`; no scene uses the override by default.
- Archer is the controlled starting hero. Bombardier, Frost Mage, and Electric Engineer are guaranteed recruit candidates while a slot and valid recruit remain.
- The pool contains 3 recruit cards, 8 qualified behavior upgrades, and 7 reused production support modifiers.
- Effective pool rarity is 10 Common, 6 Rare, 2 Epic, and 0 Legendary without mutating referenced card assets.
- Eligibility is evaluated before weighted selection. Hero cards require active targets, max-stack upgrades disappear, duplicate choices are impossible, and a fixed seed is deterministic for tests.
- The selector uses a cached roster snapshot and performs no scene scans in the weighted choice loop. Its small lists, hash set, and random generator are bounded per draft.
- Read-only validation reports 0 errors and 34 intentional legacy Modifier warnings.
- Automated result: 63/63 EditMode and 40/40 PlayMode, for 103/103 total. The original 78 tests remain intact and 25 Task 13D tests were added.
- The 500-run simulation generated 5,000 drafts and 15,000 choices with zero invalid drafts, duplicates, short drafts, recruit-guarantee failures, or max-stack violations. Every card was offered.
- Controlled PlayMode qualified the real draft manager override, recruitment, post-recruit upgrade eligibility, stack rejection, and restart clearing.
- Production PlayMode again completed all ten waves, Warlord victory, Defeat, one-time rewards, restart, pause/speed controls, and clean pooling.

## Task 13E Enemy Roster Expansion

- `crossbow_raider` is an isolated Normal enemy with 17 HP, speed 3, 5.5-unit stand-off range, 0.75-second wind-up, 2.1-second cooldown, projectile speed 10, 2 castle damage, 6 gold, and 6 XP.
- `elite_war_shaman` is an isolated Elite with 75 HP, speed 1.7, armor 1, a 5-second pulse interval, 1-second cast, 4-unit radius, 12% max-health healing, 50% self-heal multiplier, five-target cap, 3 castle damage, 45 gold, and 40 XP.
- Normal enemy pools prewarm 3 by default; Elite pools prewarm 1. Pool keys remain stable EnemyData IDs and expansion remains supported.
- Crossbow shots use a dedicated pooled castle projectile with source activation-token checks, one-hit return, trail reset, invalid-target return, and restart cleanup.
- War Shaman pulses select the lowest-health bounded nearby active non-boss enemies without reviving, overhealing, or retaining stale pooled references.
- Project-owned prefabs, materials, telegraphs, qualification data, and an isolated test wave were added. No production wave references either enemy.
- Read-only validation reports 47 cards, 7 enemies (5 Normal, 1 Elite, 1 Boss), 0 errors, and the existing 34 intentional legacy Modifier warnings.
- The production default remains the unchanged 39-card `Resources/Cards` pool. `VerticalSlice18` and the production ten-wave stage remain unchanged.
- Detailed evidence is recorded in `Assets/_Game/Docs/Task13E_EnemyRosterQualification.md`.

## Known Technical Debt

- Dense 60-100 enemy gameplay still requires a separate production encounter and physical-device profile; Task 13B only qualifies lifecycle reuse.
- Some hero ability paths allocate temporary lists.
- First-time VFX creation instantiates objects before their pooled reuse.
- Hero recruitment instantiates hero objects, which is acceptable outside the dense per-frame path.
- Dense 60-100 enemy encounters have not been qualified.
- Android IL2CPP build and physical-device profiling remain unverified.
- The batch GC peak includes test and scene-transition allocations; use Unity Profiler on device before optimization claims.
- Imported Quaternius material customizations should later move to project-owned material copies.
- Task 13E uses project-owned prototype accents and placeholders; final character art and mobile-device readability still require later visual integration and profiling.

## Protected Local Files

The following pre-existing changes remain preserved and unstaged:

- 10 Quaternius enemy material files.
- `ProjectSettings/ProjectSettings.asset`.
- `ProjectSettings/TimeManager.asset`.
- `ProjectSettings/EditorSettings.asset` (unrelated local Unity test-setting change).
- `TD catle defence.slnx`.
- `unity_launch_log.txt` was deleted by the earlier Antigravity Task 13C execution. It was not recreated and is not committed.
- External safety patches remain outside the Unity project.

## Next Approved Task

Task 13F: add two traps and one battlefield defense without reintroducing tower-placement gameplay. Preserve the qualified enemy roster, 39-card production default, and isolated `VerticalSlice18` test pool.
