# Reference Gameplay Analysis

This document contains the detailed gameplay analysis based on the two reference videos provided:
1. `ScreenRecording_07-21-2026 11-23-12_1.mp4.mp4` (Core Gameplay, Battles, and Town Screen)
2. `ScreenRecording_07-21-2026 11-30-10_1.mp4.mp4` (Weapons, Heroes, and Forge Systems)

---

## 1. Portrait Orientation & Viewport

### Reference Video Observations
* **Orientation:** Full Portrait mode (approx. 9:16 aspect ratio).
* **Viewport:** The camera is positioned at a high top-down perspective looking slightly forward (approx. 65-70 degree tilt) down onto the battlefield.
* **Layout:**
  * **Bottom:** The Castle Wall stretches horizontally across the screen. On the wall, there are three slots occupied by defenders/heroes. At the very center of the wall, there is a Castle HP indicator showing numerical HP (e.g., `3203` or `6073` HP) along with a heart icon and an HP fill bar.
  * **Middle:** The battlefield contains a path (linear/s-curve depending on the stage) leading from the top (spawner) down to the castle wall at the bottom.
  * **Top HUD:** 
    * Top-left: Game speed selector (1.0x / 1.5x / 2.0x cycle).
    * Top-center: Stage display (e.g. `Canyon / Stage 86`), wave counter (e.g. `Wave 9/20`), timer (e.g. `01:18`).
    * Top-right: Pause button.
    * Just below the stage details is the **Battle Points / Wave Progress slider** (a progress bar showing the flow of battle between waves).

---

## 2. Battlefield Layout & Path

### Reference Video Observations
* **Lanes / Paths:** The path is well-defined, winding down between rock cliffs. In Canyon (Stage 86), it is a single winding lane.
* **Spawning:** Enemies spawn from the top of the path (or multiple paths at higher levels) and advance downward toward the castle.
* **Castle Wall:** Forms the bottom boundary. It features crenellations and towers where heroes are standing.
* **Barriers/Traps:** Spikes or barricades are placed directly on the road to slow/damage enemies.

---

## 3. Castle Defense

### Reference Video Observations
* **HP System:** Large numeric health pool (e.g., thousands of hit points).
* **Defeat:** When enemies reach the wall, they hit the wall and deal damage. When HP reaches 0, the player loses.
* **Defense Time:** Stages have clear time limits or duration targets (e.g., `2m 24s`).

---

## 4. Enemy Types & Behavior

### Reference Video Observations
* **Mob/Formation Spawning:** Enemies spawn in structured clusters or lines (e.g., Grunts in a grid formation carrying shields).
* **Visual Tiers:** Enemies range from simple melee grunts to fast-moving scouts, tanky heavy soldiers, and boss-level units.
* **Shields and Armor:** Heavy shield grunts hold shields in front, absorbing frontal damage.

---

## 5. Defender/Tower & Hero System

### Reference Video Observations
* **Wall Positioning:** The wall has three main circular plinths/slots where Heroes stand.
* **Towers vs. Heroes:** In the reference, the defenders are actually unique, upgradeable **Heroes** (e.g., Lawris, ABD Monk) standing on the plinths, and they use customizable **Weapons** (e.g., Warhammer Cannon, Recurve Bow, Longbow) to fire projectiles.
* **Combat Stats:** Each Hero has:
  * Power (overall rating)
  * Speed (attacks per second)
  * Range
  * Damage (range, e.g. `23-28`)
  * Critical Rate (e.g., 5%)
  * Skill (e.g., "Arrow rain")
* **Equipment:** Heroes have 4 equipment slots: Weapon, Armor/Ring, Helmet/Amulet, Boots.

---

## 6. Economy & Progression

### Reference Video Observations
* **Gold & Materials:** Gold is used to upgrade characters and weapons. Scrolls (e.g. Weapon Scrolls) are required alongside gold for weapon upgrades.
* **Upgrade Mechanics:**
  * **Hero Level Up:** Upgrades hero stats using Gold + Hero Cards (tokens).
  * **Weapon Upgrade:** Upgrades weapon stats (e.g., `Attack +8 -> +9`) using Gold + Weapon Scrolls.
  * **Forge/Fusion:** "Combine 3 identical gear to get 1 higher tier gear." For example, combining 3 gray/Normal bows results in 1 green/Common bow with higher base stats and level cap.

---

## 7. UI Flow & Menus

### Reference Video Observations
* **Main Town Screen:**
  * **Top Bar:** Account Level/VIP status, Energy (`1517/30`), Gems (`634`), Gold (`29879`).
  * **Center Panel:** Stage Info (Act 4: Scavenger Siege, Stage 76-100, Stage 87 Active) showing progress chests for 3 stars (Clear, 50% HP Clear, 100% HP Clear).
  * **Buttons:** "Fast Raid" (cost: 7 energy) and "Battle" (cost: 7 energy).
  * **Bottom Navigation Bar:** Shop, Character, Battles (Map), Library, Map/Forge.
* **Weapons Listing Panel:** Displays unlocked/locked weapons, current weapon level (e.g. `Lv. 7 Warhammer`), and an upgrade button showing weapon scroll count.

---

## 8. Gap Analysis Summary

Stonehold already has a highly compatible vertical slice of this style:
* Camera angle and aspect ratio are already matching.
* Road layout, lane shoulder construction, and environment decoration mimic the canyon/grassland environments.
* The 3 plinths/slots on the wall match the 3 slots in the reference.
* The next step is to align Stage 1 gameplay configuration, wave progression, starting state, and UI values with the reference to achieve complete parity.
