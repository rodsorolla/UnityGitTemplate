using System;
using System.Collections.Generic;

namespace Sorolla.PersistentData
{
    /// <summary>
    /// Generic key-value save container for callers that need a string-keyed dictionary
    /// rather than a typed schema. Used by adapters that bridge legacy key/value APIs
    /// (e.g. <see cref="IPersistenceService"/>) onto <see cref="SaveSystem"/>.
    /// </summary>
    [Serializable]
    public class KVSaveData : ISaveData
    {
        public int Version => 1;
        public Dictionary<string, int> Ints = new();
        public Dictionary<string, string> Strings = new();
    }
}
