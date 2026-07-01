using System.ComponentModel.DataAnnotations;

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
        [Required(ErrorMessage = "Storage:ConnectionString is required.")]
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Name of the blob container for document storage.
        /// Default: "permit-documents" per ADR-015.
        /// </summary>
        [Required(ErrorMessage = "Storage:ContainerName is required.")]
        public string ContainerName { get; set; } = "permit-documents";

        /// <summary>
        /// Number of hours for SAS token expiry.
        /// Default: 1 hour per ADR-015.
        /// </summary>
        [Range(1, 24, ErrorMessage = "Storage:SasTokenExpiryHours must be between 1 and 24.")]
        public int SasTokenExpiryHours { get; set; } = 1;
    }
}