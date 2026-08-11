
// Exposes internal types/members to the test projects so unit tests can supply a
// custom default-template path and verify behavior without a live Azure dependency.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ATLAS.Infrastructure.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ATLAS.IntegrationTests")]
