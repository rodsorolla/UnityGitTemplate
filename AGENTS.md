# Sorolla Unity project

## Start every task

Read `docs/GAME_BRIEF.md` and `docs/PROJECT_STATE.md` first. Read
`docs/SOROLLA_GUIDE.md` for engineering conventions, then the installed Core
`Packages/com.sorolla.core/README.md` and relevant source when using its APIs.
The user's current instructions take precedence over this guidance.

When the user says “init X game using this template”, “init this project with
this game …”, or equivalent, follow `docs/INIT_PROJECT.md` and continue into
implementing the requested game. Requests to edit or discuss these instructions
are template maintenance and do not trigger game initialization. Do not
stop after renaming settings, writing a plan, or generating scripts without scene wiring.

## Game architecture and authoring

- Always build the game's architecture on the Sorolla Core SDK. Use its persistent
  data, level manager, tools, utilities, and other relevant systems. Inspect the
  installed SDK before implementing a feature; extend existing systems instead
  of creating competing implementations.
- Prefer editable prefabs for reusable gameplay objects and UI, with serialized
  references and clear Inspector settings. Keep scene composition easy to inspect
  and adjust in Unity.
- Runtime spawning must instantiate configured prefabs. Do not construct entire
  gameplay objects or UI hierarchies in code when they could be authored and
  edited in Unity.
- Expose balancing values through serialized fields or ScriptableObjects. Avoid
  hardcoded gameplay values.
- Deliver a wired starting scene that reaches gameplay when the user presses
  Play. Include the appropriate start, play, result, and restart flow in the first
  prototype.
- Use meaningful object names, organized hierarchies, and intentional prefab
  overrides. Prefer Inspector references over runtime object searches.
- Build the smallest complete playable version first, then continue through the
  requested scope. Avoid speculative systems and unnecessary dependencies.

## Working agreements

- Treat the current folder as the project. Inspect it before making changes.
- Preserve existing work, including changes inside the Core submodule.
- Make reasonable reversible choices, record assumptions, and keep working.
  Ask only for missing information that blocks meaningful progress. A missing
  remote repository or service account does not block local game development.
- Keep changes focused. Fix causes, reuse existing systems, and avoid speculative
  frameworks. Give concise progress updates; no routine plan approval is required.
- For substantial work, maintain a checklist in `tasks/todo.md`; it is execution
  tracking, not an approval gate. Do not confuse historical tasks with current scope.
- At completion, update `docs/PROJECT_STATE.md` with what works, verification,
  unresolved issues, and next steps. Update the brief when design decisions change.
  Store durable context in files instead of relying on prior conversations.

## Boundaries

- Game-specific code and assets belong in `Assets/_Game/`.
- `Packages/com.sorolla.core` is its own Git repository. Use it for reusable SDK
  improvements, preserving unrelated work and existing API contracts; keep each
  game's chosen revision stable until an intentional update.
- Preserve Unity version, dependency pins, `.meta` GUIDs, rendering configuration,
  and copied `Library` unless evidence requires a targeted change.
- Never reset Git history, delete `.git`, bulk-clean user work, or update Core to
  latest as part of initialization. Do not publish or create a hosted repository
  unless requested. Local implementation can proceed without an `origin`.
- Verify compilation and the actual playable scene flow when Unity is available.
  Report skipped checks clearly; code generation alone is not a playable-game check.
