using System;
using System.Collections.Generic;
using ATLAS.Domain.Aggregates;
using ATLAS.Domain.Entities;
using ATLAS.Domain.ValueObjects;
using Xunit;

namespace ATLAS.Domain.Tests.Aggregates
{
    public class ApplicationAggregateTests
    {
        private readonly Guid _citizenId = Guid.NewGuid();
        private readonly Guid _permitTypeId = Guid.NewGuid();
        private readonly Guid _officerId = Guid.NewGuid();

        [Fact]
        public void Constructor_ShouldInitializeWithApplication()
        {
            // Arrange & Act
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            var aggregate = new ApplicationAggregate(application);

            // Assert
            Assert.Equal(application, aggregate.Application);
            Assert.Empty(aggregate.Documents);
            Assert.Empty(aggregate.Reviews);
        }

        [Fact]
        public void Constructor_ShouldThrowException_WhenApplicationIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new ApplicationAggregate(null!));
            Assert.Contains("application", exception.Message);
        }

        [Fact]
        public void AddDocument_ShouldAddDocument_WhenStatusIsDraft()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            var aggregate = new ApplicationAggregate(application);
            var document = application.AddDocument(Guid.NewGuid(), "test.pdf", "application/pdf", 1024, "https://blob.url", _citizenId);

            // Act
            aggregate.AddDocument(document);

            // Assert
            Assert.Single(aggregate.Documents);
            Assert.Equal(document.Id, aggregate.Documents[0].Id);
        }

        [Fact]
        public void AddDocument_ShouldThrowException_WhenApplicationApproved()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.Submit();
            application.StartReview(_officerId);
            application.Approve(_officerId, "Approved");
            
            var aggregate = new ApplicationAggregate(application);
            var document = application.AddDocument(Guid.NewGuid(), "test.pdf", "application/pdf", 1024, "https://blob.url", _citizenId);

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => 
                aggregate.AddDocument(document));
            Assert.Contains("approved or rejected", exception.Message);
        }

        [Fact]
        public void AddReview_ShouldAddReview_WhenStatusIsUnderReview()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.Submit();
            application.StartReview(_officerId);
            
            var aggregate = new ApplicationAggregate(application);
            var review = application.AddReview(Guid.NewGuid(), _officerId, ReviewDecision.Approve, "Approved", true);

            // Act
            aggregate.AddReview(review);

            // Assert
            Assert.Single(aggregate.Reviews);
            Assert.Equal(review.Id, aggregate.Reviews[0].Id);
        }

        [Fact]
        public void AddReview_ShouldThrowException_WhenNotUnderReview()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            var aggregate = new ApplicationAggregate(application);
            var review = application.AddReview(Guid.NewGuid(), _officerId, ReviewDecision.Approve, "Approved", true);

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => 
                aggregate.AddReview(review));
            Assert.Contains("UnderReview", exception.Message);
        }

        [Fact]
        public void ValidateInvariants_ShouldPass_WhenValidState()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            var aggregate = new ApplicationAggregate(application);

            // Act & Assert (should not throw)
            aggregate.ValidateInvariants();
        }

        [Fact]
        public void ValidateInvariants_ShouldThrowException_WhenRejectionWithoutReasonCode()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.Submit();
            application.StartReview(_officerId);
            
            var aggregate = new ApplicationAggregate(application);
            var review = application.AddReview(Guid.NewGuid(), _officerId, ReviewDecision.Reject, "Rejected", true);
            aggregate.AddReview(review);

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => 
                aggregate.ValidateInvariants());
            Assert.Contains("ReasonCode", exception.Message);
        }
    }
}
