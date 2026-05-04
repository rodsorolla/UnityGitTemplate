using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Sorolla.PersistentData
{
    /// <summary>
    /// Validates JSON structure and extracts version information.
    /// </summary>
    public static class SaveValidator
    {
        /// <summary>
        /// Validates that a JSON string is well-formed.
        /// </summary>
        /// <param name="json">The JSON string to validate</param>
        /// <returns>True if the JSON is valid</returns>
        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                JToken.Parse(json);
                return true;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to deserialize JSON to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="json">The JSON string</param>
        /// <param name="result">The deserialized object, or default if failed</param>
        /// <param name="settings">Optional JSON settings</param>
        /// <returns>True if deserialization succeeded</returns>
        public static bool TryDeserialize<T>(string json, out T result, JsonSerializerSettings settings = null)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                result = settings != null
                    ? JsonConvert.DeserializeObject<T>(json, settings)
                    : JsonConvert.DeserializeObject<T>(json);
                return result != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Deserialization failed: {ex.Message}");
                return false;
            }
        }

    }
}
