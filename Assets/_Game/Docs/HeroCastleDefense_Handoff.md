# Stonehold TD Hero Castle Defense Handoff

## Current Project Direction

Stonehold TD has pivoted from a traditional Tower Defense prototype toward a Hero Castle Defense Roguelite.

- Static castle/base defense.
- Heroes are mounted on fixed castle/wall slots.
- Heroes auto-attack enemies.
- Future systems include status effects, 3-card draft choices, rewards, damage report, and meta progression.

## Current Working Branch And Commit

- Branch: `feature/hero-castle-defense-pivot`
- Latest commit: `8a7576217cada029b39413b65b491c56596ebc5c`
- Commit message: `Add hero castle defense foundation`
- Remote branch URL: `https://github.com/forza6644/cattle-defense-AI/tree/feature/hero-castle-defense-pivot`

## Correct Unity Project And Scene

- Unity project path: `C:\Users\forza\OneDrive\Desktop\td castle defence\TD catle defence`
- Unity version: `6000.5.2f1`
- Correct gameplay scene: `Assets/_Game/Scenes/GameScene.unity`

Important: `TD_Asset_Lab` is only an asset/map lab. It is not the current gameplay project.

## Task 1 Completed

Task 1 added the first safe data-driven Hero Castle Defense foundation without rewriting the project.

- Added `HeroDefinition`.
- Added `WeaponDefinition`.
- Added `AttackType`.
- Added `StatusEffectType`.
- Added `HeroSlot`.
- Added `HeroAttack`.
- Added `DamageTracker`.
- Created Archer, Bombardier, and Frost Mage hero ScriptableObjects.
- Created matching weapon ScriptableObjects.
- Added 3 prototype hero slots in `GameScene`.
- Disabled/bypassed the old `TowerManager` in `GameScene`.
- Old tower/grid/placement systems were not deleted.

## Important Files Created Or Modified In Task 1

### Data Scripts

- `Assets/_Game/Scripts/Data/AttackType.cs`
- `Assets/_Game/Scripts/Data/StatusEffectType.cs`
- `Assets/_Game/Scripts/Data/HeroDefinition.cs`
- `Assets/_Game/Scripts/Data/WeaponDefinition.cs`

### Gameplay Scripts

- `Assets/_Game/Scripts/Gameplay/HeroSlot.cs`
- `Assets/_Game/Scripts/Gameplay/HeroAttack.cs`
- `Assets/_Game/Scripts/Gameplay/Enemy.cs`
- `Assets/_Game/Scripts/Gameplay/Projectile.cs`

### Manager Scripts

- `Assets/_Game/Scripts/Managers/DamageTracker.cs`

### Editor Utility

- `Assets/_Game/Editor/HeroCastleDefenseSetup.cs`

### Hero Assets

- `Assets/_Game/ScriptableObjects/Heroes/ArcherHero.asset`
- `Assets/_Game/ScriptableObjects/Heroes/BombardierHero.asset`
- `Assets/_Game/ScriptableObjects/Heroes/FrostMageHero.asset`

### Weapon Assets

- `Assets/_Game/ScriptableObjects/Weapons/ArcherWeapon.asset`
- `Assets/_Game/ScriptableObjects/Weapons/BombardierWeapon.asset`
- `Assets/_Game/ScriptableObjects/Weapons/FrostMageWeapon.asset`

### Scene

- `Assets/_Game/Scenes/GameScene.unity`

## Current Manual Verification Result

Manual Unity verification passed.

- `GameScene` opens.
- Wave UI appears.
- Enemies spawn.
- Projectiles fire.
- Gameplay progresses.
- Unity Console has no red errors.
- Branch was pushed to GitHub.

## Known Current Limitations

- Map is still the old gameplay map.
- New asset/map work from `TD_Asset_Lab` is not integrated yet.
- Only 3 prototype heroes exist.
- No card draft system yet.
- No rewards/result screen yet.
- No meta progression yet.
- `DamageTracker` has no UI yet.
- UI still has old TD wording in places.
- Old tower/grid systems are disabled/bypassed, not removed.

## Recommended Next Task For Antigravity

### Task 2: Status Effects Foundation

Implement reusable status effect foundations for heroes and weapons.

- Implement reusable Slow, Burn, and Shock status effects.
- Keep the task small and verifiable.
- Do not implement cards yet.
- Do not implement rewards yet.
- Do not implement new UI yet.
- Do not integrate the new map yet.
- Reuse current `HeroAttack` and `WeaponDefinition` data.
- Ensure Unity compiles and `GameScene` runs with no console errors.

## Future Roadmap

1. Task 2: Status Effects Foundation
2. Task 3: 3-Card Draft System
3. Task 4: Damage Report + Rewards Screen
4. Task 5: Meta Upgrade + Save System
5. Task 6: Add remaining heroes and 30 cards
6. Task 7: Integrate improved map/assets from `TD_Asset_Lab`
7. Task 8: VFX, audio, camera, UI polish
8. Task 9: Build/testing

## Safety Rules For Future Agents

- Do not delete old systems until the replacement is verified.
- Do not modify third-party asset files.
- Work on feature branches.
- Commit only after Unity compiles and `GameScene` is tested.
- Keep tasks small and verifiable.
- Do not merge to `main` without approval.
