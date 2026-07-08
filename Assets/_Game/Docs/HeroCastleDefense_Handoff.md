# Stonehold TD Hero Castle Defense Handoff

## Current Project Direction

Stonehold TD has pivoted from a traditional Tower Defense prototype toward a Hero Castle Defense Roguelite.

- Static castle/base defense.
- Heroes are mounted on fixed castle/wall slots.
- Heroes auto-attack enemies.
- Status effects (Slow, Burn DoT, Shock flag) are fully implemented.
- A 3-card wave-cleared draft system applies run-wide upgrades.
- Run ends trigger a Battle Result and Damage Report screen.

## Current Working Branch And Commit

- Branch: `feature/hero-castle-defense-pivot`
- Latest commit: `ba5dd3f3246b3c6f61117e7608749f93c6b2fca4`
- Commit message: `Add meta upgrades and player profile save`
- Remote branch URL: `https://github.com/forza6644/cattle-defense-AI/tree/feature/hero-castle-defense-pivot`

## Correct Unity Project And Scene

- Unity project path: `C:\Users\forza\OneDrive\Desktop\td castle defence\TD catle defence`
- Unity version: `6000.5.2f1`
- Correct gameplay scene: `Assets/_Game/Scenes/GameScene.unity`

Important: `TD_Asset_Lab` is only an asset/map lab. It is not the current gameplay project.

---

## Tasks Completed

### Task 1: Hero slots & auto-attack
- Added `HeroDefinition` and `WeaponDefinition` configurations.
- Added `HeroSlot` and `HeroAttack` components.
- Integrated `DamageTracker`.
- Created prototype hero assets and disabled old `TowerManager` in `GameScene`.

### Task 2: Status Effects Foundation
- Created `StatusEffect` data model and `StatusEffectController` script.
- Implemented **Slow** (movespeed multiplier), **Burn** (damage-over-time), and **Shock** (flag for combos).
- Enabled project-wide `DamageTracker` support for burn ticks.
- Dynamic color trails: blue/cyan for Slow, red/orange for Burn, yellow for Shock.

### Task 3: 3-Card Draft System
- Created `CardDefinition` ScriptableObject model.
- Created `RunModifierManager` to store drafted cards and scale hero stats (damage, range, fire rate, slows, burns, shocks).
- Created `CardDraftManager` to handle weighted random picks, pausing/resuming time, and driving choices.
- Generated 10 starter cards under `Assets/_Game/Resources/Cards/`.
- Hooked drafts between waves and added a manual test trigger via the `C` key.

### Task 4: Battle Result + Damage Report
- Created `BattleResultData` runtime structure.
- Extended `DamageTracker` to compute total damage, individual sums, and percentage contributions.
- Extended UIManager to programmatically construct a **Battle Result** split overlay.
- Displays Gold, XP, and Material rewards based on wave reached, alongside a sorted damage breakdown.
- Hooked screens to Castle HP (defeat) and wave-cleared (victory) events, with debug triggers `G` (GameOver) and `V` (Victory).
- OK button returns to MainMenu scene safely, and 2X Rewards serves as an ad placeholder.

### Task 5: Meta Upgrade + Save System
- Meta Upgrade + Save System completed
- PlayerPrefs-based profile saving for coins, XP, core materials
- MetaUpgradeManager added
- MainMenu meta upgrade panel added
- Battle rewards are claimed from result screen OK button
- Duplicate reward claiming is prevented
- Meta bonuses affect Castle HP, global damage, fire rate, and range
- Card draft and result screen still work after Task 5

---

## Important Files Created Or Modified

### Data Scripts
- `Assets/_Game/Scripts/Data/AttackType.cs`
- `Assets/_Game/Scripts/Data/StatusEffectType.cs`
- `Assets/_Game/Scripts/Data/HeroDefinition.cs`
- `Assets/_Game/Scripts/Data/WeaponDefinition.cs`
- `Assets/_Game/Scripts/Data/CardDefinition.cs`
- `Assets/_Game/Scripts/Data/BattleResultData.cs`

### Gameplay Scripts
- `Assets/_Game/Scripts/Gameplay/HeroSlot.cs`
- `Assets/_Game/Scripts/Gameplay/HeroAttack.cs`
- `Assets/_Game/Scripts/Gameplay/Enemy.cs`
- `Assets/_Game/Scripts/Gameplay/Projectile.cs`
- `Assets/_Game/Scripts/Gameplay/StatusEffect.cs`
- `Assets/_Game/Scripts/Gameplay/StatusEffectController.cs`
- `Assets/_Game/Scripts/Gameplay/Castle.cs`

### Manager Scripts
- `Assets/_Game/Scripts/Managers/DamageTracker.cs`
- `Assets/_Game/Scripts/Managers/RunModifierManager.cs`
- `Assets/_Game/Scripts/Managers/CardDraftManager.cs`
- `Assets/_Game/Scripts/Managers/WaveManager.cs`
- `Assets/_Game/Scripts/Managers/MetaUpgradeManager.cs`
- `Assets/_Game/Scripts/Managers/SaveManager.cs`
- `Assets/_Game/Scripts/Core/GameManager.cs`

### UI Scripts
- `Assets/_Game/Scripts/UI/UIManager.cs`
- `Assets/_Game/Scripts/UI/MainMenuUI.cs`

---

## Current Manual Verification Result

Manual Unity verification passed.
- `GameScene` compiles and runs cleanly.
- Card drafts pause combat, allow selection, and scale statistics (C key).
- Victory/Defeat screen shows sorted damage contributions, percentages, calculated rewards, and returns to menu on OK.
- Console has zero red errors.

## Known Current Limitations
- Map is still the old gameplay prototype.
- New assets/maps from `TD_Asset_Lab` are not integrated yet.
- Only 3 active heroes exist.

---

## Recommended Next Task For Antigravity

### Task 6: Add Remaining Heroes + Expand Card Pool
Implement the remaining heroes and expand the card pool.
- Add more playable/assignable heroes with configuration, weapon definition, and specialized behaviors.
- Expand the card pool (e.g., to 30+ cards) with more varied modifiers and status effect triggers.

---

## Future Roadmap
1. **Task 6**: Add remaining heroes and 30 cards
2. **Task 7**: Integrate improved map/assets from `TD_Asset_Lab`
3. **Task 8**: VFX, audio, camera, UI polish
4. **Task 9**: Build/testing

---

## Safety Rules For Future Agents
- Do not delete old systems until the replacement is verified.
- Do not modify third-party asset files.
- Work on feature branches.
- Commit only after Unity compiles and `GameScene` is tested.
- Keep tasks small and verifiable.
- Do not merge to `main` without approval.
