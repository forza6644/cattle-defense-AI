# Stonehold Reference Gameplay Analysis (Corrected)

> **Document Status**: Audit & Parity Corrected  
> **Source Files**: `ScreenRecording_07-21-2026 11-23-12_1.mp4.mp4` (Video 1, 03:39) and `ScreenRecording_07-21-2026 11-30-10_1.mp4.mp4` (Video 2, 00:46)

---

## 1. Confirmed Reference Video Observations vs. Assumptions

### Confirmed Video Observations (Empirical Evidence)
- **Viewport & Orientation**: High-angle fixed top-down perspective, full Portrait mode (~9:16 aspect ratio).
- **Castle & Defender Composition**: The Castle Wall spans horizontally across the bottom of the screen. There are **exactly 3 primary active defender positions (plinths)** on the wall.
- **Wave System & Display**: Top-center HUD displays `Canyon / Stage 86`, `Wave 7/20` (and `Wave 9/20`), with a numerical timer (`00:53`, `01:18`) and a **horizontal Battle Points / Wave Progress Slider** directly beneath the stage info text. Total stage wave count shown in reference is **20 waves**.
- **Top HUD Composition**:
  - Top-Left: Speed-control toggle displaying multiplier (e.g. `1.3x` / `1.0x` / `2.0x` toggle cycle).
  - Top-Center: Stage Name, Stage Number, Wave Counter (`Wave X/20`), Elapsed/Remaining Time, and Wave Progress Bar.
  - Top-Right: Pause Button.
- **Bottom HUD Composition**: Numerical Castle HP bar (e.g. `3203 / 6073` or `6073 HP`) with a heart icon and fill bar situated directly over the castle wall at the bottom center.
- **Path & Spawning**: Enemies spawn from the top of a winding canyon path and advance down toward the bottom castle wall.
- **Enemy Formations**: Enemies move in structured lines, small grid blocks, and staggered formations. Heavy shielded grunts hold shields in front.
- **Defender Weapons**: 3 active defenders exhibit visibly distinct projectile behaviors (Fast single-target, Splash/Cannon, Control/Magic).
- **Out-of-Battle Meta Systems** (Video 2): "Your Weapons" card library (Upgrade with gold + scrolls), Character details page ("Lawris", Level 5, Fire stats, damage, cooldown, area size), 3-to-1 Gear Fusion/Forge.

### Assumptions & Non-Video Inferences
- Specific monetization algorithms (e.g. exact Jackpot percentages in Lucky Trader) are meta-features outside the tactical battle loop.
- Exact XP math per mob kill is inferred from battle progression standards rather than explicit video UI readouts.

---

## 2. Observable Timing Breakdown

| Timestamp | Video | Screen / Event | Observed Behavior & Timings |
|---|---|---|---|
| `00:00 - 00:40` | Video 1 | Town & Character Screen | Daily offers ("Lucky Trader"), Monk gear slots (Power 2015). |
| `00:41 - 01:05` | Video 1 | Stage Select & Town | Act 4 "Scavenger Siege", Stage 87 active. 3 Star clear thresholds visible. |
| `01:06 - 01:50` | Video 1 | Battle Start (Stage 86) | Transition to Canyon battlefield. Camera fixed high portrait angle. |
| `01:51 - 02:25` | Video 1 | Mid-Battle (Wave 7/20) | Speed set to 1.3x. Wave 7/20 active at 00:53 timer. Enemies in structured grid formation. |
| `02:26 - 03:00` | Video 1 | Late-Battle (Wave 9/20) | Wave 9/20 active at 01:18 timer. Shielded grunts in dense line formation. Castle HP at 3203. |
| `03:01 - 03:39` | Video 1 | Market / Exit | Transition to Market ("Wheel of Whispers"). |
| `00:00 - 00:15` | Video 2 | Weapons Library | 8/10 Unlocked. Shows upgrade cards with scroll requirements (e.g. 1008/3075). |
| `00:16 - 00:30` | Video 2 | Character Detail (Lawris) | Level 5 Fire Hero, Damage 4461, Cooldown 5.8s, Area Damage 894.6, Area Size 3.9. |
| `00:31 - 00:46` | Video 2 | Forge / Gear Fusion | 3-to-1 gear fusion interface for upgrading item rarity tier. |

---

## 3. Detailed System Classification Matrix

| System | Classification | Reference Observed | Stonehold Current | Audit Notes |
|---|---|---|---|---|
| Viewport & Aspect | **MATCHED** | Portrait mode (~9:16), high-angle top-down | `CameraRig.cs` forces portrait FOV & composition | Identical angle & framing |
| Castle HP HUD | **MATCHED** | Bottom-center over wall with heart & fill | `UIManager.cs` bottom-center HP bar | Identical placement & readout |
| Speed Controls | **MATCHED** | Cycle button (1x / 1.5x / 2x) at top-left | `GameManager.cs` speed cycle | Identical behavior |
| Wave Total Count | **DIFFERENT** | **20 Total Waves** shown in stage | Legacy Stage 1 has 10 waves | **Requires dedicated 20-wave parity stage** |
| Active Defender Slots | **DIFFERENT** | **3 Primary Active Defender Slots** | Scene/Code has 6 `HeroSlot`s & 5 `TowerSlot`s | **Parity stage will use 3 primary active defender slots** |
| Top Battle HUD | **PARTIALLY MATCHED** | Speed (top-left), Stage/Wave/Timer (center), Pause (right), Progress Bar below | Speed/Pause at top-right, Wave top-center | **Reposition top HUD elements to match reference layout** |
| Wave Progress Bar | **MISSING** | Horizontal progress bar below stage details | Not present in legacy HUD | **Needs to be added to UIManager top bar** |
| Card Draft System | **PARTIALLY MATCHED** | In-run upgrades with reroll | `CardDraftManager.cs` 3-card pick | Add explicit Reroll + speed restoration |
| Out-of-Battle Forge | **MISSING** | 3-to-1 Gear Combine system (Video 2) | Meta-upgrade system only | Battle-facing parity prioritized; Forge documented for next milestone |
