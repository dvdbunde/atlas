using System;

namespace ATLAS.Domain.ValueObjects
{
    public class DocumentRequirement
    {
        public string DocumentType { get; private set; }
        public bool IsRequired { get; private set; }
        public string[] AllowedExtensions { get; private set; }
        public long MaxFileSizeBytes { get; private set; }

        public DocumentRequirement(string documentType, bool isRequired, string[] allowedExtensions, long maxFileSizeBytes)
        {
            if (string.IsNullOrWhiteSpace(documentType))
                throw new ArgumentException("Document type cannot be empty", nameof(documentType));
            
            if (allowedExtensions == null || allowedExtensions.Length == 0)
                throw new ArgumentException("Allowed extensions must be provided", nameof(allowedExtensions));
            
            if (maxFileSizeBytes <= 0)
                throw new ArgumentException("Max file size must be positive", nameof(maxFileSizeBytes));

            DocumentType = documentType;
            IsRequired = isRequired;
            AllowedExtensions = allowedExtensions;
            MaxFileSizeBytes = maxFileSizeBytes;
        }

        // Value objects use value equality
        public override bool Equals(object obj)
        {
            if (obj is not DocumentRequirement other)
                return false;
            return DocumentType == other.DocumentType && IsRequired == other.IsRequired;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DocumentType, IsRequired);
        }
    }
}
