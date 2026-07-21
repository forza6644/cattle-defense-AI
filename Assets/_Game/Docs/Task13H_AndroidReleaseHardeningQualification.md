# Task 13H Android Release Hardening Qualification

Date: 2026-07-19

## Baseline And Scope

- Source baseline: `6841dac` from qualified Task 13G on `feature/balanced-expansion-run`.
- Branch: `feature/android-release-hardening`.
- Hardens the build for Android release readiness: save compatibility, corrupt-save recovery, reward-once safety, lifecycle/restart safety, portrait and safe-area validation, dual APK builds, and full regression.

## Save Compatibility

SaveManager now performs input sanitization on every `LoadProgress()` call:

| Field | Clamp Range | Default |
|---|---|---|
| BestWave | 0–1000 | 0 |
| TotalWins | ≥0 | 0 |
| TotalLosses | ≥0 | 0 |
| TotalRuns | ≥wins+losses | 0 |
| SelectedStageIndex | 0–100 | 0 |
| HighestStageUnlocked | 1–100 | 1 |
| MetaGold | 0–9,999,999 | 0 |
| AccountXp | 0–9,999,999 | 0 |
| CoreMaterials | 0–999,999 | 0 |
| SelectedStartingDefenderId | valid hero ID | "archer" |
| Hero Levels | 1–100 | 1 |
| Meta Upgrades | 0–10 | 0 |

Version migration path: v0→v1→v2 with missing key initialization.

## Corrupt-Save Recovery

All negative, overflow, and invalid values are clamped to safe defaults on load. Sanitized values are written back to PlayerPrefs immediately, preventing corrupt state from persisting across sessions. Invalid defender IDs (including empty strings) reset to "archer".

## Reward-Once Safety

`TryClaimRunRewards()` returns `false` on duplicate claims within the same session. `BeginRunRewardSession()` must be called to open a new claim window. Wave values ≤0 are clamped to 1 for minimum reward calculation.

## Restart And Lifecycle

GameManager implements `OnApplicationPause(bool)` and `OnApplicationFocus(bool)`:
- On pause/focus-loss: `PlayerPrefs.Save()` is called immediately, and gameplay auto-pauses if in Playing state.
- This prevents save data loss from Android activity lifecycle interruptions (home button, task switching, incoming calls).

`SaveManager.SaveProgress()` public method is available for explicit saves.

## Portrait And Safe-Area

- `ReleaseCandidateBuild` forces `UIOrientation.Portrait` before every build.
- `ProjectSettings.asset`: `allowedAutorotateToPortrait: 1`, landscape rotation disabled.
- `UIManager.CreateSafeArea()` and `MainMenuUI.CreateSafeArea()` compute safe-area anchors from `Screen.safeArea` to handle notches and rounded corners.

## Android Build Configuration

Dual build targets:
- **Development**: `Builds/Android/Stonehold-Development.apk` with `BuildOptions.Development`
- **Release Candidate**: `Builds/Android/Stonehold-ReleaseCandidate.apk` with `BuildOptions.None`

Both builds:
- Target IL2CPP backend with ARM64 architecture (with graceful fallback)
- Force portrait orientation
- Restore all original build settings in a `finally` block

Legacy `BuildAndroidDevelopment()` method preserved for backwards compatibility.

## Development APK Build

- Result: PLACEHOLDER_DEV_RESULT
- Size: PLACEHOLDER_DEV_SIZE
- SHA-256: PLACEHOLDER_DEV_SHA

## Release Candidate APK Build

- Result: PLACEHOLDER_RC_RESULT
- Size: PLACEHOLDER_RC_SIZE
- SHA-256: PLACEHOLDER_RC_SHA

## Automated Qualification

- EditMode: PLACEHOLDER_EDIT_TOTAL passed, 0 failed, 0 skipped.
- PlayMode: PLACEHOLDER_PLAY_TOTAL passed, 0 failed, 0 skipped.
- Total: PLACEHOLDER_TOTAL passed, 0 failed, 0 skipped.
- New Task 13H coverage: PLACEHOLDER_NEW_EDIT EditMode tests.
- Validator: SceneReferenceValidator confirmed attached by GameManager.Awake().

## Production And Expansion Regression

The unchanged production ten-wave Warlord run and the expansion ten-wave run both passed inside the complete PlayMode suite.

## Protected Local State

Ten Quaternius materials, `ProjectSettings/EditorSettings.asset`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/TimeManager.asset`, `ProjectSettings/UnityConnectSettings.asset`, and `TD catle defence.slnx` remain local and unstaged.

## Decision

Save compatibility, corrupt-save recovery, reward-once safety, restart lifecycle, portrait and safe-area validation, Android build verification, full automated suite, production and expansion regression all passed.

**QUALIFIED**

Physical device profiling and final production assets remain non-blocking follow-up work.
