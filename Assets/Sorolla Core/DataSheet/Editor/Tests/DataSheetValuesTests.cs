using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Sorolla.DataSheet.Editor;

namespace Sorolla.DataSheet.EditorTests
{
    public class DataSheetValuesTests
    {
        public enum Kind { Alpha, Beta, Gamma }

        public class Probe : ScriptableObject
        {
            public int hp = 5;
            public float speed = 1.5f;
            public bool flag = true;
            public string label = "hello";
            public Kind kind = Kind.Beta;
            public Color tint = Color.red;
            public Sprite icon;        // object reference -> non-scalar
            public int[] arr = { 1, 2 }; // array -> non-scalar
        }

        Probe _probe;
        SerializedObject _so;

        [SetUp]
        public void SetUp()
        {
            _probe = ScriptableObject.CreateInstance<Probe>();
            _so = new SerializedObject(_probe);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_probe);
        }

        SerializedProperty P(string name) => _so.FindProperty(name);

        [Test]
        public void IsScalar_TrueForScalars_FalseForRefAndArray()
        {
            Assert.IsTrue(DataSheetValues.IsScalar(P("hp")));
            Assert.IsTrue(DataSheetValues.IsScalar(P("speed")));
            Assert.IsTrue(DataSheetValues.IsScalar(P("flag")));
            Assert.IsTrue(DataSheetValues.IsScalar(P("label")));
            Assert.IsTrue(DataSheetValues.IsScalar(P("kind")));
            Assert.IsTrue(DataSheetValues.IsScalar(P("tint")));
            Assert.IsFalse(DataSheetValues.IsScalar(P("icon")));
            Assert.IsFalse(DataSheetValues.IsScalar(P("arr")));
        }

        [Test]
        public void IsComplex_TrueForArray_FalseForScalarAndObjectRef()
        {
            Assert.IsTrue(DataSheetValues.IsComplex(P("arr")));    // array/list
            Assert.IsFalse(DataSheetValues.IsComplex(P("icon")));  // object reference
            Assert.IsFalse(DataSheetValues.IsComplex(P("hp")));    // scalar int
            Assert.IsFalse(DataSheetValues.IsComplex(P("kind")));  // scalar enum
        }

        [Test]
        public void ReadScalar_ReturnsExpectedStrings()
        {
            Assert.AreEqual("5", DataSheetValues.ReadScalar(P("hp")));
            Assert.AreEqual("1.5", DataSheetValues.ReadScalar(P("speed")));
            Assert.AreEqual("true", DataSheetValues.ReadScalar(P("flag")));
            Assert.AreEqual("hello", DataSheetValues.ReadScalar(P("label")));
            Assert.AreEqual("Beta", DataSheetValues.ReadScalar(P("kind")));
            Assert.AreEqual("#FF0000FF", DataSheetValues.ReadScalar(P("tint")));
        }

        [Test]
        public void WriteScalar_RoundTripsThroughApply()
        {
            Assert.IsTrue(DataSheetValues.WriteScalar(P("hp"), "42"));
            Assert.IsTrue(DataSheetValues.WriteScalar(P("speed"), "3.25"));
            Assert.IsTrue(DataSheetValues.WriteScalar(P("flag"), "false"));
            Assert.IsTrue(DataSheetValues.WriteScalar(P("label"), "world"));
            Assert.IsTrue(DataSheetValues.WriteScalar(P("kind"), "Gamma"));
            Assert.IsTrue(DataSheetValues.WriteScalar(P("tint"), "#00FF00FF"));
            _so.ApplyModifiedProperties();

            Assert.AreEqual(42, _probe.hp);
            Assert.AreEqual(3.25f, _probe.speed);
            Assert.IsFalse(_probe.flag);
            Assert.AreEqual("world", _probe.label);
            Assert.AreEqual(Kind.Gamma, _probe.kind);
            Assert.AreEqual(Color.green, _probe.tint);
        }

        [Test]
        public void WriteScalar_ReturnsFalse_OnUnparseableOrUnsupported()
        {
            Assert.IsFalse(DataSheetValues.WriteScalar(P("hp"), "not-a-number"));
            Assert.IsFalse(DataSheetValues.WriteScalar(P("kind"), "NoSuchEnum"));
            Assert.IsFalse(DataSheetValues.WriteScalar(P("icon"), "anything"));
            Assert.IsFalse(DataSheetValues.WriteScalar(P("arr"), "anything"));
        }
    }
}
