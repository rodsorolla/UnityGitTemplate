using System;
using System.Collections.Generic;
using Sorolla.PersistentData;

namespace Sorolla.Tutorial
{
    /// <summary>
    /// Persistent data for the tutorial system. Stored via <see cref="SaveSystem"/>
    /// under file name <c>tutorial</c>. Tracks which content-level indices have had
    /// their tutorial completed so subsequent runs can skip them.
    /// </summary>
    [Serializable]
    public class TutorialSaveData : ISaveData
    {
        public int Version => 1;
        public List<int> CompletedLevels = new();
    }
}
