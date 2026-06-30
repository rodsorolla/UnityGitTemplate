using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Sorolla.DataSheet.Editor;

namespace Sorolla.DataSheet.EditorTests
{
    public class DataSheetModelTests
    {
        public class Probe : ScriptableObject
        {
            public int hp;
            public float speed;
            public string label;
        }

        [Test]
        public void BuildColumns_ReturnsTopLevelFields_WithoutScript()
        {
            List<string> cols = DataSheetModel.BuildColumns(typeof(Probe));

            CollectionAssert.DoesNotContain(cols, "m_Script");
            CollectionAssert.Contains(cols, "hp");
            CollectionAssert.Contains(cols, "speed");
            CollectionAssert.Contains(cols, "label");
        }
    }
}
