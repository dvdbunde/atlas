using System;
using System.Collections.Generic;

namespace ATLAS.Domain.Entities
{
    public abstract class Entity<T> where T : notnull
    {
        public T Id { get; protected set; }
        public DateTime CreatedDate { get; protected set; }
        public DateTime? ModifiedDate { get; protected set; }

        private readonly List<object> _domainEvents = new();
        public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

        protected Entity()
        {
            CreatedDate = DateTime.UtcNow;
        }

        protected Entity(T id) : this()
        {
            Id = id;
        }

        protected void AddDomainEvent(object domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        public override bool Equals(object obj)
        {
            if (obj is not Entity<T> other)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (GetType() != other.GetType())
                return false;

            if (Id.Equals(default(T)) || other.Id.Equals(default(T)))
                return false;

            return Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(Entity<T> left, Entity<T> right)
        {
            if (left is null && right is null)
                return true;

            if (left is null || right is null)
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(Entity<T> left, Entity<T> right)
        {
            return !(left == right);
        }
    }
}
