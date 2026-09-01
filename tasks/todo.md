# Plan: extract Sorolla Core to a shared package + spin up match10

## Goal
1. `Assets/Sorolla Core/` becomes its own repo (`sorolla-studio/sorolla-core`), mounted in every
   project at `Packages/com.sorolla.core` as a **git submodule** (embedded = editable in place).
2. `match10` is a new project cloned from this template, on `sorolla-studio/match10` (private).
3. Editing Core inside match10 → push submodule → template and every other game pull it.

## Facts established
- Unity 6000.5.0f1 (installed locally → batchmode compile check is possible).
- Core: 324 `.cs`, 23 asmdefs, 1 `.asset` (AudioLibrary), 10 prefabs, few png/wav/mp3/shaders. 20 MB.
- **No `Resources/` or `StreamingAssets/` folders inside Core** → safe to move into `Packages/`.
  (`LiveConfigSettings.cs` calls `Resources.Load`, but the *asset* lives outside Core — unaffected.)
- gh: `rodsorolla`, **admin** on `sorolla-studio`. `match10` and `sorolla-core` names are free.
- Template remote: `rodsorolla/UnityGitTemplate`, working tree clean.

## Todo

### Phase 1 — extract Core into its own repo (done in the template first, so match10 inherits it)
- [x] 1.1 `git subtree split --prefix="Assets/Sorolla Core"` → branch with Core's real history
      (preserves authorship; better than a fresh `git init`)
- [x] 1.2 Create private `sorolla-studio/sorolla-core`, push that branch as `main`
- [x] 1.3 Add `package.json` (`com.sorolla.core`), `README.md`, `.gitignore`, `CHANGELOG.md`
      — keep the existing folder structure as-is; asmdefs already drive compilation, so no
      Runtime/Editor reshuffle is needed
- [x] 1.4 In the template: `git rm -r "Assets/Sorolla Core"`, add submodule at
      `Packages/com.sorolla.core`. `.meta` files travel with the split → **GUIDs unchanged**,
      so scene/prefab references survive
- [x] 1.5 Add `"testables": ["com.sorolla.core"]` to `Packages/manifest.json` so Core's tests
      still show in the Test Runner
- [x] 1.6 **Verify**: Unity batchmode compile + assert no missing script references, before committing
- [x] 1.7 Commit + push template

### Phase 2 — create match10
- [x] 2.1 `git clone --recurse-submodules` this repo → `/Users/rodrigolaiz/Documents/Git/match10`
      (clone, not `cp -r`: keeps history and skips the 2.8 GB `Library/`)
- [x] 2.2 Rename: `productName` → `Match10`, `applicationIdentifier` → `com.sorolla.match10`,
      update `CLAUDE.md` + `README.md` project name
- [x] 2.3 Create private `sorolla-studio/match10`, point `origin` at it, push
- [x] 2.4 Add a `template` remote pointing at UnityGitTemplate, for pulling future *non-core*
      template improvements
- [x] 2.5 **Verify**: Unity batchmode compile of match10

### Phase 3 — document the workflow
- [x] 3.1 Short section in `CLAUDE.md`: how to edit Core from inside a game, push it, and pull it
      into the template / other games

## Risks & open points
- **Submodule friction** is the real cost: a submodule sits on a detached HEAD by default, and it
  is easy to commit Core changes and forget to push them. Phase 3 doc + `git config
  submodule.recurse true` mitigates this.
- `com.sorolla.sdk` (sorolla-palette) already exists as a separate git package. Core and the SDK
  overlapping in scope is a question worth answering later — not touching it here.
- Everything is reversible up to 1.7; nothing is force-pushed and no existing repo is modified.

## Review

### Phase 1 — complete (commit 4a194d8)
- `sorolla-studio/sorolla-core` (private) holds Core with **47 commits of real history**
  (subtree split, authorship intact). Tagged nothing yet; `main` is the branch.
- Template now mounts it at `Packages/com.sorolla.core` as a submodule, checked out on `main`.
- `submodule.recurse true` set locally so `git push` also pushes the submodule.
- Safety tag `pre-core-extraction` (a69a1bb) marks the pre-move state.

**Two things found during execution that were not in the plan:**
1. `.claude/skills/sorolla-core` was a symlink into `Assets/Sorolla Core/` — dead after the move.
   Removed; Claude Code auto-discovers the skill at the package location, so it is now redundant.
2. Stale `Assets/Sorolla Core` paths in `SKILL.md` (x5), Core `README.md`, `CLAUDE.md` and
   `unity-gameplay/SKILL.md` — all repointed.

**Verification:** 29 Sorolla assemblies rebuilt post-move, no `error CS` or missing-script
entries in `Editor.log`, UPM resolved `com.sorolla.core@file:` as an embedded package.
(Batchmode run was not possible — the Editor held the project lock — so this was read from
Unity's own build artifacts plus the user confirming a clean compile.)

### Phase 2 + 3 — complete
- `sorolla-studio/match10` (private) created from the template, `origin` repointed, pushed.
  `template` remote → `rodsorolla/UnityGitTemplate` for future non-core template pulls.
- Renamed: `productName` → `Match10`, `applicationIdentifier` → `com.sorolla.match10` on
  Android/Standalone/iPhone (all three were still Unity's URP-blank defaults).
- Submodule workflow documented in `CLAUDE.md` in **both** repos (template commit `ea37e90`).

**Verification (cold batchmode import, no `Library/`):**
`0 compile errors, 0 GUID conflicts, 0 missing scripts, 29/29 Sorolla assemblies, exit 0`.

**RESOLVED — `sorolla-palette` placeholder GUID** (PR #9, merged as `7c6800b` on `master`).
The first cold import of match10 failed with the `AndroidDeviceInfoBuilder` CS0103 errors:
`com.sorolla.sdk`'s `Runtime/SorollaBootstrapper.cs.meta` carries a hand-typed GUID
`a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6` that collides with Unity Purchasing. Patched locally in
each project's `Library/PackageCache` (template + match10) to unblock, but **that patch is
per-machine and evaporates on any cache wipe — every new project will hit this.**
Root fix is one line in `sorolla-studio/sorolla-palette`; not done, awaiting the go-ahead,
and the repo should be swept for other placeholder GUIDs at the same time.


### Palette GUID fix — resolved
`sorolla-palette` `Runtime/SorollaBootstrapper.cs.meta` carried a hand-typed GUID
`a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6` colliding with Unity Purchasing's
`AndroidDeviceInfoBuilder.cs.meta`. Replaced with a generated UUID4
(`462116220a2245be8e3333ecd8347fc3`) via PR #9, squashed to `master` as `7c6800b` after CI passed.
Swept the repo first: 198 metas, all 32-hex, no duplicates, no other placeholders. Verified no
scene/prefab in either project referenced the old GUID before changing it.

**Important gotcha found while verifying — a merged fix does NOT auto-propagate.**
`Packages/packages-lock.json` pins the resolved git commit even when the manifest URL carries no
ref, so palette stayed on `e947c44` and the collision persisted after deleting the PackageCache.
Note the collision is not deterministic about its victim: on that run Unity dropped the *palette*
asset instead of the purchasing one, producing four `CS0234: 'Palette' does not exist in the
namespace 'Sorolla'` errors in Core rather than the usual purchasing `CS0103`s.

To pull a palette change into any project:
```bash
# remove the com.sorolla.sdk entry from Packages/packages-lock.json
rm -rf Library/PackageCache/com.sorolla.sdk@*
# reopen Unity; it re-resolves to master tip and rewrites the lock hash
```
match10 done (commit `7d16fc7`, lock now `7c6800b`), verified by cold import with **no local
patch**: 0 compile errors, 0 GUID conflicts, 29/29 Sorolla assemblies.

**STILL TO DO — the template.** Unity was open on it throughout, so it still carries the
throwaway `Library/PackageCache` patch and a lock pinned to `e947c44`. Apply the three steps
above once Unity is closed.
