# Stonehold Parity Gap Matrix (Corrected)

> **Document Status**: Audit & Gap Corrected  
> **Source Baseline**: Reference Video Analysis (Videos 1 & 2) vs. Stonehold Codebase

---

## System Classification Legend
- **MATCHED**: Implemented and matches reference behavior.
- **PARTIALLY MATCHED**: Core logic exists, but UI layout or tuning parameters differ.
- **DIFFERENT**: Fundamental structure differs (e.g. 20 waves vs 10 waves; 3 slots vs 6 slots).
- **MISSING**: Feature observed in reference is not currently present in Stonehold.
- **INCONCLUSIVE**: Insufficient video data to verify full internal logic.

---

## Parity Gap Inventory & Action Plan

| Category | Reference Feature | Stonehold Current | Classification | Action Plan for Parity Stage |
|---|---|---|---|---|
| **Viewport** | Portrait top-down camera (9:16) | `CameraRig.cs` adaptive portrait FOV | **MATCHED** | Retain existing camera setup |
| **Stage Structure** | 20 Total Waves per stage | Legacy Stage 1 has 10 waves | **DIFFERENT** | **Create `ReferenceParityStage01.asset` with exactly 20 waves** |
| **Defender Slots** | 3 Primary Active Defender Slots on wall | 6 `HeroSlot`s, 5 `TowerSlot`s in scene | **DIFFERENT** | **Configure 3 primary active defender positions for parity stage** |
| **HUD Layout** | Top-left Speed; Top-center Stage/Wave/Timer; Top-right Pause; Wave progress bar beneath stage info | Top-right Speed/Pause; Top-center Wave; No progress bar | **PARTIALLY MATCHED** | **Update `UIManager.cs` layout & add top-center Wave Progress Bar** |
| **Castle Health HUD** | Bottom-center HP bar over castle wall (`3203 / 6073`) | Bottom-center `CastleHpBar` | **MATCHED** | Retain and format numerical readout |
| **Defender Weapons** | 3 Distinct Weapon Types (Fast Single, Slow Splash, Control/Magic) | Archer, Cannon, Frost Mage, etc. | **MATCHED** | Map 3 active defenders to Archer (Single), Cannon (Splash), Frost Mage (Control) |
| **Enemy Formations** | Structured lines, grids, staggered groups, boss climax | Waypoint path with golden-ratio lane spread | **PARTIALLY MATCHED** | **Author deterministic 20-wave formation spawns (lines, grids, staggered, boss at wave 20)** |
| **Draft & Reroll** | 3-card draft pick with Reroll option | `CardDraftManager.cs` 3-card pick | **PARTIALLY MATCHED** | **Ensure draft pauses combat, offers Reroll, and restores previous game speed upon selection** |
| **Speed Control** | 1x / 1.5x / 2x speed toggle | `GameManager.CycleGameSpeed` | **MATCHED** | Ensure speed toggle functions and restores cleanly across drafts/pauses |
| **Out-of-Battle Forge** | 3-to-1 Gear combine interface | Permanent meta-upgrades | **MISSING** | Document for meta milestone; focus battle slice on 20-wave combat parity |
