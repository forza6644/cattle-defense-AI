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

- Result: INCONCLUSIVE — local artifact exists but no build-success log entry found in available Unity Editor logs
- File: `Builds/Android/Stonehold-Development.apk`
- Size: 73,202,668 bytes (69.81 MB)
- SHA-256: `C9D9AA442A0A8B7644D5BFB97CB2E57F41A8FCFB5EF83FBA016039077D2102E7`
- Last Modified: 2026-07-19 01:28:40
- Development flag: Yes (`BuildOptions.Development`)
- Architecture: ARM64 (configured via `ReleaseCandidateBuild.cs`)
- Scripting backend: IL2CPP (configured via `ReleaseCandidateBuild.cs`)

## Release Candidate APK Build

- Result: INCONCLUSIVE — local artifact exists but no build-success log entry found in available Unity Editor logs
- File: `Builds/Android/Stonehold-ReleaseCandidate.apk`
- Size: 59,709,125 bytes (56.94 MB)
- SHA-256: `D3B648EA18FAEAC6B6AD6EB3D4C1E71D9582D8FE8414F8597FECCA6770539237`
- Last Modified: 2026-07-19 01:32:38
- Development flag: No (`BuildOptions.None`)
- Architecture: ARM64 (configured via `ReleaseCandidateBuild.cs`)
- Scripting backend: IL2CPP (configured via `ReleaseCandidateBuild.cs`)

## Automated Qualification

- EditMode: 172 passed, 0 failed, 0 skipped.
- PlayMode: 96 passed, 0 failed, 0 skipped.
- Total: 268 passed, 0 failed, 0 skipped.
- New Task 13H coverage: 42 EditMode tests (33 AndroidReleaseHardeningTests + 9 SaveManagerMigrationTests).
- Validator: SceneReferenceValidator confirmed attached by GameManager.Awake().

## Production And Expansion Regression

The unchanged production ten-wave Warlord run and the expansion ten-wave run both passed inside the complete PlayMode suite.

## Protected Local State

Ten Quaternius enemy materials, `Assets/Settings/Mobile_RPAsset.asset`, `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`, `ProjectSettings/EditorSettings.asset`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/TimeManager.asset`, `ProjectSettings/UnityConnectSettings.asset`, and `TD catle defence.slnx` are modified locally but remain unstaged and excluded from all commits.

## Decision

Save compatibility, corrupt-save recovery, reward-once safety, restart lifecycle, portrait and safe-area validation, full automated suite (268/268), and production and expansion regression all passed. Both Android APK artifacts exist on disk with valid SHA-256 hashes, but no build-success log entry was found in available Unity Editor logs to independently confirm the builds completed without error.

**QUALIFIED WITH ANDROID BUILD EVIDENCE INCONCLUSIVE**

Physical device profiling, build log confirmation, and final production assets remain non-blocking follow-up work.
