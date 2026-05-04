using System;

namespace Sorolla.GoogleSheets
{
    /// <summary>
    /// Marks a field on a data ScriptableObject as mirror-able to a Google Sheet column.
    /// Opt-in: fields without this attribute are not read or written by the sync tool.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class SheetColumnAttribute : Attribute
    {
        public string Name { get; }

        public SheetColumnAttribute(string name)
        {
            Name = name;
        }
    }
}
