using System.Reflection;
using ATLAS.Domain.Entities;
using ATLAS.Application.Commands;
using Xunit;

namespace ATLAS.Application.Tests.Architecture
{
    /// <summary>
    /// Architecture guardrail tests protecting ADR-013 (Entra ID as sole source of truth).
    /// These tests fail the build if a future milestone accidentally reintroduces
    /// local identity management inside ATLAS.
    /// </summary>
    public class Adr013UserProjectionGuardTests
    {
        private static readonly Assembly DomainAssembly = typeof(User).Assembly;
        private static readonly Assembly ApplicationAssembly = typeof(ICommand<>).Assembly;

        // Forbidden identity-management method/property names on the User aggregate.
        private static readonly HashSet<string> ForbiddenUserMembers = new()
        {
            "IsActive", "ChangeRole", "Deactivate", "Activate", "UpdateEmail", "UpdateProfile"
        };

        // Forbidden command class name fragments implying local identity management.
        private static readonly HashSet<string> ForbiddenCommandFragments = new()
        {
            "ChangeRole", "ActivateUser", "DeactivateUser", "UpdateUser", "EditUser",
            "CreateUser", "UpdateUserRole", "RegisterUser", "ResetUserPassword"
        };

        [Fact]
        public void Adr013_UserAggregate_ExposesNoIdentityManagementBehavior()
        {
            var userType = typeof(User);
            var offending = userType.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => ForbiddenUserMembers.Contains(m.Name))
                .Select(m => m.Name)
                .ToList();

            Assert.Empty(offending);
        }

        [Fact]
        public void Adr013_UserAggregate_HasNoIsActiveProperty()
        {
            var isActive = typeof(User).GetProperty("IsActive", BindingFlags.Public | BindingFlags.Instance);
            Assert.Null(isActive);
        }

        [Fact]
        public void Adr013_NoUserMutationCommandClassesExist()
        {
            var offending = ApplicationAssembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => typeof(ICommand<>).IsAssignableFrom(t) ||
                            (t.Name.EndsWith("Command") && t.GetInterfaces().Any(i => i.Name == "IRequest`1")))
                .Where(t => ForbiddenCommandFragments.Any(f => t.Name.Contains(f, StringComparison.OrdinalIgnoreCase)))
                .Select(t => t.FullName)
                .ToList();

            Assert.Empty(offending);
        }

        [Fact]
        public void Adr013_UserRepositoryUpdateAsync_OnlyReferencedByIdentityResolver()
        {
            var srcRoot = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src"));

            if (!Directory.Exists(srcRoot))
            {
                srcRoot = Path.GetFullPath(
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src"));
            }

            Assert.True(Directory.Exists(srcRoot), $"Could not locate src directory from {AppContext.BaseDirectory}");

            var files = Directory.GetFiles(srcRoot, "*.cs", SearchOption.AllDirectories);
            var offending = new List<string>();

            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                // Only flag User-projection writes, not the generic IRepository<T>.UpdateAsync
                // used legitimately by Application/PermitType/Document/AuditLog repositories.
                var referencesUserUpdate =
                    text.Contains("IUserRepository", StringComparison.OrdinalIgnoreCase) &&
                    text.Contains("UpdateAsync", StringComparison.OrdinalIgnoreCase);

                if (!referencesUserUpdate)
                    continue;

                var isIdentityResolver = file.Replace('\\', '/').Contains("/Services/IdentityResolver.cs");
                var fileName = Path.GetFileName(file);
                var isRepositoryInterfaceOrImpl = fileName.Equals("IUserRepository.cs", StringComparison.OrdinalIgnoreCase)
                                                 || fileName.Equals("UserRepository.cs", StringComparison.OrdinalIgnoreCase);

                if (isIdentityResolver || isRepositoryInterfaceOrImpl)
                    continue;

                offending.Add(file);
            }

            Assert.Empty(offending);
        }
    }
}
