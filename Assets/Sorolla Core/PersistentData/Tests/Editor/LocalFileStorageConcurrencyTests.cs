using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Sorolla.PersistentData.Tests
{
    public class LocalFileStorageConcurrencyTests
    {
        string _dir;
        LocalFileStorage _storage;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "lfs_test_" + System.Guid.NewGuid().ToString("N"));
            _storage = new LocalFileStorage(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        // Fires many concurrent SaveAsync at one file. With the old shared "<file>.json.tmp"
        // these collided ("Sharing violation" / "file not found") and corrupted/lost data;
        // unique temps + a serialized rename make it safe.
        [UnityTest]
        public IEnumerator ConcurrentSaves_SameFile_NoCorruptionOrError() => UniTask.ToCoroutine(async () =>
        {
            const string file = "concurrent";
            const int n = 24;

            var tasks = new List<UniTask>(n);
            for (int i = 0; i < n; i++)
            {
                string payload = i.ToString();
                tasks.Add(_storage.SaveAsync(payload, file));
            }
            await UniTask.WhenAll(tasks);

            // Final file is exactly one complete written payload — not partial/missing.
            var loaded = _storage.Load(file);
            Assert.IsNotNull(loaded);
            Assert.IsTrue(int.TryParse(loaded, out var v));
            Assert.GreaterOrEqual(v, 0);
            Assert.Less(v, n);

            // No temp files leaked as saves; exactly the one target remains.
            var files = _storage.GetAllSaveFiles();
            Assert.AreEqual(1, files.Length);
            Assert.AreEqual(file, files[0]);
        });
    }
}
