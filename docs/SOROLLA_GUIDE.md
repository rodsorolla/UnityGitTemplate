# Sorolla engineering guide

## Source of truth

Use the installed `Packages/com.sorolla.core/README.md` as the module index and
read the current source before implementing an API recipe. Module READMEs cover
additional setup. Core also carries reference material under
`.claude/skills/sorolla-core/`; read it explicitly when helpful rather than
assuming nested Claude skills are automatically discovered by Codex.

Read dependency versions from `Packages/manifest.json`, `Packages/packages-lock.json`,
installed package metadata, and `ProjectSettings/ProjectVersion.txt`. Do not copy
version numbers from historical task notes or upgrade dependencies during game initialization.

## Implementation conventions

- Core namespaces are `Sorolla` and its module namespaces. Game-specific code
  lives under `Assets/_Game/` with a game-specific namespace where appropriate.
- Before creating files or folders, inspect the current `Assets/_Game/` hierarchy.
  Reuse existing folders and naming conventions. Create a missing folder only
  under the appropriate existing feature or asset-type parent; avoid parallel
  structures such as a second UI or prefab tree for the same purpose.
- Private fields use `_camelCase`; public members use `PascalCase`; events use
  an `On` prefix; interfaces use an `I` prefix.
- Use private `[SerializeField]` fields, meaningful inspector headers, and
  numeric constraints. Document public API contracts and non-obvious decisions.
- Use UniTask for game async APIs. Tie async work to the relevant lifetime and
  handle cancellation. Use `.Forget()` deliberately for fire-and-forget work.
- Use the installed DOTween API for tweens instead of custom interpolation loops
  or a new tween framework; `.AsyncWaitForCompletion()` is available where
  appropriate. Reuse existing tween components when they fit. Do not assume
  optional UniTask tween integration is installed.
- Prefer ZLinq with `.AsValueEnumerable()` for runtime LINQ. Editor code can use
  System.Linq. Use Newtonsoft.Json through the existing persistence infrastructure.
- Cache frequently used components, pool frequently spawned objects, and avoid
  unnecessary allocations or disk writes in gameplay loops. Batch persistence at
  sensible checkpoints rather than saving each frame or item.

## Architecture and common tasks

Always use Sorolla Core as the architectural foundation: persistent data, level
management, tools, utilities, and other relevant SDK systems. Inspect the installed
SDK for each feature and extend its existing systems instead of building competing
implementations. Keep game-specific behavior in `Assets/_Game/`; reusable Core
improvements must preserve existing API contracts and unrelated work.

Before adding a component or utility, inspect Unity's built-in components, the
installed SDK, and existing project tools for a suitable implementation. Prefer
configuring or composing them over writing custom equivalents. Add custom
components for game-specific behavior or a concrete gap; do not wrap an existing
component merely to rename its settings or reproduce its functionality.

- Inspect `GameInitializer`, `GameManager`, `SorollaManager`, and service registration
  before adding boot work. `IAsyncInitializable` supports awaited boot tasks;
  check the current initialization ordering and avoid double initialization.
- UI: inspect `UIManager`, `UIScreen`, and `UIRegistry`. Create game prefabs under
  `_Game`, register them, wire references, and use the existing opening/closing APIs.
- Persistence: inspect `SaveSystem`, `ISaveData`, and a comparable existing save
  model. Preserve existing save contracts and handle migration when changing them.
- Tutorials: inspect `TutorialStepBase` and `TutorialController`, create the
  appropriate game-specific step/assets, and configure the actual sequence.
- Level flow: use the existing setup, cleanup, and result lifecycle. Core reserves
  level-end reason values below 100; use the current custom-reason contract for
  game reasons. C# enums cannot be extended with partial declarations.
- Treat scene wiring, ScriptableObjects, UI references, and `.meta` files as part
  of implementing a feature. C# files alone do not complete a Unity feature.

## Prefabs and Editor authoring

- Author reusable gameplay objects and UI as editable prefabs with serialized
  references and clear Inspector settings. Keep scene composition easy to inspect
  and adjust in Unity.
- Runtime spawning must instantiate configured prefabs. Do not construct entire
  gameplay objects or UI hierarchies in code when they could be authored and
  edited in Unity.
- Prefer one configurable prefab when structure and behavior are shared. Supply
  different content/data and use supported states rather than creating a prefab
  per label, reward, item, or similar content change. Use variants or separate
  prefabs for meaningful structural or behavioral differences. Keep Inspector
  fields focused on required references and useful configuration; avoid a large
  set of switches for unrelated cases while preserving editable balancing.
- For Canvas UI, author responsive layouts with standard RectTransform anchor
  presets, pivots, offsets, and appropriate built-in components such as layout
  groups, LayoutElement, ContentSizeFitter, and CanvasScaler. Do not create custom
  anchor systems or scripts that repeatedly set RectTransform geometry when
  Unity's standard layout tools can achieve the result. Add custom layout logic
  only for a concrete requirement those tools cannot express.
- Expose balancing values through serialized fields or ScriptableObjects rather
  than hardcoding them in gameplay code.
- Use meaningful object names, organized hierarchies, and intentional prefab
  overrides. Prefer Inspector references over runtime object searches.
- Deliver a wired starting scene that reaches gameplay on Play. Establish the
  smallest complete loop with appropriate start, play, result, and restart flow
  before expanding the game. Avoid speculative systems and unnecessary dependencies.

## Verification policy

Never test in the Unity Editor unless the user explicitly requests testing in
the task prompt. Do not enter Play Mode, run Edit Mode/Play Mode tests, or launch
Unity (including batch mode) for compilation/import or gameplay verification by
default. Using the Editor to author assets and wire scenes/prefabs is still
allowed; automatic imports during authoring do not authorize a test session.

Use static source, serialized asset, reference, and diff checks by default.
Report precisely which checks ran and which did not. Keep delivering the wired
game flow, but do not describe it as tested or compilation-verified without
evidence. When testing is explicitly requested, run checks relevant to the task
and report their results. This policy also applies to Core changes and updates.

## Core changes across games

The parent game records one Core commit. Game-specific changes stay in `_Game`;
reusable improvements can be made inside the Core repository. Palette may be a
Core dependency; Palette must not depend on Core.

Before editing Core, inspect its status and branch. If detached, create a suitable
branch at the current commit before committing; do not switch to a moving `main`
and accidentally change the game's dependency baseline. Preserve unrelated work.

When committing/publishing is requested:

1. Review the Core change, verify it according to the policy above, and commit
   only its intended files inside Core.
2. Push the Core commit to the shared Core repository so it is fetchable.
3. Commit the `Packages/com.sorolla.core` pointer change in the game repository,
   then publish the game commit as requested.

Do not assume `submodule.recurse=true` makes pushes publish Core commits. Check
push configuration explicitly, or push Core explicitly before the parent.

Other games stay on their recorded commits. Adopt an improvement by fetching
Core, reviewing and selecting the intended revision, checking local modifications,
checking it in that game according to the verification policy above, then
committing its pointer update. Do not update every
game automatically. Template improvements outside Core must be applied separately;
do not blindly merge the template's game assets into an established game.
