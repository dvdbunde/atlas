using System.ComponentModel.DataAnnotations;
using ATLAS.Infrastructure.Options;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Options
{
    public class StorageOptionsValidationTests
    {
        [Fact]
        public void ConnectionString_ShouldBeRequired()
        {
            var options = new StorageOptions
            {
                ConnectionString = "",
                ContainerName = "permit-documents",
                SasTokenExpiryHours = 1
            };

            var results = ValidateModel(options);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(StorageOptions.ConnectionString)));
        }

        [Fact]
        public void ContainerName_ShouldBeRequired()
        {
            var options = new StorageOptions
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "",
                SasTokenExpiryHours = 1
            };

            var results = ValidateModel(options);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(StorageOptions.ContainerName)));
        }

        [Fact]
        public void SasTokenExpiryHours_ShouldBeInRange()
        {
            var options = new StorageOptions
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "permit-documents",
                SasTokenExpiryHours = 0
            };

            var results = ValidateModel(options);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(StorageOptions.SasTokenExpiryHours)));
        }

        [Fact]
        public void ValidOptions_ShouldPassValidation()
        {
            var options = new StorageOptions
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "permit-documents",
                SasTokenExpiryHours = 1
            };

            var results = ValidateModel(options);
            Assert.Empty(results);
        }

        private static System.Collections.Generic.List<ValidationResult> ValidateModel(object model)
        {
            var results = new System.Collections.Generic.List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            return results;
        }
    }
}