using Microsoft.AspNetCore.Mvc.Testing;

namespace ATLAS.IntegrationTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
}
