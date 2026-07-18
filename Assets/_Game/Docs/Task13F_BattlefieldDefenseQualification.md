# Task 13F Battlefield Defense Qualification

Date: 2026-07-18

## Baseline And Scope

- Source: `2eb9db88cf3e53c2997ca320c425beea51999d57`.
- Branch: `feature/battlefield-defenses`.
- Adds two isolated traps and one isolated battlefield defense using fixed automatic anchors.
- No grid, free placement, drag, rotation UI, or production scene fixture was added.
- Production waves, the default 39-card pool, and `VerticalSlice18` are unchanged.

## Anchor Architecture

`BattlefieldAnchor` registers enabled Trap or Defense anchors without scene scans. `BattlefieldAnchorManager` caches registrations, claims the first valid unoccupied anchor, rejects missing or occupied anchors, and clears occupancy on restart. Qualification tests create controlled anchors outside production stage data.

Cards deploy through `CardDraftManager`. A Trap or BattlefieldDefense card enters `RunModifierManager.ActiveCards` only after a valid deployment succeeds.

## Caltrops

- ID `trap_caltrops`; Common Trap; automatic LaneAnchor placement.
- Radius 2; duration 18 seconds; physical damage 2 every 0.9 seconds.
- Slow multiplier 0.72 for 1.2 seconds, adjusted by crowd-control resistance.
- Maximum active 2; maximum 20 ticks per enemy activation.
- Uses normal armor damage and existing non-stacking Slow status. Activation IDs prevent pooled reuse from inheriting tick limits.

## Burning Oil

- ID `trap_burning_oil`; Rare Trap; automatic LaneAnchor placement.
- Radius 2.5; waiting lifetime 20 seconds; ignition tell 0.65 seconds.
- Burning duration 5 seconds; burn value 3; refresh interval 0.75 seconds; status duration 1.1 seconds.
- Maximum active 1; maximum 8 applications per enemy activation.
- It is inert before trigger, cannot recursively create zones, and removes its own sourced status effects when removed or restarted.

## Wooden Barricade

- ID `defense_wooden_barricade`; Rare BattlefieldDefense.
- Health 90; armor 2; maximum active 1; no damage, regeneration, reward, gold, or XP.
- Melee enemies stop and attack through one `TakeDamage` API. Crossbow Raider prioritizes the active barricade and cannot damage the castle through it. Warlord can destroy it. War Shaman and heroes cannot target it.
- Destruction releases its anchor and clears the manager target.

## Qualification Assets

- Definitions, cards and pool: `Assets/_Game/ScriptableObjects/BattlefieldDefenseQualification/`.
- Prefabs: `Assets/_Game/Prefabs/BattlefieldDefenseQualification/`.
- Materials: `Assets/_Game/Art/Materials/BattlefieldDefenseQualification/`.
- Cards: `deploy_caltrops`, `deploy_burning_oil`, `deploy_wooden_barricade`.
- Pool: `task13f_qualification`, outside `Resources/Cards`.

## Runtime And Reset Safety

`TrapRuntimeManager` and `BattlefieldDefenseManager` reuse inactive runtime objects and skip destroyed external references safely. `GameManager` creates single manager instances and resets traps, defense, occupancy, sourced status effects and delayed work at run start. No per-frame scene scan, LINQ loop, or repeated Instantiate/Destroy path is used after warmup.

## Validator

- Cards 50 total: production 39 remains unchanged; qualification adds 2 Trap and 1 BattlefieldDefense.
- Enemies 7; Traps 2; Defenses 1; CardPools 2.
- Errors 0; warnings 34 supported legacy Modifier warnings.

## Automated Qualification

- Previous suite: 141 tests.
- New Task 13F: 13 EditMode and 19 PlayMode tests.
- Final: 88/88 EditMode and 85/85 PlayMode; 173/173 total; 0 failed; 0 skipped.
- Production ten-wave, Warlord, victory/defeat, one-time rewards, restart, pause/speed, card draft and pooling regressions remain green.

## Stress And Controlled Encounter

- 300 Caltrops deployments, 300 Burning Oil deployments and 200 Barricade create/destroy cycles.
- Trap runtime created at most two objects during the isolated stress interval and reused at least 598 times; defense created at most one and reused at least 199 times.
- Cleanup: active traps 0, burning zones 0, barricades 0, occupied anchors 0, stale ticks 0, stale targets 0.
- Existing enemy-pool regression retains invalid returns 0, reward-once behavior and clean projectile/registry cleanup.
- Controlled fixture includes four active HeroAttack components, Runner, Armored, Crossbow Raider, War Shaman, Warlord, both traps and the barricade. It completes without soft-lock and resets all managers and anchors.

## Performance And Limitations

Runtime candidate selection uses the cached EnemyManager registry and bounded dictionaries/lists. Prototype objects are pooled after warmup. Final art, animation, audio, physical Android profiling and the final balanced production encounter are P3 follow-up work.

## Protected Local State

Ten Quaternius materials, `ProjectSettings/EditorSettings.asset`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/TimeManager.asset`, and `TD catle defence.slnx` remain local and unstaged. `unity_launch_log.txt` was not recreated.

## Decision

Final revalidation passed with 88/88 EditMode and 85/85 PlayMode tests, 173/173 total, and zero validator errors. The remaining final art, audio, physical Android profiling, and dense-encounter profiling items are non-blocking P3 follow-up work.

**QUALIFIED**

Task 13F is complete. Next task: balanced ten-wave expansion run and densest-encounter profiling.
