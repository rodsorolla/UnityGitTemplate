using System;
using System.Collections.Generic;
using Sorolla.PersistentData;

namespace Sorolla.Purchasing
{
    [Serializable]
    public class EntitlementsSaveData : ISaveData
    {
        public int Version => 1;
        public List<string> Entitlements = new();
    }
}
