# Project state

Lifecycle: template

This is the reusable UnityGitTemplate base. Do not rebrand this source template
when asked to maintain the template itself. Initialize only a user-designated
copy when asked to start a game.

## Starting configuration

- Unity version: read `ProjectSettings/ProjectVersion.txt`.
- Core: `Packages/com.sorolla.core`, a separate repository pinned by the parent.
- Build scene order: read `ProjectSettings/EditorBuildSettings.asset`.
- Dependency versions: read project manifests and installed package metadata.
- Shared game implementation conventions: `docs/SOROLLA_GUIDE.md`.

## Working features

Template infrastructure is present. No new game has been implemented through
this workflow yet.

## Verification

Instruction setup only; no Unity import or gameplay validation claimed.

## Known issues / inherited state

Inspect copied project and Core Git status before initialization. Copies can
contain uncommitted work, cached data, and service identities from the template.
Historical `tasks/todo.md` entries describe template development, not game scope.

## Next action

Copy the complete folder, including hidden `.git` contents and optionally the
existing `Library`, rename the copy, open it as the working project, and request
“init this project with this game …”. Follow `docs/INIT_PROJECT.md`.

## Decisions

- Preserve Git history and Core submodule when copying.
- Keep the inherited dependency set; no automatic dependency reinstall/upgrade.
- Initialize and build locally without requiring a hosted repository.
