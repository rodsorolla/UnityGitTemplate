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

The initialization trigger includes “init X game using this template”. Shared
instructions require Sorolla Core architecture, editable gameplay/UI prefabs,
configured prefabs for runtime spawning, editable balancing values, organized
hierarchies, and a wired first playable with the appropriate game loop. They also
require focused scope, preserved Core API contracts, and durable design/progress
notes. Reuse built-in and installed components, DOTween for tweens, standard Unity
anchors/layout components for Canvas UI, and the existing `_Game` folders. Prefer
one configurable prefab for shared structure and behavior, with focused Inspector
settings; reserve variants or separate prefabs for meaningful differences.
Editor testing requires an explicit request in the task prompt. Static checks
are the default; Editor authoring and wiring remain part of implementation.

## Verification

The 2026-09-22 template instruction update aligns `AGENTS.md`, the engineering
guide, initialization workflow, and README on component/prefab reuse, UI layout,
folder reuse, and opt-in Editor testing. Documentation diffs were reviewed and
`git diff --check` passed. No Unity Editor testing was requested or performed;
no game code, assets, or Core files were changed.

Instruction setup only; no Unity import or gameplay validation claimed.
The 2026-09-15 instruction update was reviewed for consistency across `AGENTS.md`,
the engineering guide, initialization workflow, and README; `git diff --check`
passed for these documentation files. No game code, assets, or Core files were
changed by this update. Unrelated working-tree changes were left intact.

The subsequent user-requested full commit includes the upgrade from Unity
6000.5.0f1 to 6000.6.0f1, updated package manifest/lock, URP settings, Project
Auditor settings, and regenerated texture/sprite metadata in the template and
Core. Both package JSON files parse successfully; all 428 modified template
metadata GUIDs and all 8 modified Core metadata GUIDs were preserved. Unity's
generated metadata contains trailing whitespace, retained as generated. No new
compilation or Play Mode validation was run for this commit/push task.

## Known issues / inherited state

Inspect copied project and Core Git status before initialization. Copies can
contain uncommitted work, cached data, and service identities from the template.
Historical `tasks/todo.md` entries describe template development, not game scope.

## Next action

Copy the complete folder, including hidden `.git` contents and optionally the
existing `Library`, rename the copy, open it as the working project, and request
“init X game using this template: [game concept]”. Follow `docs/INIT_PROJECT.md`.

## Decisions

- Preserve Git history and Core submodule when copying.
- Keep the inherited dependency set; no automatic dependency reinstall/upgrade.
- Initialize and build locally without requiring a hosted repository.
- Always use Sorolla Core as the game's architectural foundation and prioritize
  assets that can be edited directly in the Unity Editor.
- Reuse existing tools, components, prefab structure, and folder organization
  before introducing custom equivalents or duplicates.
- Never test in the Unity Editor unless explicitly requested in the task prompt;
  report skipped compilation and gameplay checks without claiming validation.
