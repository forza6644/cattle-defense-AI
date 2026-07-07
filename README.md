# Stonehold

**Defend your castle gate from waves of raiders by placing and upgrading towers along a single winding path. Every kill drops gold — spend it fast, because each wave hits harder than the last.**

## Controls
- **Select / Place:** Click or tap on an empty build slot to open the tower menu, then click a tower to build it.
- **Upgrade:** Click or tap on an existing tower to see upgrade options.

## Current Milestone
**Milestone 2 - Prototype Implementation (In Progress)**

## Roadmap
- [x] Milestone 1: Game Design Document & Architecture
- [ ] Milestone 2: Playable Prototype (3 Waves, 3 Towers, 2 Enemies)
- [ ] Milestone 3: Expansion to Endless / Full Loop
- [ ] Milestone 4: Meta-progression and Idle Generation
- [ ] Milestone 5: Polish & Polish

## Project Setup

This is a Unity project. Open this folder from Unity Hub:

```text
C:\Users\forza\OneDrive\Desktop\td castle defence\TD catle defence
```

Use Unity `6000.5.2f1` when possible. The project should include these source-controlled folders:

- `Assets`
- `Packages`
- `ProjectSettings`

Do not commit Unity-generated folders such as `Library`, `Temp`, `Logs`, `Obj`, `Build`, `Builds`, or `UserSettings`.

## Git Workflow

Pull the latest changes before starting work:

```bash
git pull
```

Create a branch for your work:

```bash
git switch -c your-branch-name
```

Check what changed:

```bash
git status
```

Commit your changes:

```bash
git add README.md AI_WORKFLOW.md
git commit -m "Add Unity AI workflow documentation"
```

Push your branch:

```bash
git push -u origin your-branch-name
```

## Git LFS

Large Unity assets should use Git LFS. This project already has `.gitattributes` rules for common binary assets, including:

- `*.psd`
- `*.fbx`
- `*.blend`
- `*.wav`
- `*.mp3`
- `*.mp4`
- `*.mov`
- `*.png`
- `*.jpg`

Before adding large assets, make sure Git LFS is installed:

```bash
git lfs install
git lfs track
```

## Working With AI Tools

Use Codex for coding, refactoring, documentation changes, branch work, commits, and pull request preparation.

Use Codex safely:

- Ask Codex to inspect before editing.
- Keep each request small and specific.
- Tell Codex not to touch gameplay code unless that is the goal.
- Review `git status` before committing.
- Test scenes and prefabs in Unity before pushing gameplay or asset changes.

Avoid conflicts with Claude and Antigravity:

- Use Claude for planning and review before large changes.
- Use Codex for the actual code edits and Git workflow.
- Use Antigravity for local file checks and workspace inspection.
- Do not ask multiple tools to edit the same file at the same time.
- Pull latest changes before starting, and commit or stash your work before switching tasks.
