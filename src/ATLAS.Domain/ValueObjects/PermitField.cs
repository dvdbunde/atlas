using System;
using ATLAS.Domain.Enums;

namespace ATLAS.Domain.ValueObjects
{
    /// <summary>
    /// PermitField is a Value Object (not an Entity).
    /// 
    /// VALUE OBJECT CHARACTERISTICS:
    /// 1. No identity: PermitField has no Id field
    /// 2. Immutable: Once created, field properties should not change
    /// 3. Equality by value: Two PermitFields are equal if all properties match
    /// 
    /// REFERENCE STRATEGY (Phase A - Milestone 5):
    /// - ApplicationFieldValue references PermitField using FieldName (not PermitFieldId)
    /// - FieldName is treated as immutable
    /// - This is documented in ADR-014 (to be created in Phase F+G)
    /// </summary>
    public class PermitField
    {
        public string Name { get; private set; }
        public FieldType Type { get; private set; }
        public bool IsRequired { get; private set; }
        public string DefaultValue { get; private set; }

        public PermitField(string name, FieldType type, bool isRequired, string defaultValue = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Field name cannot be empty", nameof(name));
            
            if (name.Length < 2 || name.Length > 100)
                throw new ArgumentException("Field name must be between 2 and 100 characters", nameof(name));

            Name = name;
            Type = type;
            IsRequired = isRequired;
            DefaultValue = defaultValue;
        }        

        // Value objects use value equality
        public override bool Equals(object obj)
        {
            if (obj is not PermitField other)
                return false;
            return Name == other.Name && Type == other.Type && IsRequired == other.IsRequired && DefaultValue == other.DefaultValue;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Type, IsRequired, DefaultValue);
        }
    }   
}
