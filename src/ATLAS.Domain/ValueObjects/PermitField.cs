using System;
using System.Collections.Generic;
using System.Linq;
using ATLAS.Domain.Enums;

namespace ATLAS.Domain.ValueObjects
{
    public class PermitField
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public FieldType Type { get; private set; }
        public bool IsRequired { get; private set; }
        public string DefaultValue { get; private set; }
        public int Order { get; internal set; }
        private List<string> _options = new();
        public IReadOnlyCollection<string> Options => _options.AsReadOnly();

        // EF Core materialization constructor — does NOT validate (data already in DB)
        private PermitField()
        {
        }

        public PermitField(
            string name,
            FieldType type,
            bool isRequired,
            string defaultValue = null,
            IReadOnlyCollection<string> options = null)
            : this(Guid.NewGuid(), name, type, isRequired, defaultValue, options)
        {
        }

        // Aggregate-internal constructor: assigns a stable Id and lets the
        // aggregate assign Order after insertion.
        internal PermitField(
            Guid id,
            string name,
            FieldType type,
            bool isRequired,
            string defaultValue = null,
            IReadOnlyCollection<string> options = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Field name cannot be empty", nameof(name));
            
            if (name.Length < 2 || name.Length > 100)
                throw new ArgumentException("Field name must be between 2 and 100 characters", nameof(name));

            // Dropdown validation
            if (type == FieldType.Dropdown)
            {
                if (options == null || options.Count == 0)
                    throw new ArgumentException("Dropdown field must have at least one option", nameof(options));

                // Validate DefaultValue is within Options when specified
                if (!string.IsNullOrEmpty(defaultValue) && !options.Contains(defaultValue))
                    throw new ArgumentException(
                        $"DefaultValue '{defaultValue}' must be one of the defined options: {string.Join(", ", options)}",
                        nameof(defaultValue));
                
                _options = (options as List<string>) ?? options?.ToList() ?? new List<string>();                        
            }

            Id = id;
            Name = name;
            Type = type;
            IsRequired = isRequired;
            DefaultValue = defaultValue;            
        }

        // Aggregate-internal mutation: re-validates invariants on update.
        internal void Update(string name, FieldType type, bool isRequired, string defaultValue, IReadOnlyCollection<string> options)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Field name cannot be empty", nameof(name));

            if (name.Length < 2 || name.Length > 100)
                throw new ArgumentException("Field name must be between 2 and 100 characters", nameof(name));

            if (type == FieldType.Dropdown)
            {
                if (options == null || options.Count == 0)
                    throw new ArgumentException("Dropdown field must have at least one option", nameof(options));

                if (!string.IsNullOrEmpty(defaultValue) && !options.Contains(defaultValue))
                    throw new ArgumentException(
                        $"DefaultValue '{defaultValue}' must be one of the defined options: {string.Join(", ", options)}",
                        nameof(defaultValue));

                _options = (options as List<string>) ?? options?.ToList() ?? new List<string>();
            }
            else
            {
                _options.Clear();
            }

            Name = name;
            Type = type;
            IsRequired = isRequired;
            DefaultValue = defaultValue;
        }

        public override bool Equals(object obj)
        {
            if (obj is not PermitField other)
                return false;
            return Name == other.Name
                && Type == other.Type
                && IsRequired == other.IsRequired
                && DefaultValue == other.DefaultValue
                && Options.SequenceEqual(other.Options);
        }

        public override int GetHashCode()
        {
            var hash = HashCode.Combine(Name, Type, IsRequired, DefaultValue);
            foreach (var option in Options)
                hash = HashCode.Combine(hash, option);
            return hash;
        }
    }   
}