using System.Collections.Generic;
using Sorolla;

namespace Sorolla.Lives.Tests.Helpers
{
    public sealed class FakeRemoteConfigProvider : IRemoteConfigProvider
    {
        public readonly Dictionary<string, int> Ints = new Dictionary<string, int>();
        public readonly Dictionary<string, long> Longs = new Dictionary<string, long>();
        public readonly Dictionary<string, float> Floats = new Dictionary<string, float>();
        public readonly Dictionary<string, bool> Bools = new Dictionary<string, bool>();
        public readonly Dictionary<string, string> Strings = new Dictionary<string, string>();

        public int GetInt(string k, int v) => Ints.TryGetValue(k, out var x) ? x : v;
        public long GetLong(string k, long v) => Longs.TryGetValue(k, out var x) ? x : v;
        public float GetFloat(string k, float v) => Floats.TryGetValue(k, out var x) ? x : v;
        public bool GetBool(string k, bool v) => Bools.TryGetValue(k, out var x) ? x : v;
        public string GetString(string k, string v) => Strings.TryGetValue(k, out var x) ? x : v;
    }
}
