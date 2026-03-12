using UnityEngine;

namespace Sorolla.GoogleSheets
{
    /// <summary>
    /// Base configuration for Google Sheets importers.
    /// Game-specific configs extend this and add their own sheet URL fields.
    /// </summary>
    public abstract class GoogleSheetsConfigBase : ScriptableObject
    {
        [Header("Output Settings")]
        [Tooltip("Folder where imported assets will be created/updated")]
        public string OutputFolder = "Assets/_Game/Data";
    }
}
