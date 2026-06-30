using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Sorolla.DataSheet.Editor
{
    /// <summary>A flat string table. Column 0 is always the asset name (row key).</summary>
    public class SheetTable
    {
        public List<string> Columns = new List<string>();
        public List<List<string>> Rows = new List<List<string>>();
    }

    public struct CellChange
    {
        public string rowKey;
        public string column;
        public string oldValue;
        public string newValue;
    }

    /// <summary>Result of diffing an imported table against the current one.</summary>
    public class ImportDiff
    {
        public List<CellChange> Changes = new List<CellChange>();
        public List<string> UnmatchedRows = new List<string>();
    }

    /// <summary>
    /// Pure serialization for DataSheet: CSV (hand-written, RFC-4180-ish) and JSON
    /// (via JsonUtility with a columns/rows wrapper), plus name-keyed diffing.
    /// No Unity asset access here — operates on <see cref="SheetTable"/> strings only.
    /// </summary>
    public static class DataSheetIO
    {
        // ---------- CSV ----------

        public static string ToCsv(SheetTable table)
        {
            var sb = new StringBuilder();
            WriteCsvRow(sb, table.Columns);
            foreach (var row in table.Rows)
                WriteCsvRow(sb, row);
            return sb.ToString();
        }

        static void WriteCsvRow(StringBuilder sb, List<string> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(EscapeCsv(cells[i] ?? ""));
            }
            sb.Append('\n');
        }

        static string EscapeCsv(string s)
        {
            bool needsQuote = s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r");
            if (!needsQuote) return s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        public static SheetTable ParseCsv(string csv)
        {
            var table = new SheetTable();
            var records = ParseCsvRecords(csv);
            if (records.Count == 0) return table;
            table.Columns = records[0];
            for (int r = 1; r < records.Count; r++)
                table.Rows.Add(records[r]);
            return table;
        }

        // Splits CSV text into records (list of fields), honoring quotes/escapes/newlines.
        static List<List<string>> ParseCsvRecords(string csv)
        {
            var records = new List<List<string>>();
            var field = new StringBuilder();
            var record = new List<string>();
            bool inQuotes = false;
            bool sawAny = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char c = csv[i];
                sawAny = true;
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"') { field.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else field.Append(c);
                }
                else
                {
                    switch (c)
                    {
                        case '"': inQuotes = true; break;
                        case ',': record.Add(field.ToString()); field.Clear(); break;
                        case '\r': break;
                        case '\n':
                            record.Add(field.ToString()); field.Clear();
                            records.Add(record); record = new List<string>();
                            break;
                        default: field.Append(c); break;
                    }
                }
            }
            // flush trailing field/record if the file didn't end with a newline
            if (field.Length > 0 || record.Count > 0)
            {
                record.Add(field.ToString());
                records.Add(record);
            }
            else if (!sawAny)
            {
                // empty input -> no records
            }
            return records;
        }

        // ---------- JSON ----------

        [System.Serializable]
        class JsonRow { public List<string> cells = new List<string>(); }

        [System.Serializable]
        class JsonSheet { public List<string> columns = new List<string>(); public List<JsonRow> rows = new List<JsonRow>(); }

        public static string ToJson(SheetTable table)
        {
            var js = new JsonSheet { columns = new List<string>(table.Columns) };
            foreach (var row in table.Rows)
                js.rows.Add(new JsonRow { cells = new List<string>(row) });
            return JsonUtility.ToJson(js, true);
        }

        public static SheetTable ParseJson(string json)
        {
            var js = JsonUtility.FromJson<JsonSheet>(json) ?? new JsonSheet();
            var table = new SheetTable { Columns = js.columns ?? new List<string>() };
            if (js.rows != null)
                foreach (var r in js.rows)
                    table.Rows.Add(r.cells ?? new List<string>());
            return table;
        }

        // ---------- Diff ----------

        /// <summary>
        /// Diffs imported against current, matching rows by column 0 (Name).
        /// Only columns present in BOTH headers are compared (unknown imported columns ignored).
        /// Imported rows whose name has no current match are reported in UnmatchedRows.
        /// </summary>
        public static ImportDiff Diff(SheetTable current, SheetTable imported)
        {
            var diff = new ImportDiff();

            // index current rows by name
            var currentByName = new Dictionary<string, List<string>>();
            foreach (var row in current.Rows)
                if (row.Count > 0) currentByName[row[0]] = row;

            foreach (var imp in imported.Rows)
            {
                if (imp.Count == 0) continue;
                string name = imp[0];
                if (!currentByName.TryGetValue(name, out var cur))
                {
                    diff.UnmatchedRows.Add(name);
                    continue;
                }

                for (int ci = 1; ci < imported.Columns.Count; ci++)
                {
                    string col = imported.Columns[ci];
                    int curCol = current.Columns.IndexOf(col);
                    if (curCol < 0) continue; // unknown column -> ignore

                    string newVal = ci < imp.Count ? imp[ci] : "";
                    string oldVal = curCol < cur.Count ? cur[curCol] : "";
                    if (newVal != oldVal)
                    {
                        diff.Changes.Add(new CellChange
                        {
                            rowKey = name,
                            column = col,
                            oldValue = oldVal,
                            newValue = newVal
                        });
                    }
                }
            }
            return diff;
        }
    }
}
