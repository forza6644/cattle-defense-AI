# Three-Lane Gameplay Correction

Date: 2026-08-17  
Scene: `Assets/_Game/Scenes/V2/GameplayIntegration_V2.unity`  
Status: Implemented in source. Live Unity Test Runner / PlayMode screenshots were not executed in this session (Editor had the project open; Unity MCP was not connected).

## Why the old system was incorrect

`WaveManager.GetRoutePoints()` built a straight line from every portal to a single gate point `(0, 0.1, 0.4)` and then applied `laneHalfWidth = 5.2` as a random lateral offset.

Left, Center, and Right portals therefore **spawned** in three places and **marched as one wide highway**. Lane identity did not exist on the enemy.

## What changed

- Added `CombatLaneRouting` as the single lane-math helper (not a second manager).
- `WaveManager` assigns each spawn to lane 0/1/2 and builds a per-lane route.
- `Enemy.LaneIndex` is set on activate and cleared on pool despawn.
- Existing wave assets keep working: new `WaveData.SpawnEntry.laneAssignment` defaults to `Auto` (serialized 0).
- Portals are sorted by world X so lane 0 is always the leftmost portal.
- Scene gizmos draw the three routes in the Editor. They do not replace the visual master slice.

No new gameplay scene. No duplicate combat systems. Hero/crystal targeting remains range-based.

## How lane assignment works

| Value | Meaning |
|---|---|
| `WaveLaneAssignment.Auto` | Round-robin Left/Center/Right. Bosses use Center. |
| `Left` / `Center` / `Right` | Whole spawn entry stays on that lane (single- or two-lane pressure). |

Production portals in `GameplayIntegration_V2`:

| Portal | Position |
|---|---|
| Portal_Left | `(-10.5, 0.1, 38.6)` |
| Portal_Center | `(0, 0.1, 38.6)` |
| Portal_Right | `(10.5, 0.1, 38.6)` |

`WaveManager.spawnPortals` is empty in the scene and is filled at runtime by finding those three portals, then sorting by X.

Route: spawn X is held for the combat march. Only the **last** waypoint eases 18% toward the gate X so arrival still hits the castle without collapsing into one funnel.

Within-lane scatter is capped at 1.2m (default 0.55m). It is not a substitute for lanes.

Serialized `spawnDepthJitter: 3` in the production scene is clamped to 1.5m at runtime.

## Pooling

`PrepareForSpawn` and `DespawnToPool` set `LaneIndex = -1`.  
`ActivateFromPool(..., laneIndex)` writes the new lane. Reused instances do not keep the previous lane. Activation IDs are unchanged.

## Castle arrival

Unchanged: finishing the path or closing to 2.2m calls `ReachCastle()` → `Castle.TakeDamage`. Side lanes finish at the gate apron on their own X (about 18% toward center) and still use the existing castle HP pipeline.

## Hero / crystal / cards

`HeroAttack` and `StarterCrystal` still target through `EnemyManager.FindTarget` by range. They were not given lane filters. Card draft, run modifiers, and projectile activation IDs were not rewritten.

## Tests added

- EditMode: `CombatLaneRoutingTests`
- PlayMode: `ThreeLaneRoutingPlayModeTests`

Existing pooling, hero, crystal, and card tests were not renamed or disabled.

## Remaining limitations

- Heroes and the starter crystal still target by **range**, not by lane. That is intentional for this phase.
- `GameplayComposition_V2` is still a lookdev leftover and is not loaded in production.
- Legacy `GameScene` has no spawn portals. The shared `WaveManager` synthesizes three columns around `SpawnPoint` at `fallbackLaneSeparation` (3.5m). Production play uses `GameplayIntegration_V2`.
- Live Editor/device visual QA still needs a Play-mode pass in `GameplayIntegration_V2` after Unity recompiles (this session found the Editor in play mode with domain reload disabled, so assemblies were stale).
