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
- [ ] 1.1 `git subtree split --prefix="Assets/Sorolla Core"` → branch with Core's real history
      (preserves authorship; better than a fresh `git init`)
- [ ] 1.2 Create private `sorolla-studio/sorolla-core`, push that branch as `main`
- [ ] 1.3 Add `package.json` (`com.sorolla.core`), `README.md`, `.gitignore`, `CHANGELOG.md`
      — keep the existing folder structure as-is; asmdefs already drive compilation, so no
      Runtime/Editor reshuffle is needed
- [ ] 1.4 In the template: `git rm -r "Assets/Sorolla Core"`, add submodule at
      `Packages/com.sorolla.core`. `.meta` files travel with the split → **GUIDs unchanged**,
      so scene/prefab references survive
- [ ] 1.5 Add `"testables": ["com.sorolla.core"]` to `Packages/manifest.json` so Core's tests
      still show in the Test Runner
- [ ] 1.6 **Verify**: Unity batchmode compile + assert no missing script references, before committing
- [ ] 1.7 Commit + push template

### Phase 2 — create match10
- [ ] 2.1 `git clone --recurse-submodules` this repo → `/Users/rodrigolaiz/Documents/Git/match10`
      (clone, not `cp -r`: keeps history and skips the 2.8 GB `Library/`)
- [ ] 2.2 Rename: `productName` → `Match10`, `applicationIdentifier` → `com.sorolla.match10`,
      update `CLAUDE.md` + `README.md` project name
- [ ] 2.3 Create private `sorolla-studio/match10`, point `origin` at it, push
- [ ] 2.4 Add a `template` remote pointing at UnityGitTemplate, for pulling future *non-core*
      template improvements
- [ ] 2.5 **Verify**: Unity batchmode compile of match10

### Phase 3 — document the workflow
- [ ] 3.1 Short section in `CLAUDE.md`: how to edit Core from inside a game, push it, and pull it
      into the template / other games

## Risks & open points
- **Submodule friction** is the real cost: a submodule sits on a detached HEAD by default, and it
  is easy to commit Core changes and forget to push them. Phase 3 doc + `git config
  submodule.recurse true` mitigates this.
- `com.sorolla.sdk` (sorolla-palette) already exists as a separate git package. Core and the SDK
  overlapping in scope is a question worth answering later — not touching it here.
- Everything is reversible up to 1.7; nothing is force-pushed and no existing repo is modified.

## Review
_(filled in after execution)_
