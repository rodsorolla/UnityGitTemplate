namespace Sorolla.PersistentData
{
    /// <summary>
    /// Interface for data migrators that upgrade save data from one version to another.
    /// </summary>
    public interface IMigrator
    {
        /// <summary>
        /// The type name this migrator handles.
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// The version this migrator upgrades from.
        /// </summary>
        int FromVersion { get; }

        /// <summary>
        /// The version this migrator upgrades to.
        /// </summary>
        int ToVersion { get; }

        /// <summary>
        /// Migrates JSON data from FromVersion to ToVersion.
        /// </summary>
        /// <param name="json">The JSON string at FromVersion</param>
        /// <returns>The migrated JSON string at ToVersion</returns>
        string Migrate(string json);
    }
}
