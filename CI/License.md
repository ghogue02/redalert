# Unity License Guidance for CI

This repository uses `game-ci/unity-builder` with a Unity license provided via the `UNITY_LICENSE` GitHub secret. If CI shows activation errors (for example: "No valid Unity license", "ULF parse error", "Entitlement check failed"), validate the following:

Checklist
1) Secret content: `UNITY_LICENSE` must contain the raw ULF (text) content with proper newlines. Do not wrap the license in quotes, JSON, or base64 unless explicitly required by your process.
2) Scope: Ensure the license/entitlement covers headless/CI builds for the Unity LTS version used by this repo.
3) Rotation: If you rotated or revoked your license/seat, update the GitHub secret and re-run.
4) Activation docs: For Unity Personal or Build Server workflows, follow game-ci’s official activation guidance to generate a valid ULF for CI:
   - https://game.ci/docs/github/activation
   - https://game.ci/docs/github/activation-manual
5) No secrets in repo: Never commit license files or serials to the repository.

Version Alignment
- The workflows auto-detect `ProjectSettings/ProjectVersion.txt` and use that version if present; otherwise they default to `2022.3.48f1`.
- If logs indicate version mismatch, ensure `ProjectSettings/ProjectVersion.txt` exists and reflects the intended editor version, or pin the Unity version explicitly in the workflow.

After fixing the license or version:
1) Re-run the "Build WebGL (Artifact Only)" workflow.
2) Inspect the `logs-snapshot` artifact for `Editor.log` to confirm activation succeeded.