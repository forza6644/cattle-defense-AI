# Stonehold Parity Gap Matrix

This matrix compares the reference gameplay elements (observed in the two videos) against Stonehold's current codebase implementation.

---

## 1. Core Viewport and Camera

| Reference Feature | Stonehold Current | Gap Status | Priority | Action Plan |
|---|---|---|---|---|
| **Portrait Mode (9:16)** | Handled by `CameraRig.cs` and Canvas Scalers | **Matches** | P0 | Keep current implementation |
| **Top-Down Tilting Camera** | Position: `(0, 35, -10.1)`, Rotation: `(70, 0, 0)` | **Matches** | P0 | Keep current implementation |
| **Center-bottom Castle Wall** | Castle wall is at the bottom of the viewport | **Matches** | P0 | Keep current implementation |

---

## 2. Battlefield & Lanes

| Reference Feature | Stonehold Current | Gap Status | Priority | Action Plan |
|---|---|---|---|---|
| **Winding Path** | Waypoint-based path with lane spread | **Matches** | P0 | Keep current implementation |
| **Spawning** | Spawner at top, Castle at bottom | **Matches** | P0 | Keep current implementation |
| **Wall Plinths / Slots** | 6 plinths/slots created in `StagePresentationController.cs` | **Different count** | P1 | Reference shows 3 slots. Update castle presentation in `StagePresentationController.cs` to show 3 slots (or align slots configuration with stage 1). |

---

## 3. UI and HUD

| Reference Feature | Stonehold Current | Gap Status | Priority | Action Plan |
|---|---|---|---|---|
| **Top HUD Layout** | Top-left: Speed control. Center: Wave count / Stage. Top-right: Pause. | **Partial** | P1 | Align UIManager layout to match top HUD composition where appropriate. |
| **HP Indicator** | HP is shown at the bottom center of the screen on the castle wall itself (heart icon + bar + text). | **Partial** | P1 | Place/align HP bar at the bottom center of the screen to match. |
| **Speed Toggle** | Speed button cycles 1x -> 1.5x -> 2x | **Matches** | P1 | Keep current speed control. |

---

## 4. Defender / Hero System

| Reference Feature | Stonehold Current | Gap Status | Priority | Action Plan |
|---|---|---|---|---|
| **3 Placed Defenders** | Player has up to 6 plinths and places towers | **Different** | P1 | Tune Stage 1 slot configuration. Standardize the slot positioning for Stage 1 to focus on 3 primary slots for alignment. |
| **Hero-based attacks** | Towers fire projectiles. HeroAttack fires abilities. | **Matches** | P0 | Keep current hybrid tower/hero setup. |

---

## 5. Wave & Pacing Tuning (Stage 1)

| Reference Feature | Stonehold Current | Gap Status | Priority | Action Plan |
|---|---|---|---|---|
| **Wave count: 10/20** | Stage 1 has 10 waves. | **Matches** | P0 | Ensure Stage 1 runs exactly 10 waves. |
| **Starting Gold** | `startingGold = 150` | **Matches** | P1 | Tune starting gold via `GameConfig.asset`. |
| **Pacing / Delays** | 5s between waves | **Matches** | P1 | Keep current pacing parameters. |
