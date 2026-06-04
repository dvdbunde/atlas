using System;

namespace ATLAS.Domain.Entities
{
    public class Document : Entity<Guid>
    {
        public Guid ApplicationId { get; private set; }
        public string FileName { get; private set; }
        public string ContentType { get; private set; }
        public long FileSize { get; private set; }
        public string BlobUrl { get; private set; }
        public DateTime UploadedDate { get; private set; }
        public Guid UploadedById { get; private set; }

        // Make constructor internal to enforce aggregate boundary
        internal Document(Guid id, Guid applicationId, string fileName, string contentType, long fileSize, string blobUrl, Guid uploadedById)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Document ID cannot be empty", nameof(id));

            if (applicationId == Guid.Empty)
                throw new ArgumentException("Application ID cannot be empty", nameof(applicationId));
        
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));
            
            if (fileName.Length > 255)
                throw new ArgumentException("File name cannot exceed 255 characters", nameof(fileName));
            
            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentException("Content type cannot be empty", nameof(contentType));
            
            if (fileSize <= 0)
                throw new ArgumentException("File size must be positive", nameof(fileSize));
            
            if (fileSize > 25 * 1024 * 1024) // 25MB
                throw new ArgumentException("File size cannot exceed 25MB", nameof(fileSize));
            
            if (string.IsNullOrWhiteSpace(blobUrl))
                throw new ArgumentException("Blob URL cannot be empty", nameof(blobUrl));
            
            if (uploadedById == Guid.Empty)
                throw new ArgumentException("Uploaded by ID cannot be empty", nameof(uploadedById));

            Id = id;
            ApplicationId = applicationId;
            FileName = fileName;
            ContentType = contentType;
            FileSize = fileSize;
            BlobUrl = blobUrl;
            UploadedDate = DateTime.UtcNow;
            UploadedById = uploadedById;
        }

        internal protected Document()
        {
        }
    }
}

