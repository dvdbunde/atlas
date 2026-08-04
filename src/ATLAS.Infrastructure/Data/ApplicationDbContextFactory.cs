using ATLAS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ATLAS.Infrastructure.Data
{
    /// <summary>
    /// Design-time factory for ApplicationDbContext
    /// Enables EF Core tools to create the DbContext without a full dependency injection container
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            throw new Exception("ApplicationDbContextFactory was used");
        }
    }
}
