# Task 13C Qualification

Task 13C introduces eight prototype hero behavior upgrades without adding them to
the production draft pool. The prototype assets remain under
`Assets/_Game/ScriptableObjects/ExpansionPrototypeCards/HeroUpgrades`; the normal
draft continues to load the 39 cards under `Resources/Cards`.

## Behavior Coverage

| Prototype card | Runtime behavior | Qualification coverage |
| --- | --- | --- |
| `archer_twin_volley` | Adds one projectile per stack and composes with piercing | PlayMode basic attack and composition tests |
| `archer_piercing_arrows` | Adds one pierced target per stack with 80% follow-up damage | Enabled `HeroAttack` PlayMode test |
| `bombardier_cluster_shells` | Spawns bounded secondary blasts without recursive splitting | PlayMode spawn and damage attribution test |
| `bombardier_wide_blast` | Adds 0.75m splash radius per stack with a 5m runtime cap | EditMode stack-cap and PlayMode behavior tests |
| `frost_mage_shard_volley` | Adds one slowing shard per stack | PlayMode multi-target slow test |
| `frost_mage_echoing_nova` | Repeats Frost Nova once after one second | PlayMode repeat and run-reset cancellation tests |
| `electric_engineer_extended_circuit` | Adds one chain target per stack | PlayMode chain coverage |
| `electric_engineer_forked_current` | Adds bounded 50% forks from the primary target | PlayMode fork coverage |

## Safety Contracts

- Hero upgrade stacks are accepted atomically. Rejected stacks do not enter the
  active-card list and do not partially mutate behavior state.
- Enemy activation IDs are non-zero and unique among active enemies, including
  domain-reload-disabled and integer-wrap scenarios.
- Delayed attacks and pooled projectiles use activation IDs to reject stale
  targets after reuse.
- Projectiles spawned from inactive templates are activated and return with
  behavior state cleared.
- Cluster damage remains attributed to `bombardier`, while an explicit secondary
  flag prevents recursive cluster spawning.
- Echoing Nova checks that its run modifier still exists after the delay, so a
  cleared or restarted run cannot execute the stale echo.

## Data Isolation

- Prototype hero upgrades: 8.
- Production `Resources/Cards` cards: 39.
- No prototype asset is loaded by the production card draft.
- The read-only validator remains available at
  `Stonehold/Validation/Validate Gameplay Expansion Data`.

## Repository Note

`unity_launch_log.txt` was removed by the original Antigravity Task 13C commit.
The corrective qualification does not recreate it. Third-party materials and
`ProjectSettings/ProjectSettings.asset` are intentionally excluded from this
task and its commit.
