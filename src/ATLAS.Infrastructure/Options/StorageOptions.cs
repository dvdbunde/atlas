//----------------------
// Storage Options
// Strongly-typed configuration for Azure Blob Storage
//----------------------

namespace ATLAS.Infrastructure.Options
{
    /// <summary>
    /// Configuration options for Azure Blob Storage.
    /// Bound from the "Storage" configuration section.
    /// </summary>
    public class StorageOptions
    {
        /// <summary>
        /// The configuration section name.
        /// </summary>
        public const string SectionName = "Storage";

        /// <summary>
        /// Azure Blob Storage connection string.
        /// Use "UseDevelopmentStorage=true" for Azurite local development.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Name of the blob container for document storage.
        /// Default: "permit-documents" per ADR-015.
        /// </summary>
        public string ContainerName { get; set; } = "permit-documents";

        /// <summary>
        /// Number of hours for SAS token expiry.
        /// Default: 1 hour per ADR-015.
        /// </summary>
        public int SasTokenExpiryHours { get; set; } = 1;
    }
}
