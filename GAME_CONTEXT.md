# Stonehold — Game Context & Technical Specification

> **Analysis Date**: 2026-08-15  
> **Repository**: `forza6644/cattle-defense-AI`  
> **Project Directory**: `Stonehold-V2`  
> **Target Platform**: Android (Mobile Portrait) & Standalone Windows 64-bit  

---

## 1. Game Overview & Status

- **Game Name**: Stonehold (Internal repo: `cattle-defense-AI`, legacy project identifier: `TD catle defence`)
- **Developer / Company**: forza6644
- **Genre**: Portrait Hero Castle Defense Roguelite
- **Core Loop**:
  Enemies march down a central approach towards the fortress castle gate. The player defends the gate using fixed castle wall heroes (up to 6 slots) and a dedicated gate Starter Crystal. Kills award Gold and XP. Level-ups trigger a 3-card Roguelite drafting modal offering hero recruits, behavior-altering upgrades, status modifiers, traps, and battlefield defenses. Players can cast active Castle Ultimate abilities, trigger cross-elemental synergies (e.g., Thermal Shock, Overload, Corrosive Blast, Sub-Zero Shatter), collect passive Relics, and adjust pre-run Ascension Heat mutators.
- **Target Platforms**:
  - **Android** (Primary Mobile Target): Forced Portrait orientation (`UIOrientation.Portrait`), dynamic `Screen.safeArea` notch/insets handling, ARM64 architecture, IL2CPP scripting backend, Adaptive Performance package integration.
  - **Standalone Windows 64-bit** (PC / Editor Target): Full keyboard hotkey support (Esc for pause, 1/2/3 for speeds, C for draft, 1-3 for Castle Ultimates) and standalone executable builds.
  - **iOS**: Configured in project settings with thermal throttling and portrait support; primary active mobile release pipelines target Android APKs.
- **Current Development Status**:
  - **Active Milestone**: Advanced Vertical Slice / Release Hardening Phase (Tasks 13H & 14B Qualified).
  - **Content Status**: 8 fully implemented heroes, 5 elemental starter crystals, 6 authored stages / biomes, 50+ drafted cards, 8 ascension heat mutators, 8 legendary relics, 18+ achievements, 6 codex bestiary entries, infinite endless abyssal survival mode, 3-star campaign progression, 3 castle ultimate abilities, deep combat telemetry, and dual Android APK release automation (Development & Release Candidate).
  - **Stability & Verification**: 366 passing automated tests (196 EditMode + 170 PlayMode), zero compile warnings, robust object pooling for zero runtime allocations during dense waves, and corrupt-save recovery.

---

## 2. Game Engine & Version

- **Game Engine**: Unity 6 (Unity 6000)
- **Exact Editor Version**: `6000.5.2f1` (Revision: `eb73d3b415a1`, verified in `ProjectSettings/ProjectVersion.txt`)
- **Render Pipeline**: Universal Render Pipeline (URP) version `17.5.0`
  - Asset Configs: `Assets/Settings/Mobile_RPAsset.asset`, `Assets/Settings/PC_RPAsset.asset`, `Assets/Settings/Mobile_Renderer.asset`, `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`
- **Color Space**: Linear
- **Target Frame Rate**: 60 FPS (`Application.targetFrameRate = 60`)

---

## 3. Programming Languages, Frameworks, Packages & SDKs

### Languages & Assemblies

- **C#**: .NET Standard 2.1 / C# 9.0+ via Unity Roslyn compiler.
- **Assembly Definition Files (`asmdef`)**:
  - `Stonehold.Runtime.asmdef` (`Assets/_Game/Scripts/`): Main runtime assembly for game logic, managers, UI, and combat.
  - `Stonehold.EditModeTests.asmdef` (`Assets/_Game/Tests/EditMode/`): Unit tests, contract validators, and data integrity checks.
  - `Stonehold.PlayModeTests.asmdef` (`Assets/_Game/Tests/PlayMode/`): Integration tests, full wave simulations, pooling lifecycle tests.

### Packages & Dependencies (`Packages/manifest.json`)

- `com.unity.render-pipelines.universal` (`17.5.0`): Universal Render Pipeline.
- `com.unity.inputsystem` (`1.19.0`): New Unity Input System.
- `com.unity.ugui` (`2.5.0`): Unity UI (Canvas, EventSystem, Layouts).
- `com.unity.test-framework` (`1.7.0`): Unity Test Framework (NUnit integration).
- `com.unity.ai.navigation` (`2.0.13`): AI Navigation & NavMesh runtime.
- `com.unity.ai.inference` (`2.6.1`): Sentis AI inference engine.
- `com.unity.adaptiveperformance` (`1.0.0`): Mobile thermal & performance scaling.
- `com.unity.timeline` (`1.8.12`): Cutscene & sequencing framework.
- `com.unity.visualscripting` (`1.9.11`): Visual scripting tools.
- `com.coplaydev.unity-mcp` (GitHub git dependency): Unity Model Context Protocol integration.
- Standard IDE plugins: `com.unity.ide.rider` (`3.0.40`), `com.unity.ide.visualstudio` (`2.0.27`), `com.unity.collab-proxy` (`2.12.4`).

---

## 4. Project Folder Structure & Important Files

```text
Stonehold-V2/
├── Assets/
│   ├── _Game/
│   │   ├── Docs/                          # Architecture, qualification reports, visual style bible
│   │   ├── Editor/                        # Build scripts (ReleaseCandidateBuild.cs), validation tools
│   │   ├── Prefabs/                       # Enemies, Heroes, Projectiles, Environment, Battlefield anchors
│   │   ├── Resources/                     # Runtime Resources.Load assets
│   │   │   ├── Cards/                     # 39 production CardDefinitions (AddHero, modifiers, upgrades)
│   │   │   ├── Crystals/                  # 5 StarterCrystalDefinitions (Fire, Ice, Lightning, Shadow, Stone)
│   │   │   ├── Mutators/                  # 8 AscensionMutatorDefinitions
│   │   │   ├── Relics/                    # 8 RelicDefinitions
│   │   │   ├── StageNature/               # Environment foliage & prop prefabs
│   │   │   └── AudioManager.prefab        # Central audio manager prefab
│   │   ├── Scenes/
│   │   │   ├── MainMenu.unity             # Primary frontend entry scene (Build index 0)
│   │   │   ├── GameScene.unity            # Legacy test scene (Build index 1)
│   │   │   ├── V2/
│   │   │   │   ├── GameplayIntegration_V2.unity  # Primary active gameplay scene (Build index 2)
│   │   │   │   └── GameplayComposition_V2.unity  # Composition layout scene
│   │   │   ├── ArtDirection/              # Lighting & lookdev test scenes
│   │   │   └── Lookdev/                   # Approved visual lookdev scene
│   │   ├── ScriptableObjects/             # Authored data assets (Enemies, Heroes, Stages 1-6, CardPools)
│   │   ├── Scripts/
│   │   │   ├── Core/                      # GameManager.cs, GameState.cs, CameraRig.cs, ExpansionRunContext.cs
│   │   │   ├── Data/                      # CardDefinition.cs, EnemyData.cs, HeroDefinition.cs, StageData.cs, etc.
│   │   │   ├── Diagnostics/               # ExpansionRunBalanceSimulator.cs
│   │   │   ├── Gameplay/                  # Castle.cs, HeroAttack.cs, StarterCrystal.cs, Enemy.cs, Projectile.cs, etc.
│   │   │   ├── Managers/                  # SaveManager.cs, HeroRosterManager.cs, CardDraftManager.cs, WaveManager.cs, etc.
│   │   │   ├── Performance/               # PerformanceOptimizer.cs
│   │   │   ├── Telemetry/                 # RealCombatTelemetryLogger.cs
│   │   │   └── UI/                        # UIManager.cs, MainMenuUI.cs, FloatingCombatTextManager.cs, SceneFader.cs
│   │   └── Tests/
│   │       ├── EditMode/                  # 10 EditMode test suites (196 tests)
│   │       └── PlayMode/                  # 38 PlayMode test suites (170 tests)
│   └── Settings/                          # URP assets, renderers, volume profiles
├── Packages/
│   └── manifest.json                      # Unity package dependencies
├── ProjectSettings/                       # Engine, player, quality, physics, and input settings
├── Builds/                                # Android APKs & Win64 executable output directories
├── PROJECT_PLAN.md                        # High-level milestone and design roadmap
├── PROJECT_STATUS.md                      # Detailed technical verification baseline
└── README.md                              # Repository overview and setup instructions
```

---

## 5. Main Gameplay Systems & Interactions

### 1. Hero Roster & Placement System (`HeroRosterManager`, `HeroSlot`, `HeroAttack`)

- The castle wall features fixed defense slots. Heroes are recruited in a **center-outward order** (`ApplyCenterOutOrder`: slots `-0.9`, `+0.9`, `-2.7`, `+2.7`, `-4.5`, `+4.5`) so key defenders occupy central sightlines.
- **8 Distinct Heroes**:
  - **Archer** (Physical / Single Target): High attack speed, Piercing Arrows, Twin Volley multishot.
  - **Bombardier** (Physical / Splash): Heavy artillery shells, Cluster Shells, Wide Blast AOE.
  - **Frost Mage** (Frost / Slow & Freeze): Ice bolts, Slow debuffs, Freeze stun, Shatter vulnerability triggers.
  - **Fire Mage** (Fire / Burn DoT): Fireball impacts, Burn DoT stacks, Wildfire ground zones.
  - **Electric Engineer** (Lightning / Chain & Shock): Chain lightning arcs, Shock debuff, Extended Circuit conductivity.
  - **Sniper** (Physical / Heavy Single Target): High-caliber armor-piercing rounds, high critical multiplier, deadeye focus.
  - **Plague Doctor** (Poison / Caustic DoT): Toxic poison vials, armor-shredding corrosion.
  - **Radiant Paladin** (Holy / Defensive Support): Holy smite, castle healing aura, defensive shielding.

### 2. Fortress Starter Crystal System (`StarterCrystal`, `StarterCrystalDefinition`)

- Mounted prominently atop the fortress central gate, acting as an independent, non-hero automated defense turret.
- **5 Selectable Elements**:
  - **Lightning Crystal**: Rapid chain lightning with Shock debuff.
  - **Fire Crystal**: High explosive splash damage with Burn DoT.
  - **Ice Crystal**: High slow magnitude with frost impact pulses.
  - **Stone Crystal**: Heavy physical armor-penetrating siege shockwaves.
  - **Shadow Crystal**: Piercing dark energy with decay DoT.

### 3. Elemental Synergy & Reaction Engine (`StatusEffectController`, `CombatTelemetryManager`)

- Applying multiple status effects to an enemy triggers amplified reactive explosions:
  - **Thermal Shock** (Burn + Frost/Slow): Instant massive % Max HP burst.
  - **Overload** (Burn + Shock): High-voltage electric shockwave damaging surrounding enemies.
  - **Corrosive Blast** (Burn + Poison): Armor-melting acidic detonation.
  - **Sub-Zero Shatter** (Frost/Freeze + Heavy Physical): Shatters frozen armor for bonus damage.

### 4. Enemy Spawning & Object Pooling (`WaveManager`, `EnemyPoolManager`, `Enemy`)

- `EnemyPoolManager` prewarms and recycles enemies keyed by stable IDs (`grunt`, `runner`, `brute`, `armored`, `warlord_boss`, `crossbow_raider`, `elite_war_shaman`, `void_stalker`, `void_nullifier`, `void_lord`).
- Unique `ActivationId` tokens prevent stale projectiles, lingering status effects, or delayed death callbacks from affecting recycled instances.
- Special enemy mechanics: Elite affixes, War Shaman AOE healing, Crossbow Raider ranged standoffs, and multi-phase Boss encounters.

### 5. Battlefield Traps & Defenses (`BattlefieldAnchorManager`, `TrapRuntimeManager`, `BattlefieldDefenseManager`)

- Fixed ground anchors outside the gate automatically deploy drafted battlefield assets:
  - **Caltrops**: Ground hazard slowing and damaging passing infantry.
  - **Burning Oil**: Ignited zone dealing heavy continuous fire damage.
  - **Wooden Barricade**: High-health destructible roadblock stalling enemy advancement.

### 6. Active Castle Ultimates (`CastleAbilityManager`, `CastleAbilityDefinition`)

- Player-triggered active abilities with dedicated Energy pool (100 Max Energy, passive recharge):
  - **Arcane Mortar Strike**: Targeted heavy artillery shell dealing 350 AOE damage (30 Energy).
  - **Fortress Kinetic Aegis**: Overcharges shield generators for +300 Kinetic Shield (40 Energy).
  - **Call the Militia**: Deploys defensive vanguard slowing enemy advancement (35 Energy).

### 7. Roguelite Card Drafting & Progression (`CardDraftManager`, `CardDraftSelector`, `RunProgressionManager`, `RunModifierManager`)

- Enemy kills award XP -> Level-up halts gameplay (`GameState.LevelUp`) -> 3-card draft presented.
- Reroll feature available for 20 Gold (`CardDraftManager.RerollCost`).
- Supports 50+ cards categorized into Recruit Hero, Hero Upgrade, Global Upgrade, Trap, Battlefield Defense, Castle Upgrade, and Legendary Modifier.

### 8. Relics & Artifacts (`RelicManager`, `RelicDefinition`)

- 8 run-defining passive artifacts drafted or dropped by Elites: *Aegis Battery*, *Chrono Hourglass*, *Frostbite Talisman*, *Midas Coin*, *Overload Prism*, *Spectral Lantern*, *Vampiric Crest*, *Volatile Catalyst*.

### 9. Ascension Heat Mutators (`AscensionManager`)

- Pre-run difficulty toggles (Heat 1 to 5+) adding mutators (e.g. *Fast Enemies*, *Armored Horde*, *Brittle Castle*, *Empowered Elites*, *Nullification Rifts*) in exchange for score and reward multipliers.

### 10. Endless Abyssal Survival Mode (`EndlessSurvivalManager`)

- Post-Victory infinite survival mode with procedural scaling, boss encounters every 5 waves, and Abyssal Overcharge blessing drafts (*Void Surge*, *Eternal Resonance*, *Singularity Barrier*, *Midas Singularity*, *Abyssal Aegis*).

---

## 6. Scenes, Levels, Game States & Flow

### Scenes

- **`MainMenu`** (`Assets/_Game/Scenes/MainMenu.unity`): Title, Stage Map, Hero Roster, Meta Upgrades, Ascension Drawers, Bestiary Codex, Achievements, Settings.
- **`GameplayIntegration_V2`** (`Assets/_Game/Scenes/V2/GameplayIntegration_V2.unity`): Primary active combat scene with full URP lighting, highway path, castle gate, and pooled managers.
- **`GameScene`** (`Assets/_Game/Scenes/GameScene.unity`): Legacy baseline scene maintained for regression.
- **`Stonehold_ApprovedVisualLookdev`** (`Assets/_Game/Scenes/Lookdev/`): Visual quality reference and lighting lab.

### Game States (`GameState` Enum)

- `MainMenu`: Out-of-run frontend state.
- `Playing`: Active wave simulation at player-selected speed (1x, 1.5x, 2x). Effective clock is paced at 1.32x base and 3.168x at top speed.
- `Paused`: Time scale frozen (`Time.timeScale = 0`), pause modal active.
- `LevelUp`: Time scale frozen (`Time.timeScale = 0`), 3-card draft modal active.
- `Victory`: All waves cleared (`Time.timeScale = 0`), Victory summary displayed, unscaled VFX, Endless Mode entry prompt.
- `Defeat`: Castle HP reduced to 0 (`Time.timeScale = 0`), Defeat summary displayed, unscaled VFX.

### Authored Stages (6 Biomes)

- **Stage 1 — Castle Road** (Grassy Plains, Goblin Grunts/Runners, Warlord Boss)
- **Stage 2 — Highlands Fortress** (Rocky Foothills, Armored Orcs, Siege Rams)
- **Stage 3 — Frozen Pass** (Glacial Peaks, Frostbite fiends)
- **Stage 4 — Titan Citadel / Volcanic Caldera** (Infernal Magma, Molten elementals)
- **Stage 5 — Volcanic Pinnacle / Toxic Mire** (Caustic abominations)
- **Stage 6 — Abyssal Void Rift** (Singularity, Void Stalkers, Void Nullifiers, Void Lord Boss)

---

## 7. Player Data, Progression & Save System

### Persistence Architecture (`SaveManager`)

- **Storage**: Unity `PlayerPrefs` with explicit `PlayerPrefs.Save()` calls.
- **Save Version**: Version 2 (`CurrentSaveVersion = 2`).
- **Data Integrity & Corruption Recovery**:
  - Automatically sanitizes and clamps all values on load (e.g., `BestWave` 0–1000, `MetaGold` 0–9,999,999, `AccountXp` 0–9,999,999, `CoreMaterials` 0–999,999, hero levels 1–100, meta upgrades 0–10).
  - Sanitized values are immediately written back to disk, neutralizing corrupt/overflowed data.
  - Invalid or empty starting defender IDs fallback safely to `"archer"`.
- **Lifecycle Protection**:
  - `GameManager` implements `OnApplicationPause(true)` and `OnApplicationFocus(false)` to force `PlayerPrefs.Save()` and pause active combat on Android home button, lock screen, or incoming calls.

### Currencies & Economies

| Currency | Scope | Source | Usage |
| :--- | :--- | :--- | :--- |
| **In-Run Gold** | Current Run | Enemy kills, wave clear bonus | Card draft rerolls (20 Gold) |
| **Meta Gold / Coins** | Persistent | Run completion, achievements | Permanent Meta Upgrades in Shop |
| **Account XP** | Persistent | Enemy kills, stage clears | Account level & unlock progression |
| **Core Materials** | Persistent | Boss kills, achievements | Advanced meta crafting / ascension |
| **Abyssal Trophies** | Persistent | Endless Survival waves | High-tier endless rewards |

### Permanent Progression

- **6 Meta Upgrades** (`MetaUpgradeManager`):
  - *Castle Fortification* (`castle_hp`): +10 HP per level (Max 10).
  - *Crystal Attack* (`damage`): +15% Global Damage per level (Max 10).
  - *MetaGold Bonus* (`gold_bonus`): +10% Gold per level (Max 10).
  - *Castle Regeneration* (`castle_regen`): +1 HP per 5s per level (Max 10).
  - *Faster Defenders* (`fire_rate`): +3% Fire Rate per level (Max 10).
  - *Longer Watch* (`range`): +3% Range per level (Max 10).
- **Defender Meta Levels**: Levels 1–100 per hero tracked under `meta_level_<heroId>`.
- **Campaign Star Map**: 3 stars per stage (Victory, 70%+ Castle HP, Heat 2+ cleared).
- **Codex & Achievements**: 18+ milestone quests with claimable rewards.

---

## 8. Authentication, Backend Services & APIs

- **Authentication**: None (100% offline, local client-side).
- **Backend Services & Databases**: None (No remote servers, Firebase, PlayFab, AWS, or external database).
- **HTTP / REST / WebSocket APIs**: None.
  > [!NOTE]
  > Because Stonehold is fully offline with zero remote HTTP communications, no `openapi.yaml` or `GAME_API_SETUP.md` files are required.

---

## 9. Multiplayer, Leaderboards, Analytics, Ads & Purchases

- **Multiplayer**: None (Single-player offline experience).
- **Leaderboards**: Local offline personal bests (Highest Wave, Best Score, Stars Earned) stored in `PlayerPrefs`.
- **Achievements**: Fully custom in-game achievement system (`AchievementManager.cs`) with 18+ milestone quests and animated toast notifications.
- **Analytics**: Local in-engine combat telemetry logger (`CombatTelemetryManager.cs`, `RealCombatTelemetryLogger.cs`). No third-party tracking SDKs are attached.
- **Advertisements**: None. Zero ad SDKs (AdMob, Unity Ads, AppLovin) integrated.
- **In-App Purchases (IAP)**: None. Zero real-money microtransactions; 100% gameplay-driven economy.

---

## 10. Environment Variables & Configuration

- **Environment Variables**: No external `.env` or cloud secret variables required.
- **Engine Configurations**:
  - `ProjectSettings/ProjectSettings.asset` (Company: `forza6644`, Product: `Stonehold`, Package ID, Orientation).
  - `ProjectSettings/QualitySettings.asset` (Quality tiers, shadows, texture resolutions).
  - `Assets/Settings/Mobile_RPAsset.asset` (URP mobile render asset).
  - `Assets/_Game/ScriptableObjects/GameConfig.asset` (Base castle HP, starting gold, wave configs).
- **Credential Protection**: No passwords, private keys, keystores, or auth tokens exist in the repository.

---

## 11. Build & Run Instructions

### 1. Opening in Unity

- Launch **Unity Hub**.
- Open project located at `C:\Users\forza\OneDrive\Desktop\Stonehold-V2` using **Unity 6000.5.2f1**.
- Open `Assets/_Game/Scenes/MainMenu.unity` or `Assets/_Game/Scenes/V2/GameplayIntegration_V2.unity` and click **Play**.

### 2. Running Automated Tests

- **Via Unity Editor UI**:
  - Open `Window` -> `General` -> `Test Runner`.
  - Select **EditMode** tab -> Click **Run All** (196 tests).
  - Select **PlayMode** tab -> Click **Run All** (170 tests).
- **Via Command Line (PowerShell)**:

  ```powershell
  # EditMode Tests
  & "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\forza\OneDrive\Desktop\Stonehold-V2" -runTests -testPlatform editmode -testResults "test_results_editmode.xml" -logFile "editmode.log"

  # PlayMode Tests
  & "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\forza\OneDrive\Desktop\Stonehold-V2" -runTests -testPlatform playmode -testResults "test_results_playmode.xml" -logFile "playmode.log"
  ```

### 3. Building Android APKs (`ReleaseCandidateBuild.cs`)

- **Via Unity Editor Menu**:
  - `Stonehold` -> `Android` -> `Build Development APK` (Generates `Builds/Android/Stonehold-Development.apk`).
  - `Stonehold` -> `Android` -> `Build Release Candidate APK` (Generates `Builds/Android/Stonehold-ReleaseCandidate.apk`).
- **Via Command Line**:

  ```powershell
  & "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\Users\forza\OneDrive\Desktop\Stonehold-V2" -executeMethod Stonehold.Editor.ReleaseCandidateBuild.BuildReleaseCandidate -logFile "android_build.log"
  ```

### 4. Building Windows 64-bit Standalone Executable

- **Via Unity Editor Menu**:
  - `Stonehold` -> `Win64` -> `Build Standalone Win64` (Generates `Builds/Win64/StoneholdV2.exe`).

---

## 12. Existing Bugs, Incomplete Features, TODOs & Technical Risks

| Area | Item | Classification | Description & Impact |
| :--- | :--- | :--- | :--- |
| **Art & Presentation** | 3D Visual Assets | Incomplete / Prototype | Characters and barricades use temporary Quaternius/primitive assets. Final commercial Synty POLYGON visual asset pipeline is planned for Task 14B. |
| **Animation** | Character Rigging | Incomplete / Prototype | Defenders currently utilize procedural code oscillations (`ProceduralAnimator.cs`) rather than skinned humanoid animation state machines. |
| **Hardware Profiling** | Android Device Verification | Technical Risk | Dense wave scenarios (70+ enemies on Wave 9) are qualified at 60 FPS in Editor playmode and batch runners, but real physical mobile thermal throttling and GPU fill-rates remain *Needs verification* on physical hardware. |
| **Audio** | Headless Audio Listener | Minor Test Artifact | Headless test execution logs minor warnings regarding missing AudioListener when initializing test scenes; does not impact gameplay runtime. |
| **Memory** | Ability List Allocations | Technical Debt | A few hero targeting paths allocate small temporary candidate lists; should be migrated to preallocated or non-allocating arrays. |

---

## 13. Automated Tests Currently Available

The test suite comprises **366 total automated tests** across EditMode and PlayMode:

### EditMode Test Suites (10 Suites, 196 Tests)

- `AndroidReleaseHardeningTests` (33 tests): Safe area calculation, orientation enforcement, dual build paths, save corruption clamps, lifecycle pause hooks.
- `ExpansionRunEditModeTests` (42 tests): Wave structures, card pool definitions, draft probabilities, balance math.
- `CardPoolEditModeTests` (23 tests): Weighted drafting, hero recruit guarantees, max stack boundaries.
- `GameplayExpansionDataTests` (21 tests): Category schemas, rarity values, behavior upgrade contracts.
- `BattlefieldDefenseEditModeTests` (13 tests): Trap and defense data contracts and limits.
- `EnemyRosterExpansionEditModeTests` (12 tests): Enemy classification (Normal, Elite, Boss) and stat integrity.
- `BaselineEditModeTests` (9 tests): Core damage formulas, modifier math.
- `GameplayParityTests` (9 tests): Combat and progression parity checks.
- `SaveManagerMigrationTests` (9 tests): Save format v0 -> v1 -> v2 migration paths and corrupt data recovery.
- `StarterCrystalTests` (1 test): Starter crystal definitions and elements.

### PlayMode Test Suites (38 Suites, 170 Tests)

- `BaselinePlayModeTests`: Full 10-wave playthrough, Warlord boss victory, unscaled result VFX, speed toggles (1x, 1.5x, 2x), restart cleanup.
- `EnemyPoolingPlayModeTests`: 100-cycle spawn/despawn stress tests, zero instance leaks, token wrap protection.
- `HeroBehaviorPlayModeTests`: Multishot, Piercing, Cluster Shells, Chain Lightning, Burn/Slow behavior upgrades.
- `ElementalSynergyPlayModeTests`: Thermal Shock, Overload, Corrosive Blast, and Shatter reaction triggers.
- `EliteEnemyAffixesPlayModeTests`: War Shaman healing pulses, Crossbow Raider standoff attacks, Elite affixes.
- `EndlessSurvivalPlayModeTests`: Procedural scaling waves, Abyssal Overcharge drafts, trophy awards.
- `RelicSystemPlayModeTests`: Passive relic acquisition and combat stat hook queries.
- `CastleAbilitiesPlayModeTests`: Arcane Mortar, Kinetic Shield, and Militia summons.
- `AscensionHeatPlayModeTests`: Heat point calculation and enemy stat modifiers.
- `AchievementSystemPlayModeTests`: Progress tracking, unlocking, and reward claims.
- `BestiaryPlayModeTests`: Discovery recording, kill counter increments.
- `CampaignMapPlayModeTests`: 3-star evaluations, node unlock progression.
- `CombatTelemetryPlayModeTests`: DPS calculation, MVP hero reporting, damage attribution.
- `Stage1-6 RealTelemetryPlayModeTests`: Real combat telemetry logging across all 6 biomes.
- `VfxPerformancePlayModeTests` & `AudioIntegrationPlayModeTests`: Particle pooling reset and audio subscription safety.

---

## 14. Recommended Next Development Tasks (Prioritized)

### Priority 0: Physical Mobile Hardware Verification

- **Physical Android Device Profiling**: Deploy `Builds/Android/Stonehold-ReleaseCandidate.apk` to low-end, mid-range, and high-end Android hardware. Profile GPU frame times, battery consumption, and thermal behavior with Unity Adaptive Performance.
- **OS Kill / Lifecycle Edge Case Verification**: Verify save recovery and resume state across Android OS kills, incoming phone calls, and background switching.

### Priority 1: Visual Asset Production & Character Animation

- **Commercial 3D Asset Integration (Task 14B)**: Replace prototype Quaternius environment and character meshes with approved Synty POLYGON fantasy assets.
- **Humanoid Animation State Machines**: Replace procedural transform oscillations with skinned humanoid animation clips (Idle, Aim/Draw, Cast, Throw, Reload).

### Priority 2: Audio & Polish

- **Dynamic Audio Polish**: Implement hero voice lines, attack grunts, and dedicated ambient music tracks per biome.
- **Mobile URP Post-Processing Pass**: Fine-tune Bloom, Color Grading, and Vignette profiles for optimal mobile performance and visual impact.

### Priority 3: Optional Commercial Features (Post-Proof)

- **Cloud Save & Authentication**: Optional Google Play Games / Apple Game Center cloud synchronization.
- **Live Ops & Monetization**: Optional rewarded video ad placements for rerolls / revives.
