# DataSheet

Edit every asset of a `ScriptableObject` type as a spreadsheet. Pick a type — each asset becomes a row, each serialized field a column. Edit inline with the real Unity property drawers and full native Undo. No Inspector clicking, no leaving the editor.

Editor-only, reusable, zero game dependencies.

## Opening

Menu: **Tools → Sorolla → DataSheet**

## Layout

```
┌────────────────────────────────────────────────────────────────────────┐
│ [TypeName (12) ▾] [search…] [Columns ▾] [+ Create] [Export ▾] [Import]  │  ← toolbar
│                                                       [History (3)] [↻] │
├────────────────────────────────────────────────────────────────────────┤
│ Name        │ Hp   │ Speed │ Weapon Type │ Icon          │ Tint         │  ← header
│ IronSword   │ 12   │ 1.0   │ [Sword   ▾] │ [⊙ sword.png] │ [██████]     │  ← rows = assets
│ Excalibur   │ 50   │ 1.2   │ [Sword   ▾] │ [⊙ none    ] │ [██████]     │
├────────────────────────────────────────────────────────────────────────┤
│                    < Page 1 / 3 · 120 assets >                          │  ← pager (50/page)
└────────────────────────────────────────────────────────────────────────┘
```

## Toolbar

| Control | What it does |
|---------|--------------|
| **Type picker** | Lists every concrete `ScriptableObject` type that has at least one asset, with its asset count. Pick one to load the grid. |
| **Search** | Filters rows by asset name (case-insensitive substring). |
| **Columns ▾** | Toggle individual columns on/off. Choice is remembered per type (stored in `EditorPrefs`). |
| **+ Create** | Creates a new asset of the current type (save-file dialog), then adds it as a row. |
| **Export ▾** | Writes the visible columns/rows to a **CSV** or **JSON** file. |
| **Import** | Reads a CSV/JSON file and writes matching values back into assets (with a diff preview — see below). |
| **History (n)** | Toggles the session change-log panel. |
| **↻** | Rescans types and reloads rows (use after renaming/adding assets outside the window). |

## Editing

- Each cell is a real `PropertyField`: enums show dropdowns, object references show object pickers, colors show swatches.
- Edits apply immediately and register **native Undo** — press **Ctrl/Cmd+Z** to revert any edit.
- Array and nested-struct fields can't be edited inline (they'd break the grid); they show a compact read-only summary like `[3]` instead.

## Detail panel (complex fields)

Lists, arrays, and nested structs can't be edited in a grid cell (they show as a `[N]`
summary). Click the **▸** button at the start of a row to open the **Detail panel** on the
right — it shows that asset's complex fields as full, editable drawers (with native Undo).
Scalars and object references stay in the grid. Click **✕** to close. The Detail panel and
the History panel can be open at the same time (they stack as right-side columns).

## Export

Exports the **visible** columns and all (filtered) rows. Column 0 is always the asset **Name** (the row key).

- Scalars (int, float, bool, string, enum, color) export as text.
- Object references export as their asset **path** (read-only — not re-imported).
- Arrays / structs are skipped.

## Import (round-trip)

Import matches rows by **asset name** and writes **scalar fields only**. This keeps the round-trip safe — object references and arrays are never written from a text file, so there's no risk of mismatched-GUID corruption.

1. Pick a CSV/JSON file (typically one you exported and edited in a spreadsheet).
2. DataSheet diffs it against the current assets and shows a preview: how many cell changes will apply, plus any unmatched rows that will be ignored.
3. Confirm to apply. Every applied change is one Undo step and is recorded in History.

> Import is a **one-way merge**: only rows whose name matches a current asset are updated. Assets absent from the file are left untouched (never deleted).

### Duplicate names

Because rows are matched by name, two assets of the same type sharing a name are ambiguous. DataSheet detects this, shows a warning banner, and flags it in the import dialog rather than silently writing to the wrong asset.

## History

The History panel lists every cell edit made during the session (newest first), each with the asset, field, old → new value, and a timestamp. **Revert** restores the old value (one more Undo step). The log is in-memory only — it clears when Unity closes, and complements Ctrl/Cmd+Z.

## Module layout

```
Sorolla Core/DataSheet/Editor/
├── Sorolla.DataSheet.Editor.asmdef   # Editor-only assembly, no game deps
├── DataSheetValues.cs                # scalar SerializedProperty <-> string (shared core)
├── DataSheetIO.cs                    # SheetTable, CSV/JSON serialize/parse, name-keyed diff
├── DataSheetHistory.cs               # in-memory session change-log
├── DataSheetModel.cs                 # type discovery, column model, row loading
├── DataSheetTable.cs                 # IMGUI grid + edit/history capture
├── DataSheetWindow.cs                # the EditorWindow (toolbar, paging, export/import)
└── Tests/                            # 12 EditMode tests (Values/IO/History/Model)
```

Namespace: `Sorolla.DataSheet.Editor`.

## Scope

Intentionally out of scope (keep it simple): persistent/on-disk history, full asset version snapshots, importing object references or arrays, and UI Toolkit rendering.
