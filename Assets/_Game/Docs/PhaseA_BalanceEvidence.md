# Phase A Balance Evidence Pack

Date: 2026-08-19  
HEAD at writing: `0bef63e` plus subsequent MSAA / draft-tap commits.  
Mode: read-only inventory. **No balance numbers were changed for this pack.**

This pack replaces theoretical audit claims with repository + telemetry facts. Do not implement Archer/crystal/reroll/Patience changes from the prior mathematical audit until a later evidence-backed task is opened.

## Sources

- Hero assets: `Assets/_Game/Resources/Heroes/`
- Cards: `Assets/_Game/Resources/Cards/` (49 assets, loaded by `CardDraftManager`)
- Crystals: `Assets/_Game/Resources/Crystals/`
- Economy: `GameConfig.asset`, `EconomyManager`, `CardDraftManager.RerollCost`
- Slots: `HeroRosterManager.GetMaxSlotsForCurrentStage()`
- Telemetry (automated, 2026-08-18): `stage1_telemetry_report.json`, `stage2_telemetry_report.txt`, `stage3_telemetry_report.txt`

## Runtime evidence (Stages 1–3)

- Stage 1 Castle Road: Victory, **50/50** castle HP, 245 spawned, 5 drafts, ~125s.
- Stage 2 Highlands: Victory, **40/50** HP, 279 spawned, 5 drafts, ~110s.
- Stage 3 Frozen Frontier: Victory, **41/50** HP, 316 spawned, 5 drafts, ~114s.

Stage 1 Wave 10 is not an observed fail point in this harness. Late-game Archer falloff is **not** shown by Stage 1–3 telemetry (no per-hero DPS table in these reports).

## Archer

- Base: 10 damage, 1.1 APS, 14m range, ability multiplier 1.35, cooldown 8s, 4 ability targets.
- Existing Archer-relevant cards: Rapid Volley (`archer_multishot`, +1 multishot target), Sharpened Arrows (+15% global damage), Piercing Focus (+15% single-target damage — **already exists**, global attack-type, not armor pen).
- **HOLD:** do not add another Piercing Focus. Do not buff Archer without Stage 4–10 per-hero damage telemetry.

## Economy / reroll

- Starting gold: **150**.
- Reroll: **flat 20g** (`CardDraftManager.RerollCost`).
- `draftRunMode` is on. `EconomyManager` **skips wave-clear gold bonus** in draft runs. In-run gold is starting gold + kill rewards.
- Stage 1 Wave 1 kill gold at authored rewards: 6×5 + 4×4 = **30g**.
- There is **no run-gold ledger** in telemetry, so late-run reroll spam is unproven.
- **HOLD:** do not scale reroll 20→25→30 yet.

## Patience / Treasury

- `Patience.asset`: **+20% Sniper fire rate**, `targetHeroId: sniper`. Not an economy card.
- `IdleTreasuryManager`: meta idle claim, not a draft card.
- **REJECT** any “rework Patience gold” task. If Sniper fire-rate stacking is a problem, that is a different investigation.

## Crystals (authored)

- Lightning: 12 dmg, 1.1 APS, 24m, 3 chains.
- Fire: 11 dmg, 0.9 APS, 24m, splash 2.2, DoT 4/3s.
- Ice: 12 dmg, 1.1 APS, 28m, slow 0.4 for 3.5s.
- Stone: 28 dmg, 0.6 APS, 24m, splash 1.8.
- Shadow: 13 dmg, 1.0 APS, 28m, DoT 5/4s.
- Lightning “39.6 DPS” assumes three full-damage chains with no falloff. Not measured.
- **HOLD:** do not change Shadow 13→16 without crystal-attribution combat logs.

## Slot identity

- Production max slots: **6** (parity stage only uses 3).
- Handoff/audit “4-hero composition” is not current code.
- Product decision later: keep 6 or enforce 4. Not a balance hotfix.

## Already shipped (do not duplicate)

- Draft synergy badges + HUD (`EvaluateSynergyTag`, `UIManager` synergy badge/HUD).
- Elemental synergy floating text (`FloatingCombatTextManager`).
- Full-surface draft tap: card body `Button` + `raycastTarget = true` (CreateImage defaults raycasts off; that was the tap bug).

## Deferred (Phase B)

Google Play Games, Unity Ads, IAP: no packages in `Packages/manifest.json`. Do not start until core economy evidence and MSAA/draft UX land.

## Recommended next balance work (not this pack)

1. Add per-hero damage to real telemetry (Stage 4–10 + Hard).
2. Log run gold before/after each draft/reroll.
3. Only then reopen A2 Archer, A4 reroll, A5 crystals.
