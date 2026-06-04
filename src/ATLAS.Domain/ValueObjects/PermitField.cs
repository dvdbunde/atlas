using System;

namespace ATLAS.Domain.ValueObjects
{
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

    public enum FieldType
    {
        Text = 1,
        Number = 2,
        Date = 3,
        Dropdown = 4,
        MultiSelect = 5,
        Checkbox = 6,
        TextArea = 7
    }
}
