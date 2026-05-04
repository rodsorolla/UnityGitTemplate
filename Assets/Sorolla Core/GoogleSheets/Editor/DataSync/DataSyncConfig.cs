using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Sorolla.GoogleSheets
{
    /// <summary>
    /// Editor-only configuration for the Data ↔ Sheet Sync tool.
    /// Stored as an asset so the spreadsheet id and credential path persist across sessions.
    /// </summary>
    // Previously lived in RandomTrain.EditorTools.Sheets. MovedFrom lets existing DataSyncConfig assets
    // keep their m_Script reference after the lift into Sorolla Core.
    [MovedFrom(true, "RandomTrain.EditorTools.Sheets", null, null)]
    [CreateAssetMenu(fileName = "DataSyncConfig", menuName = "Sorolla/Google Sheets/Data Sync Config")]
    public class DataSyncConfig : ScriptableObject
    {
        [Tooltip("Google Sheets spreadsheet id — the long token in the sheet URL between /d/ and /edit.")]
        public string SpreadsheetId;

        [Tooltip("Path to the service-account JSON key, relative to project root.")]
        public string CredentialsPath = "Assets/Editor/SheetsCredentials/credentials.json";

        [Tooltip("If true, Pull will delete assets that are missing from the sheet. Off by default — prevents accidents.")]
        public bool AllowDeletionsOnPull = false;

        [Tooltip("Folder where new assets created by Pull are saved.")]
        public string DefaultOutputFolder = "Assets";
    }
}
