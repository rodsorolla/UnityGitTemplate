using System.Collections.Generic;

namespace Sorolla.GoogleSheets
{
    /// <summary>
    /// Static utility for parsing CSV data from Google Sheets.
    /// </summary>
    public static class CSVParser
    {
        /// <summary>
        /// Parses a single CSV line, handling quoted fields.
        /// </summary>
        public static string[] ParseLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            string current = "";

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.Trim());
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            result.Add(current.Trim());

            return result.ToArray();
        }

        /// <summary>
        /// Parses an entire CSV string into rows of string arrays.
        /// </summary>
        public static List<string[]> ParseCSV(string csv, bool skipHeader = true)
        {
            var result = new List<string[]>();
            var lines = csv.Split('\n');
            int start = skipHeader ? 1 : 0;

            for (int i = start; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                result.Add(ParseLine(line));
            }

            return result;
        }

        public static string GetString(string[] values, int index, string defaultValue = "")
        {
            if (index < values.Length && !string.IsNullOrEmpty(values[index]))
                return values[index];
            return defaultValue;
        }

        public static int GetInt(string[] values, int index, int defaultValue = 0)
        {
            if (index < values.Length && int.TryParse(values[index], out int result))
                return result;
            return defaultValue;
        }

        public static float GetFloat(string[] values, int index, float defaultValue = 0f)
        {
            if (index < values.Length && float.TryParse(values[index], out float result))
                return result;
            return defaultValue;
        }

        public static bool GetBool(string[] values, int index, bool defaultValue = false)
        {
            if (index >= values.Length) return defaultValue;
            var val = values[index].Trim().ToUpperInvariant();
            return val == "TRUE" || val == "YES" || val == "1";
        }
    }
}
