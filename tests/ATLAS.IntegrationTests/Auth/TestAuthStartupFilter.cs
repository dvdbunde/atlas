using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace ATLAS.IntegrationTests.Auth;

public class TestAuthStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<TestAuthMiddleware>();
            next(app);
        };
    }
}
