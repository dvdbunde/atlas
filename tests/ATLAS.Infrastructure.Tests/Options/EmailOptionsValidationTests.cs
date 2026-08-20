using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ATLAS.Infrastructure.Options;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Options
{
    public class EmailOptionsValidationTests
    {
        [Fact]
        public void EmptyAcs_ShouldPassValidation_ForLocalDevelopment()
        {
            // Local development intentionally has no ACS configuration.
            var options = new EmailOptions { Acs = new AcsEmailOptions() };

            var results = ValidateModel(options);
            Assert.Empty(results);
        }

        [Fact]
        public void ValidAcs_ShouldPassValidation()
        {
            var options = new EmailOptions
            {
                Acs = new AcsEmailOptions
                {
                    Endpoint = "https://atlas-comm-test.communication.azure.com",
                    SenderAddress = "DoNotReply@atlas.com"
                }
            };

            var results = ValidateModel(options);
            Assert.Empty(results);
        }

        [Fact]
        public void MissingSenderAddress_ShouldFailValidation()
        {
            var options = new EmailOptions
            {
                Acs = new AcsEmailOptions
                {
                    Endpoint = "https://atlas-comm-test.communication.azure.com",
                    SenderAddress = ""
                }
            };

            var results = ValidateModel(options);
            Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("SenderAddress"));
        }

        [Fact]
        public void MissingEndpoint_ShouldFailValidation()
        {
            var options = new EmailOptions
            {
                Acs = new AcsEmailOptions
                {
                    Endpoint = "",
                    SenderAddress = "DoNotReply@atlas.com"
                }
            };

            var results = ValidateModel(options);
            Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Endpoint"));
        }

        [Fact]
        public void InvalidEndpoint_ShouldFailValidation()
        {
            var options = new EmailOptions
            {
                Acs = new AcsEmailOptions
                {
                    Endpoint = "not-a-valid-uri",
                    SenderAddress = "DoNotReply@atlas.com"
                }
            };

            var results = ValidateModel(options);
            Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Endpoint"));
        }

        private static List<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            return results;
        }
    }
}
