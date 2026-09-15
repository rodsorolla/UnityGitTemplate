# Initialize a copied Sorolla project

## User workflow

Close the template's Unity editor so files are consistent. Copy the complete
folder, including hidden `.git` contents and `Library` if desired. Rename the
copy and open that folder as the agent's working project. Say, for example:

> Init Marble Garden game using this template: a portrait mobile puzzle where the player
> drags colored marbles into matching pots. Build a playable prototype with a
> short tutorial, level completion, restart, and saved progression.

The agent handles the steps below and continues into implementation. No project
creation script, fresh clone, GitHub account, or dependency reinstall is required.

## 1. Inspect and identify the copy

- Resolve the working path and inspect `git status --short`, `git remote -v`,
  `.gitmodules`, and `git submodule status`.
- Inspect Core separately with `git -C Packages/com.sorolla.core status --short`
  and `git -C Packages/com.sorolla.core rev-parse HEAD`.
- Inspect `docs/GAME_BRIEF.md` and `docs/PROJECT_STATE.md`. If already initialized,
  resume that game; do not repeat identity or remote changes. A deliberate rename
  is a separate user request.
- Confirm this is the requested copy, not the source template. If the request
  could rebrand the actual source template unintentionally, ask for the copy's
  location before making identity changes; read-only inspection can continue.
- Preserve existing modifications. Read Unity version, manifests, scene order,
  bootstrap components, registries, and the relevant Core APIs before editing.
- Keep `Library`. It is an ignored cache; Unity may refresh it. Do not delete it
  preemptively, force a cold import, or copy another project's cache over it.

## 2. Preserve independent Git repositories

A normal folder copy contains its own root `.git` directory. Core's `.git` file
normally points to `../../.git/modules/Packages/com.sorolla.core`. Confirm with
`git rev-parse --absolute-git-dir` in both repositories that their resolved Git
storage belongs to this copy. Check for external worktree/common-directory or
alternate-object-store links if the copied metadata is not a normal standalone
repository. Do not mutate a repository whose Git storage still belongs to the source.

- Keep the root `.git`, `.gitmodules`, Core working files, and Core revision.
  Do not run `git init`, reset/amend inherited commits, checkout Core's `main`,
  or run `git submodule update --remote` to initialize a game.
- If the copied game `origin` still targets UnityGitTemplate, record its URL and
  remove that remote from the copy (`git remote remove origin`). This leaves the
  game local until its own destination is provided. Inspect remaining remotes
  and branch upstream/push settings so none can accidentally publish the game
  back to the template. Leave Core's shared origin unchanged.
- If the user supplies a new game remote, set that as the parent `origin`.
  Do not invent a URL or require repository creation to continue implementation.
- If `.git` was omitted or the submodule is broken, preserve the files and any
  local changes first. Diagnose and repair the specific metadata problem; do not
  delete and reclone Core over existing work.
- Record the initial Core commit and Git destination in `docs/PROJECT_STATE.md`.

## 3. Write the brief and set game identity

Populate `docs/GAME_BRIEF.md` from the user's concept. Define a concrete first
playable and make explicit assumptions for unspecified, reversible details.
Record `Lifecycle: initializing` in project state before beginning mutations;
record each completed initialization step so interrupted work can resume safely.

- Set Unity `productName` from the game name; preserve company `Sorolla` unless
  requested otherwise. Derive a valid lowercase identifier such as
  `com.sorolla.marblegarden`, or use the supplied identifier. Update the platform
  entries within `applicationIdentifier`, including Android and iPhone.
- Give the copy its own product GUID once. Record the chosen identity so reruns
  preserve it. When editing YAML, safely encode values rather than interpolating
  arbitrary names into shell commands or regex replacement strings.
- Inspect Unity Cloud linkage (`cloudProjectId`, `projectName`, `organizationId`)
  and game-specific analytics, Firebase, purchasing, or remote-config identities.
  Template identities are not new-game identities. Unlink inherited cloud identity
  using supported Unity project settings; do not provision external services or
  fabricate credentials. Record any service setup still needed and use existing
  local/mock/baked paths where available to keep the prototype playable.
- Preserve dependency versions, package lock, Core revision, render pipeline,
  and existing scenes/prefabs. Adapt them deliberately for the game.
- Keep `.meta` files paired with assets. Let Unity regenerate `.sln`/`.csproj`
  files as needed instead of treating generated IDE files as project identity.
- Preserve historical task notes (archive them under `tasks/archive/` if replacing
  the checklist), then create the current implementation checklist.

## 4. Implement the requested game

Always build the architecture on Sorolla Core, using its persistent data, level
manager, tools, utilities, and other relevant systems. Inspect the installed SDK
before implementing features and extend existing systems instead of creating
competing implementations. Follow the authoring rules in `AGENTS.md` and
`docs/SOROLLA_GUIDE.md`.

Implement the actual game in `Assets/_Game/`. Author reusable gameplay objects and
UI as editable prefabs, and instantiate configured prefabs for runtime spawning.
Do not construct entire gameplay objects or UI hierarchies in code when they could
be authored and edited in Unity. Expose balancing values through serialized fields
or ScriptableObjects. Use meaningful names, organized hierarchies, intentional
prefab overrides, and Inspector references instead of runtime object searches.

Wire scene and prefab references, configure registries and data assets, and make
the starting scene reach gameplay when the user presses Play. Include the
appropriate start, play, result, and restart flow in the first prototype.
Read the real build scene order instead of assuming README example scene names.
Build the smallest complete playable version first, then continue through the
requested scope. Avoid speculative systems, unnecessary dependencies, and unrelated
monetization or live-service systems.

## 5. Verify and leave a usable handoff

- Use the Unity version declared in `ProjectVersion.txt`. Do not open a second
  editor process on an already-open project. Use the active editor where possible.
- Check compilation/import, missing scripts/references, and the entry-to-game flow.
  Exercise controls, tutorial, win/lose where applicable, restart, and persistence
  across a restart. Run relevant existing tests for changed systems.
- Inspect the authored prefabs and their scene instances: references are assigned,
  balancing values are editable, and runtime spawning uses configured prefabs.
- Treat editor process failures and runtime exceptions as failures, not merely
  an absence of `error CS` messages. If Unity execution is unavailable, document
  exact checks still needed and do not claim the game was played or validated.
- Inspect the parent and Core diffs separately. Verify initialization did not
  unexpectedly change dependencies or Core revision. Do not commit `Library`.
- Update project state with completed identity changes, working features, Core
  revision, verification evidence, limitations, and next tasks. Mark lifecycle
  `initialized` once local identity/context setup is complete; track gameplay
  completion and validation separately. Keep incomplete steps visible.
- Report what is playable, how to run it, assumptions made, and any blockers.
  Commit/push only within the user's requested scope; never publish to template origin.
