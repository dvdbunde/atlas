using System;

namespace ATLAS.Domain.Entities
{
    /// <summary>
    /// Represents a field value for a dynamic permit application form.
    /// This is a child entity within the Application aggregate.
    /// 
    /// DOMAIN INVARIANTS (Phase A - Milestone 5):
    /// 1. FieldValue ownership: FieldValues are owned by Application aggregate (OwnsMany)
    /// 2. No separate repository: FieldValues are persisted through Application aggregate
    /// 3. Aggregate boundaries: FieldValues cannot exist without an Application
    /// 4. PermitField reference strategy: FieldName references PermitField.Name (not PermitFieldId)
    ///    - FieldName is treated as immutable
    ///    - No DocumentId or PermitFieldId fields (as per approved design)
    /// </summary>
    public class ApplicationFieldValue : Entity<Guid>
    {
        /// <summary>
        /// Gets the ID of the parent Application aggregate.
        /// </summary>
        public Guid ApplicationId { get; private set; }

        /// <summary>
        /// Gets the name of the field (references PermitField.Name).
        /// Must be unique within the parent PermitType.
        /// </summary>
        public string FieldName { get; private set; }

        /// <summary>
        /// Gets the value entered by the citizen.
        /// Null values are converted to string.Empty on construction and update.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Gets the sort order for displaying fields in the form.
        /// Lower values appear first. Must be non-negative.
        /// </summary>
        public int SortOrder { get; private set; }

        /// <summary>
        /// Private parameterless constructor for EF Core.
        /// </summary>
        private ApplicationFieldValue()
        {
            // EF Core requires a parameterless constructor
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationFieldValue"/> class.
        /// </summary>
        /// <param name="applicationId">The ID of the parent application.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The value entered by the citizen (can be null).</param>
        /// <param name="sortOrder">The sort order for display (must be non-negative).</param>
        /// <exception cref="ArgumentException">Thrown when applicationId is empty or fieldName is invalid.</exception>
        internal ApplicationFieldValue(
            Guid applicationId,
            string fieldName,
            string value,
            int sortOrder)
            : base(Guid.NewGuid())
        {
            if (applicationId == Guid.Empty)
                throw new ArgumentException("Application ID cannot be empty", nameof(applicationId));

            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Field name cannot be null, empty, or whitespace", nameof(fieldName));

            if (fieldName.Length > 100)
                throw new ArgumentException("Field name cannot exceed 100 characters", nameof(fieldName));

            if (sortOrder < 0)
                throw new ArgumentException("Sort order must be non-negative", nameof(sortOrder));

            ApplicationId = applicationId;
            FieldName = fieldName;
            Value = value ?? string.Empty;
            SortOrder = sortOrder;
        }

        /// <summary>
        /// Updates the value of this field.
        /// </summary>
        /// <param name="newValue">The new value (null becomes string.Empty).</param>
        public void UpdateValue(string newValue)
        {
            Value = newValue ?? string.Empty;
        }
    }
}
