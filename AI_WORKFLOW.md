# AI Workflow

This project can be worked on with Unity, Codex, Claude, Antigravity, and GitHub. Keep each tool focused so changes stay safe and easy to review.

## Tool Roles

Claude is for planning and review.

- Use Claude to discuss architecture, feature ideas, risks, and code-review questions.
- Ask Claude to review plans before large changes.
- Do not rely on Claude as the final source of truth for Unity scene or prefab behavior.

Codex is for coding, refactoring, documentation, Git branches, commits, and pull request preparation.

- Ask Codex to inspect the repository before editing.
- Keep Codex tasks small and specific.
- Tell Codex which files or systems are allowed to change.
- Ask Codex to show changed files before committing.

Antigravity is for local workspace and file checks.

- Use Antigravity to inspect folders, compare files, and check local workspace state.
- Use it to confirm whether generated Unity folders are present.
- Avoid using it to make overlapping edits while Codex is editing the same files.

Unity is for testing scenes and prefabs.

- Open the project in Unity Hub.
- Use Unity `6000.5.2f1` when possible.
- Test scenes, prefabs, serialized fields, materials, audio, and gameplay behavior inside the Unity Editor.
- Save scene and prefab changes intentionally.

## Safe Workflow

1. Pull the latest changes.
2. Create a new branch.
3. Make one focused change.
4. Test in Unity when assets, scenes, prefabs, or gameplay code changed.
5. Review `git status`.
6. Commit only the files related to the task.
7. Push the branch and open a pull request.

## Conflict Avoidance

- Do not let Claude, Codex, and Antigravity edit the same file at the same time.
- Do not edit Unity scenes or prefabs in multiple tools at once.
- Prefer one active branch per task.
- Commit or stash local work before switching tasks.
- Pull latest changes before starting a new session.
- If Unity rewrites scene, prefab, or `.asset` files unexpectedly, inspect the diff before committing.

## Unity Files To Track

These folders should stay in Git:

- `Assets`
- `Packages`
- `ProjectSettings`

These Unity-generated folders should stay out of Git:

- `Library`
- `Temp`
- `Logs`
- `Obj`
- `Build`
- `Builds`
- `UserSettings`

## Large Assets

Use Git LFS for large binary assets such as:

- Photoshop files: `*.psd`
- Models: `*.fbx`, `*.blend`
- Audio: `*.wav`, `*.mp3`
- Video: `*.mp4`, `*.mov`
- Images: `*.png`, `*.jpg`

Before adding large assets, confirm Git LFS is installed:

```bash
git lfs install
git lfs track
```
