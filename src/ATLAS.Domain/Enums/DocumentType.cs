using System;

namespace ATLAS.Domain.Enums
{
    [Obsolete("DocumentType enum is deprecated. File type validation should use MIME type checks and DocumentRequirement value object instead. This enum will be removed in a future version.")]
    public enum DocumentType
    {
        PDF = 1,
        JPG = 2,
        PNG = 3
    }
}