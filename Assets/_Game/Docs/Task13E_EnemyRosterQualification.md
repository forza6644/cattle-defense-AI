# Task 13E Enemy Roster Qualification

Date: 2026-07-18

## Baseline And Scope

- Source baseline: `92b28022da2b73eccd0bb89fe8a0272537e6a28d`.
- Branch: `feature/enemy-roster-expansion`.
- Scope: one isolated ranged Normal enemy and one isolated healing Elite.
- Production ten-wave data, the 39-card default pool, and `VerticalSlice18` were not changed.
- Task 13F was not started.

## Existing Architecture Audit

- EnemyData supplies stable IDs and Normal, Elite, or Boss classification.
- EnemyPoolManager keys pools by stable ID, prewarms by classification, expands on demand, and records created, active, inactive, peak, expansion, reuse, and invalid-return diagnostics.
- Enemy assigns a non-zero activation ID per pooled activation, registers only active targetable instances with EnemyManager, clears runtime state on return, and guards delayed death callbacks with the activation ID.
- Damage and rewards use the existing Enemy damage/death path; each death grants its reward once. Castle arrival uses the existing Castle damage API and grants no kill reward.
- Existing hero projectiles retain target activation IDs, so stale shots cannot damage a reused enemy.
- Death presentation completes before pool return. Health, status, registration, paths, visuals, colliders, animation state, reward state, castle-arrival state, and special-action state are cleared on reuse.
- Dense target selection uses EnemyManager's registry. Task 13E adds no per-frame scene scans or LINQ in enemy combat updates.

## Crossbow Raider

- Stable ID: `crossbow_raider`.
- Classification: Normal.
- Health 17; movement speed 3; armor 0.
- Gold 6; XP 6; castle damage 2.
- Stand-off range 5.5; wind-up 0.75 seconds; cooldown 2.1 seconds; projectile speed 10.
- Counterplay: low health, visible wind-up, bright ranged tell, and a readable projectile trail. Accumulated Raiders become a clear priority target.
- It walks normally, stops at exact stand-off distance, faces the castle, winds up, and fires one pooled bolt through the existing Castle damage API.

## War Shaman

- Stable ID: `elite_war_shaman`.
- Classification: Elite.
- Health 75; movement speed 1.7; armor 1.
- Gold 45; XP 40; castle damage 3.
- Pulse interval 5 seconds; cast time 1 second; radius 4; heal 12% of target maximum health; self-heal multiplier 0.5; target cap 5.
- Boss exclusion is enabled.
- Counterplay: larger silhouette, Elite indicator, cast ring, and healing pulse. Killing or interrupting the Elite ends sustain.
- Pulses select the lowest-health valid active nearby non-boss enemies. They do not revive, over-heal, mutate stats, or retain targets after pooling.

## Assets

- Data and isolated wave: `Assets/_Game/ScriptableObjects/EnemyExpansionQualification/`.
- Prefabs: `Assets/_Game/Prefabs/EnemyExpansion/`.
- Project-owned materials: `Assets/_Game/Art/Materials/EnemyExpansion/`.
- Builder: `Assets/_Game/Editor/EnemyRosterExpansionBuilder.cs`.
- Runtime behavior: `Assets/_Game/Scripts/Gameplay/EnemySpecialBehavior.cs`.
- Ranged projectile: `Assets/_Game/Scripts/Gameplay/EnemyCastleProjectile.cs`.

Normal pools prewarm 3 by default and Elite pools prewarm 1. The qualification stress test explicitly prewarmed four Raiders and one Shaman. Both pool types retain on-demand expansion.

## Reset And Stale Safety

- Crossbow reset clears action state, wind-up, cooldown, castle target, activation token, telegraph state, and pending attack execution.
- Pooled bolts reset source, source activation token, target, damage, speed, hit state, trail, and active-set membership.
- War Shaman reset clears cast state, cooldown, candidate buffer usage, activation token, and telegraph/pulse state.
- Every delayed action checks the source activation token. Death, pool return, restart, invalid targets, and source reuse cancel stale work.
- Restart cleanup despawns all active expansion enemies and all active enemy castle projectiles.

## Validator

Read-only validation result:

- Cards: 47 assets discovered across project paths; production default remains 39 cards.
- Enemies: 7 total (5 Normal, 1 Elite, 1 Boss).
- Errors: 0.
- Warnings: 34 intentional supported legacy Modifier-category warnings.
- Neither expansion enemy is referenced by production waves.

## Automated Tests

- Previous baseline: 103 tests.
- New EditMode tests: 12.
- New PlayMode tests: 26.
- EditMode result: 75 passed, 0 failed, 0 skipped.
- PlayMode result: 66 passed, 0 failed, 0 skipped.
- Grand total: 141 passed, 0 failed, 0 skipped.

Coverage includes data validity, unique IDs, classification, prefab and special-role contracts, production isolation, pool prewarm/reuse, stand-off movement, cancellable wind-up and pulse, one-hit projectile behavior, stale-source cancellation, all six hero damage paths, status reset, reward-once behavior, activation-ID wrap, cleanup, controlled combat, and the unchanged ten-wave Warlord regression.

## Stress And Controlled Encounter

- Stress activations: 500 Crossbow Raiders and 200 War Shamans.
- Stress prewarm: Raider 4, Shaman 1.
- Sequential stress pool expansions: 0; peak active: 1 per type; reuse counts at least 500 and 200.
- Cleanup: active enemies 0; registry 0; active enemy projectiles 0; invalid returns 0; stale ranged hits 0.
- Focused lifecycle tests separately cover repeated return/reuse, activation-ID wrap, death cancellation, pool-return cancellation, restart cleanup, and stale healing-target removal.
- Controlled qualification includes two Raiders, two Shamans, an allied normal target, four simultaneous active HeroAttack instances, multiple ranged projectiles in flight, two healing pulses, and real Slow/Shock status paths. A focused role encounter also verified castle damage and that killing the Shaman stops support. Cleanup ended with registry/projectiles at zero.
- Hero integration exercises Archer, Bombardier, Frost Mage, Fire Mage, Electric Engineer, and Sniper against both new roles. Damage attribution and one-time rewards remained valid.

## Production Regression

- Full EditMode: 75/75 passed.
- Full PlayMode: 66/66 passed in 106.04 seconds after the final controlled-encounter gate.
- Existing ten-wave stage and Warlord victory regression passed.
- Defeat, result, one-time rewards, restart, pause/resume, 1x/1.5x/2x speed, status reset, manager lifecycle, enemy/projectile pooling, and card draft regression passed.
- Production waves do not reference either Task 13E enemy.
- The default 39-card production pool and `VerticalSlice18` remain unchanged.

## Performance And Limitations

- Repeated enemy combat uses pooled enemies and pooled ranged projectiles after warmup.
- Candidate selection uses bounded reusable arrays; no scene scan or LINQ was added to dense updates.
- Prototype telegraph objects are created once per pooled enemy instance and reset with that instance.
- Stress qualification is deterministic Editor PlayMode evidence, not physical-device profiling.
- Final character assets, animation, sound, balance in production waves, and mobile-device profiling remain later work.

## Protected Local State

The ten modified Quaternius materials, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/TimeManager.asset`, unrelated local `ProjectSettings/EditorSettings.asset`, and `TD catle defence.slnx` remain local and unstaged. `unity_launch_log.txt` was not recreated.

## Decision

Task 13E is locally qualified. Next task: two traps and one battlefield defense.
