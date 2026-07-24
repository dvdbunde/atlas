using System;

namespace ATLAS.Domain.ValueObjects
{
    public class DocumentRequirement
    {
        public Guid Id { get; private set; }
        public string DocumentType { get; private set; }
        public bool IsRequired { get; private set; }
        public string[] AllowedExtensions { get; private set; }
        public long MaxFileSizeBytes { get; private set; }
        public int Order { get; internal set; }

        public DocumentRequirement(string documentType, bool isRequired, string[] allowedExtensions, long maxFileSizeBytes)
            : this(Guid.NewGuid(), documentType, isRequired, allowedExtensions, maxFileSizeBytes)
        {
        }

        // Aggregate-internal constructor: assigns a stable Id and lets the
        // aggregate assign Order after insertion.
        internal DocumentRequirement(Guid id, string documentType, bool isRequired, string[] allowedExtensions, long maxFileSizeBytes)
        {
            if (string.IsNullOrWhiteSpace(documentType))
                throw new ArgumentException("Document type cannot be empty", nameof(documentType));
            
            if (allowedExtensions == null || allowedExtensions.Length == 0)
                throw new ArgumentException("Allowed extensions must be provided", nameof(allowedExtensions));
            
            if (maxFileSizeBytes <= 0)
                throw new ArgumentException("Max file size must be positive", nameof(maxFileSizeBytes));

            Id = id;
            DocumentType = documentType;
            IsRequired = isRequired;
            AllowedExtensions = allowedExtensions;
            MaxFileSizeBytes = maxFileSizeBytes;
        }

        // Aggregate-internal mutation.
        internal void Update(bool isRequired, string[] allowedExtensions, long maxFileSizeBytes)
        {
            IsRequired = isRequired;
            AllowedExtensions = allowedExtensions;
            MaxFileSizeBytes = maxFileSizeBytes;
        }

        // Value objects use value equality - check all properties
        public override bool Equals(object obj)
        {
            if (obj is not DocumentRequirement other)
                return false;
            
            return DocumentType == other.DocumentType && 
                   IsRequired == other.IsRequired &&
                   AllowedExtensions.SequenceEqual(other.AllowedExtensions) &&
                   MaxFileSizeBytes == other.MaxFileSizeBytes;
        }

        public override int GetHashCode()
        {
            var hash = HashCode.Combine(DocumentType, IsRequired, MaxFileSizeBytes);
            
            foreach (var ext in AllowedExtensions)
                hash = HashCode.Combine(hash, ext.GetHashCode());
            
            return hash;
        }
    }
}
