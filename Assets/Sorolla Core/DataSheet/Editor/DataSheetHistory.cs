using System.Collections.Generic;

namespace Sorolla.DataSheet.Editor
{
    /// <summary>One recorded cell edit. Values are stored as DataSheetValues scalar strings.</summary>
    public struct ChangeEntry
    {
        public string assetName;
        public string fieldPath;
        public string oldValue;
        public string newValue;
        public string timestamp;
    }

    /// <summary>
    /// In-memory, newest-first log of cell edits made during the window session.
    /// Not persisted — complements Unity's native Undo. Revert is performed by the
    /// window, which writes <see cref="ChangeEntry.oldValue"/> back via DataSheetValues.
    /// </summary>
    public class DataSheetHistory
    {
        readonly List<ChangeEntry> _entries = new List<ChangeEntry>();

        public IReadOnlyList<ChangeEntry> Entries => _entries;

        public void Record(ChangeEntry e) => _entries.Insert(0, e);

        public void RemoveAt(int index)
        {
            if (index >= 0 && index < _entries.Count)
                _entries.RemoveAt(index);
        }

        public void Clear() => _entries.Clear();
    }
}
