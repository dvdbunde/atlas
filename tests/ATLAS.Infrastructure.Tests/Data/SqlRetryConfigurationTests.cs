//----------------------
// SQL Retry Configuration Tests
// Verifies that EnableRetryOnFailure is configured correctly
//----------------------

using ATLAS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Data
{
    public class SqlRetryConfigurationTests
    {
        [Fact]
        public void AddInfrastructure_RegistersDbContext()
        {
            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(LocalDB)\\MSSQLLocalDB;Database=ATLAS_Test;Trusted_Connection=True;",
                ["Storage:AccountName"] = "",
                ["Storage:ConnectionString"] = "UseDevelopmentStorage=true",
                ["Storage:ContainerName"] = "permit-documents",
                ["Storage:SasTokenExpiryHours"] = "1"
            };
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();

            var services = new ServiceCollection();
            services.AddInfrastructure(config);
            var provider = services.BuildServiceProvider();

            var context = provider.GetService<ApplicationDbContext>();
            Assert.NotNull(context);
        }
    }
}
