using System;
using System.Collections.Generic;
using Sorolla.PersistentData;

namespace Sorolla.Events
{
    /// <summary>
    /// Root persisted state for the events module. Stored as JSON via
    /// Sorolla.PersistentData.SaveSystem under file name "events".
    /// </summary>
    [Serializable]
    public sealed class EventsSaveData : ISaveData
    {
        
        public const int CurrentVersion = 1;
        public int Version => CurrentVersion;

        /// <summary>UTC ISO of last observed wall-clock. Used for rollback detection.</summary>
        public string lastSeenUtcIso;

        public List<EventInstanceProgress> instances = new List<EventInstanceProgress>();

        public EventInstanceProgress FindOrCreate(string eventId, string firstSeenUtcIso)
        {
            for (int i = 0; i < instances.Count; i++)
                if (instances[i].eventId == eventId) return instances[i];
            var fresh = new EventInstanceProgress
            {
                eventId = eventId,
                progress = 0,
                claimedStepBitset = 0,
                grandPrizeClaimed = false,
                firstSeenUtcIso = firstSeenUtcIso,
            };
            instances.Add(fresh);
            return fresh;
        }

        public EventInstanceProgress Find(string eventId)
        {
            for (int i = 0; i < instances.Count; i++)
                if (instances[i].eventId == eventId) return instances[i];
            return null;
        }

        public bool Remove(string eventId)
        {
            for (int i = 0; i < instances.Count; i++)
                if (instances[i].eventId == eventId) { instances.RemoveAt(i); return true; }
            return false;
        }
    }
}
