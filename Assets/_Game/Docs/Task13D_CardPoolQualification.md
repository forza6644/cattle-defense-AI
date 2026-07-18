# Task 13D Card Pool Qualification

Date: 2026-07-17

## Scope

- Source: `8f34d660a584bd24f169dbeeaaa97a1f90276a31`
- Branch: `feature/curated-card-pool`
- Pool ID: `vertical_slice_18`
- Asset: `Assets/_Game/ScriptableObjects/CardPools/VerticalSlice18.asset`
- Starting hero: Archer
- Production default: unchanged 39-card `Resources/Cards` pool

## Inventory

| Card | Category | Effective rarity | Weight | Source |
|---|---|---:|---:|---|
| recruit_bombardier | Recruit | Common | 2.00 | Reused production |
| recruit_frost_mage | Recruit | Common | 2.00 | Reused production |
| recruit_electric_engineer | Recruit | Common | 2.00 | Reused production |
| archer_twin_volley | Hero Upgrade | Common | 1.20 | Task 13C |
| archer_piercing_arrows | Hero Upgrade | Rare | 0.80 | Task 13C |
| bombardier_cluster_shells | Hero Upgrade | Rare | 0.80 | Task 13C |
| bombardier_wide_blast | Hero Upgrade | Common | 1.20 | Task 13C |
| frost_mage_shard_volley | Hero Upgrade | Rare | 0.80 | Task 13C |
| frost_mage_echoing_nova | Hero Upgrade | Epic | 0.35 | Task 13C |
| electric_engineer_extended_circuit | Hero Upgrade | Rare | 0.80 | Task 13C |
| electric_engineer_forked_current | Hero Upgrade | Epic | 0.35 | Task 13C |
| war_training | Support: global damage | Common | 1.20 | Reused production |
| battle_rhythm | Support: global fire rate | Common | 1.20 | Reused production |
| watchtower_expansion | Support: global range | Common | 1.20 | Reused production |
| fast_casting | Support: ability cooldown | Common | 1.20 | Reused production |
| empowered_abilities | Support: ability damage | Common | 1.20 | Reused production |
| frostbite | Support: Frost slow strength | Rare | 0.80 | Reused production |
| wide_blast | Support: ability radius | Rare | 0.80 | Reused production |

No new support card was required. Distribution is 3 Recruit, 8 Hero Upgrade, and 7 Modifier; effective rarity is 10 Common, 6 Rare, 2 Epic, and 0 Legendary.

## Architecture And Rules

`CardPoolDefinition` stores stable metadata, explicit ordered references, expected count, category/rarity constraints, starting hero, recruit policy, and per-pool effective rarity/weight. Referenced assets are not mutated. `CardDraftManager` accepts an optional injected override; null retains the original production Resources behavior.

Eligibility is filtered before weighting. Recruit cards require an open slot and an absent hero. Hero upgrades and targeted modifiers require the target hero. Behavior upgrades at max stacks disappear. Null, invalid-weight, and duplicate-ID entries are ignored by selection and rejected by validation. While valid recruits and slots remain, one recruit is guaranteed; after all four heroes are active, recruits disappear. Fixed pool/state/seed returns the same ordered choices, while normal play remains randomly seeded.

The selector receives a cached roster/stack snapshot, so no scene scan occurs in its choice loop. It allocates bounded temporary lists and a hash set once per draft; this is outside the dense combat loop.

## Qualification

- Validator: 0 errors; 34 documented warnings from unchanged legacy Modifier cards.
- EditMode: 63 passed, 0 failed, 0 skipped.
- PlayMode: 40 passed, 0 failed, 0 skipped.
- Previous tests: 78. New tests: 25. Grand total: 103/103.
- Simulation: 500 runs, 5,000 drafts, 15,000 choices.
- Simulation failures: invalid 0, duplicates 0, fewer-than-three 0, recruit guarantee 0, max-stack 0.
- Offer frequency: minimum 289, maximum 1,231, average 833.33. Every card was reachable and offered; all three missing heroes remained recruitable.
- Controlled PlayMode used the real `CardDraftManager`, recruited Bombardier through normal card application, unlocked its upgrade, rejected an over-cap stack without adding ActiveCards, and cleared draft behavior state on reset.
- Production regression ran without override and passed the ten-wave Warlord victory, Defeat, rewards-once, restart, pause, 1x/1.5x/2x, enemy/projectile/VFX pooling, and console safety checks.

## Limitations And Local State

This is an isolated qualification pool, not the commercial 45-60 card system. It is not assigned to the default GameScene and includes no Legendary cards or rerolls. Distribution validation proves reachability and rule correctness, not final balance.

Protected unstaged state remains outside the Task 13D commit: ten Quaternius enemy materials and existing ProjectSettings changes. `unity_launch_log.txt` remains deleted and was not recreated.

## Next Task

Add the sixth normal enemy and one Elite. Give both readable silhouettes, distinct counterplay, pooled lifecycle coverage, deterministic tests, and a ten-wave regression without changing the qualified production card default or `VerticalSlice18` asset.
