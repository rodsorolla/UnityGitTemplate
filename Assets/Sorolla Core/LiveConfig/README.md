# Live Config

Push balance tweaks from a Google Sheet to every installed build on the next
cold boot, without shipping a new binary.

## Architecture

```
 Google Sheet           Apps Script Web App            Device
──────────────     ────────────────────────     ─────────────────────
  Authoring   ──▶  doGet() → JSON payload  ──▶  LiveConfigBootstrapper
                      (6h CacheService)                │
                                                       ▼
                                                  ServiceLocator
                                                  <LiveConfigData>
                                                       │
                                                       ▼
                                          CombatManager / TrainManager /
                                          ShopService / LevelFlow
```

Two layers:

1. **Content registry (SOs, ship with the build)** — `EnemyRegistry`, `DefenderRegistry`
   hold prefabs, sprites, VFX refs. These can't live in JSON and don't change
   without a new binary.
2. **Tunables (JSON, live-updatable)** — HP, damage, wave composition, shop
   prices, bonus-card magnitudes. Authored in SOs for editor convenience,
   serialized to JSON for transport.

### What fields are live-updatable?

A field becomes live-updatable when two things are true:

- It's a scalar, string, or enum (not `UnityEngine.Object` — prefabs, sprites,
  VFX stay on the SOs and ship with the build).
- It's tagged with `[SheetColumn("ColumnName")]`, OR it lives inside a custom
  `IDataSyncTab` implementation.

SOs without any `[SheetColumn]` tags — tutorial steps, UI configs — are
invisible to the sync and the live system. They only change with a new build.

## Fallback chain (runtime)

On cold boot `LiveConfigBootstrapper` tries each in order:

1. **Fetch** the Apps Script URL (8s timeout). On success, writes the JSON to
   `Application.persistentDataPath` and registers the parsed `LiveConfigData`.
2. **Persistent cache** — the JSON from the last successful fetch.
3. **Baked StreamingAssets** — `live_config.json` bundled with the build (always
   present because `LiveConfigBuildPreprocessor` re-bakes it from the current
   SOs on every build).

So a fresh install with no internet still boots with valid data.

## Workflow — making a balance change reach players

1. Edit the Google Sheet (or edit the SO and Push from `Tools → Sorolla Core → Data Sync`).
2. **Clear the Apps Script cache** — open the Apps Script editor, pick the
   `clearCache` function from the dropdown, click Run. Otherwise the 6h
   `CacheService` TTL delays propagation.
3. On a device, **cold-boot** (force-quit + relaunch — suspend/resume doesn't
   re-run `LiveConfigBootstrapper`). New values land.

To verify on-device which source the boot used, enable `Development Build`
when making the build — a tiny overlay in the top-right shows
`LC: Fetched · v1 · sheet-20260423-…`.

## Editor workflows

`Tools → Sorolla Core → Live Config` is the single control panel.

- **Settings** — inline editor for `LiveConfigSettings.asset` (URL, timeout,
  schema version, filename).
- **Editor Dev Mode** — `Use ScriptableObjects on Play` (per-machine via
  `EditorPrefs`). When on, the bootstrapper skips fetch/cache/baked and builds
  `LiveConfigData` straight from the authoring SOs. Lets SO edits take effect
  on next Play with no rebake. Turn off to exercise the real fetch path
  in-editor.
- **Actions** — `Bake JSON from SOs`, `Test Fetch` (hits the URL with
  `?nocache=1` and shows size/version), `Clear Persistent Cache`, reveal
  buttons for both JSON files.
- **Runtime Status** (Play Mode only) — current source, schema version,
  content version, counts, and any fetch/parse issue.

## Extending

### Add a new enemy / defender / level (new instance of an existing type)

Mostly a one-time Unity setup; sheet push is the last step.

1. Create the SO: *Assets → Create → Random Train → Enemies → Melee Enemy Data*
   (or Defender / Wave Config for their equivalents).
2. Drag it into `_EnemyRegistry` / `_DefenderRegistry` / `LevelDatabase`.
   Without this, `CombatManager` / `GameLevelFlowManager` doesn't see it.
3. Fill Unity-only inspector fields — prefab, sprites, VFX refs.
4. If it uses a brand-new `EnemyType` / `DefenderType` value, add an entry to
   the enum (code change).
5. Push once: *Tools → Sorolla Core → Data Sync → Push All* (or per-tab).
   The sheet now has a row for it.

After that, every stat tweak is sheet-only → cold boot → land.

### Add a new enemy subclass with a new tunable parameter

Worked example: a "Jumper" enemy with a `JumpHeight` float. The pattern
applies to any new subclass of `EnemyData` / `DefenderData` that introduces a
field the existing subclasses don't have.

The touchpoints are: **enum → runtime DTO → new SO class → tab registration →
Apps Script → Unity content → seed the sheet → bake**.

1. **Enum value** — `_Game/Scripts/Model/Enums/EnemyType.cs`: add
   `Jumper` at the end of the enum.
2. **Runtime DTO** — `_Game/Scripts/Model/Config/EnemyConfigData.cs`:
   add `public float JumpHeight = 0f;`. Defaults to 0 so non-Jumper enemies
   are unaffected.
3. **New SO class** — new file `_Game/Scripts/Model/Config/JumperEnemyData.cs`:

   ```csharp
   [CreateAssetMenu(fileName = "JumperEnemyData",
       menuName = "Random Train/Enemies/Jumper Enemy Data")]
   public class JumperEnemyData : EnemyData
   {
       [Header("Jumper")]
       [SheetColumn("JumpHeight")] public float JumpHeight = 1.5f;

       public override EnemyConfigData ToConfigData()
       {
           var c = CreateBaseConfig();
           c.JumpHeight = JumpHeight;
           return c;
       }
   }
   ```

4. **Tab registration** — *nothing to do.* `CollectionTab.ConcreteTypes`
   discovers every non-abstract subclass of the tab's base type by reflection,
   so `JumperEnemyData` joins the schema (and becomes creatable on Pull) as
   soon as it compiles.

   This step used to require hand-adding `typeof(JumperEnemyData)` to a
   `ConcreteTypes` array. Such lists drift badly, and the failure is silent:
   the base-type columns are all present, so the tab looks complete while
   every subclass-specific parameter is dropped from Push and ignored on
   Pull. Override `ConcreteTypes` only to deliberately *exclude* a type.
5. **Apps Script** — `tools/AppsScript/LiveConfig.gs`:
   - Add an entry to `ENEMY_FLAGS` for the new subclass (e.g.
     `'JumperEnemyData': { isRanged: false, isFlying: false }`). If you skip
     this, the Apps Script logs a warning in Executions and falls back to
     defaults. For defenders, add to `DEFENDER_PATTERNS` instead.
   - Inside `readEnemies_`'s returned object, add the new field:
     `JumpHeight: num_(r.JumpHeight, 0),`.
   - Redeploy via *Manage deployments → Edit → New version* to keep the URL
     stable.
6. **Unity content** — build the prefab (GameObject with `EnemyView`),
   *Assets → Create → Random Train → Enemies → Jumper Enemy Data*, fill
   inspector fields (Type, Prefab, stats, JumpHeight), drag the SO into
   `_EnemyRegistry.asset`.
7. **Gameplay code** (separate concern) — `JumpHeight` is just data until
   `CombatService` / `EnemyModel` reads it and does something. That's
   game-design work, not part of the live-config plumbing.
8. **Seed + bake** — *Tools → Sorolla Core → Data Sync → Push All* adds the
   `JumpHeight` column and the Jumper row to the sheet. *Tools → Sorolla Core →
   Live Config → Bake JSON from SOs* writes the new schema into
   `StreamingAssets/live_config.json`.
9. **Place in waves** — edit a `WaveConfig` SO and push, or edit the
   `WaveSpawns` tab directly with `EnemyType=Jumper`.

After that, every Jumper tuning change — including `JumpHeight` — is sheet-only,
cold-boot-to-device, no new build required.

The defender equivalent is the same shape: new subclass of `DefenderData`
(auto-discovered by `DefendersTab`), new field on `DefenderConfigData`, and
mirrored in the Apps Script's `readDefenders_`.

### Add a new category of live-updatable config (new SO type)

Uncommon — probably every few months when a new system needs live tuning.
Worked example: a hypothetical `AudioConfig` with master/music/SFX volumes.

1. **Write the SO class** — `AudioConfig : ScriptableObject` with
   `[SerializeField, SheetColumn("MusicVolume")] float _musicVolume;` etc.
   and a `ToData()` method returning a POCO.
2. **Write the POCO DTO** — `AudioConfigData` with the same scalar fields,
   no Unity types. Lives next to `TrainConfigData` in `Model/Config/`.
3. **Write the sync tab** — one line:
   `public class AudioConfigTab : SingleAssetTab<AudioConfig> { public override string TabName => "AudioConfig"; }`.
   `DataSyncWindow` auto-discovers it.
4. **Create the asset** — `Assets → Create → …` → save under
   `_Game/Data/General/AudioConfig.asset`. Fill values.
5. **Push once** — *Tools → Sorolla Core → Data Sync → Push All*. The tab
   appears in the sheet with one row of data.
6. **Add to `LiveConfigData`** — one new field: `public AudioConfigData Audio;`.
7. **Add to `ScriptableObjectLiveConfigBuilder.Build()`** — one line in the
   object initializer: `Audio = LoadSingle<AudioConfig>().ToData(),`.
8. **Add to `tools/AppsScript/LiveConfig.gs`** — a read call and a JSON
   assembly section mirroring the POCO shape. Redeploy via
   *Manage deployments → Edit → New version* so the URL doesn't change.
9. **Consume it** — whoever needs the data resolves
   `ServiceLocator.Instance.Resolve<LiveConfigData>().Audio.MusicVolume`.
10. **Bake + play** — *Tools → Sorolla Core → Live Config → Bake JSON from SOs*
    → Play.

The step that bites people: #8. The Apps Script lives server-side and has
to mirror the C# shape or the fetched JSON won't match what the client
expects.

## Troubleshooting

- **Sheet edit doesn't appear in gameplay** — 99% of the time it's the
  Apps Script cache. Run `clearCache` from the Apps Script editor, then
  cold-boot.
- **Persistent cache is stale** — `Tools → Sorolla Core → Live Config →
  Clear Persistent Cache`, then cold-boot.
- **"Schema vN > client max vM" warning** — server sends a newer payload than
  this client can parse. Bump `MaxSupportedSchemaVersion` in
  `LiveConfigSettings.asset` after the client code handles the new fields.
- **All sources failed** — check that `StreamingAssets/live_config.json`
  exists (re-bake from the Live Config window), that the Apps Script URL
  works in a browser, and that `Assets/Resources/LiveConfigSettings.asset`
  is present.

## Files

| Path                                                                | Role                                                       |
| ------------------------------------------------------------------- | ---------------------------------------------------------- |
| `Sorolla Core/LiveConfig/Runtime/LiveConfigSettings.cs`             | URL + timeout + schema cap SO                              |
| `Sorolla Core/LiveConfig/Runtime/LiveConfigFetcher.cs`              | `UnityWebRequest` wrapper with timeout                     |
| `Sorolla Core/LiveConfig/Runtime/StreamingAssetsReader.cs`          | Cross-platform StreamingAssets reader                      |
| `Sorolla Core/Managers/IAsyncInitializable.cs`                      | Async-init hook for `GameManager._gameManagers`            |
| `_Game/Scripts/Managers/LiveConfigBootstrapper.cs`                  | Orchestrates the fallback chain                            |
| `_Game/Scripts/Managers/LiveConfigEditorPrefs.cs`                   | Per-machine dev toggle                                     |
| `_Game/Scripts/Managers/LiveConfigDebugOverlay.cs`                  | Development-build IMGUI overlay                            |
| `_Game/Scripts/Model/LiveConfig/LiveConfigData.cs`                  | POCO root + `BuildCombatConfigData` helper                 |
| `_Game/Scripts/Model/LiveConfig/ScriptableObjectLiveConfigBuilder.cs` | SO → LiveConfigData converter (editor-only)              |
| `_Game/Scripts/Editor/LiveConfig/LiveConfigBaker.cs`                | Writes `StreamingAssets/live_config.json`                  |
| `_Game/Scripts/Editor/LiveConfig/LiveConfigBuildPreprocessor.cs`    | Auto-bakes before every build                              |
| `_Game/Scripts/Editor/LiveConfig/LiveConfigWindow.cs`               | The control panel                                          |
| `tools/AppsScript/LiveConfig.gs`                                    | Server-side Apps Script Web App                            |
