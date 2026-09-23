# Sorolla Unity template

To start a game, close Unity and copy this complete folder, including its hidden
`.git` contents. Keeping `Library` is supported. Rename the copy, open that folder
in Codex, and say:

> Init Marble Garden game using this template: a portrait puzzle game where players drag
> colored marbles into matching pots. Implement a playable loop with tutorial,
> restart, and saved progression.

Codex follows `AGENTS.md` and `docs/INIT_PROJECT.md`: it preserves Core and the
existing dependencies, sets game identity, records the brief, and implements the
game. The copy keeps Git history; it does not require a new clone or hosted repo.
Sorolla Core remains a separate submodule; other games adopt updates explicitly.

The shared instructions require Sorolla Core architecture, editable prefabs for
gameplay and UI, prefab-based runtime spawning, Inspector-editable balancing,
organized scenes, and a complete playable loop. Reuse existing components and
`_Game` folders, use DOTween for tweens and standard Unity anchors/layout tools
for Canvas UI, and prefer one configurable prefab for shared structure and
behavior. Unity Editor testing runs only when explicitly requested in the task
prompt; static checks are the default. Design decisions and progress are
recorded for the next session.

- [Initialization workflow](docs/INIT_PROJECT.md)
- [Game brief](docs/GAME_BRIEF.md)
- [Current state](docs/PROJECT_STATE.md)
- [Engineering conventions](docs/SOROLLA_GUIDE.md)

The old `scripts/new-project.sh` workflow has been removed. Historical task notes
may describe that retired workflow; they are not current setup instructions.
