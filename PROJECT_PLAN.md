# Stonehold Approved Product Plan

Date: 2026-07-17

## Active Decisions

- Stonehold is a portrait Hero Castle Defense Roguelite.
- It is not a traditional tower-placement TD.
- The recent reference gameplay video is the official target for gameplay depth and quality.
- Stonehold must keep an independent identity. Do not copy another game's names, artwork, UI, card art, layouts, or exact content.
- Stable core gameplay has priority over cosmetic polish.
- Synty POLYGON remains the preferred paid visual direction for evaluation.
- Full paid-asset integration waits until the expanded gameplay slice is fun, stable, and technically proven.

## Current Corrective Work

Task 12B completes the baseline corrections:

- Result-state VFX can finish while gameplay is paused.
- Castle healing is separated from damage feedback.
- Idle status controllers do no per-frame work.
- Status timing handles larger frame deltas without silently skipping burn ticks.
- Particle pooling clears hierarchy state and rejects stale/double returns.
- Unity now discovers 13 focused automated tests.

## Next Milestone: Gameplay Expansion Vertical Slice

Target scope:

- 4 meaningfully differentiated active heroes selected for deep upgrade testing.
- 15-20 deliberately varied cards in the test pool.
- 6 enemy archetypes.
- 1 Elite enemy.
- 1 Boss.
- 2 traps.
- 1 battlefield defense.

Implementation order:

1. **Completed in Task 13A:** define backward-compatible contracts for expanded card categories, Legendary rarity, hero behavior upgrades, traps, defenses, castle upgrades, rerolls, enemy classification, shields, dodge, elemental resistance, and crowd-control resistance; add read-only validation and automated tests.
2. **Completed in Task 13B:** implement and qualify Enemy Pooling while preserving current behavior and balance. The current 10-wave run, registry, rewards, status reset, stale-reference guards, boss, result, and restart lifecycle passed the 58-test baseline.
3. **Completed in Task 13C:** qualify eight behavior-changing prototype upgrades for Archer, Bombardier, Frost Mage, and Electric Engineer. The prototypes remain outside `Resources/Cards`; the full result is 78/78 automated tests, including the 10-wave Warlord regression.
4. **Next - Task 13D:** build the curated 15-20 card test pool with deterministic validation tests.
5. Add the sixth normal enemy and one Elite with readable counterplay.
6. Add two traps and one battlefield defense without reintroducing tower-placement gameplay.
7. Create one balanced 10-wave expansion run and profile the densest encounter.
8. Qualify save compatibility, drafts, rewards, restart, and Android build behavior.

Task 13A establishes schemas and validation only. Traps, defenses, armor expansion, shields, dodge, resistances, Legendary cards, and behavior-changing upgrades are not playable until their later implementation tasks are completed and tested.

## Planned Card System

Categories:

- Recruit Hero.
- Hero Upgrade.
- Global Upgrade.
- Trap.
- Battlefield Defense.
- Castle Upgrade.
- Legendary Modifier.
- Reroll.

Rarity:

- Common.
- Rare.
- Epic.
- Legendary.

Hero upgrades must eventually change attack behavior, including:

- Multishot.
- Split projectiles.
- Ricochet.
- Piercing.
- Burn zones.
- Chain attacks.
- Explosion-radius changes.
- Extra casts or projectiles.

## Planned Enemy And Battlefield Systems

Enemy archetypes:

- Grunt.
- Runner.
- Shield.
- Armored Brute.
- Ranged.
- Support or Healer.
- Elite.
- Boss.

Enemy mechanics:

- Armor.
- Shield.
- Dodge.
- Elemental resistance.
- Crowd-control resistance.

Battlefield mechanics:

- Caltrops.
- Oil or burn trap.
- Wooden barricade.
- Further defenses after validation.

## Later Milestones

- Commercial content target: approximately 8 heroes and 45-60 cards.
- Multiple behavior-changing upgrades per hero.
- Dense encounters of roughly 60-100 enemies using the qualified pooling foundation, followed by real encounter and physical-device profiling. Task 13B does not qualify this density target.
- Android device profiling and release hardening.
- Paid visual-asset integration after gameplay proof.
- Additional stages, heroes, bosses, and live updates after the first publishable version.

## Rejected Or Superseded Directions

- Traditional grid/tower-placement gameplay as the main loop.
- Multiplayer in the initial release.
- Shop, ads, battle pass, daily missions, skins, or monetization before the core loop is proven.
- Buying and merging a full TD code template into the existing project.
- Cosmetic expansion before gameplay depth, performance, and stability.
- Copying the reference title's exact content or monetization surface.
