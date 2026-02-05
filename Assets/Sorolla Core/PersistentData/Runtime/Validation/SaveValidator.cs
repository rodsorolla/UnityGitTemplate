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
        /// Extracts the Version property from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string</param>
        /// <returns>The version number, or -1 if not found or invalid</returns>
        public static int GetVersion(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return -1;

            try
            {
                var obj = JObject.Parse(json);

                // Try common version property names
                var versionToken = obj["Version"] ?? obj["version"] ?? obj["_version"];

                if (versionToken != null && versionToken.Type == JTokenType.Integer)
                    return versionToken.Value<int>();

                return -1;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Failed to extract version: {ex.Message}");
                return -1;
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

        /// <summary>
        /// Sets a property value in a JSON string.
        /// </summary>
        /// <param name="json">The original JSON string</param>
        /// <param name="propertyName">The property to set</param>
        /// <param name="value">The new value</param>
        /// <returns>The modified JSON string</returns>
        public static string SetProperty(string json, string propertyName, object value)
        {
            try
            {
                var obj = JObject.Parse(json);
                obj[propertyName] = JToken.FromObject(value);
                return obj.ToString(Formatting.Indented);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to set property {propertyName}: {ex.Message}");
                return json;
            }
        }
    }
}
