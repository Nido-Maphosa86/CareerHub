using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CareerHub.Api.Tests.Integration;

// Starts the real CareerHub application once for all tests in a class.
// IClassFixture<T> is the xUnit mechanism for shared, expensive setup —
// starting the app is slow, so we do it once and reuse it for every test.
//
// IMPORTANT: This only works if Program.cs has this line at the very bottom
// (after app.Run()):
//
//     public partial class Program { }
//
// Without that line, the test project cannot see the Program class and
// this file will not compile.

public class WebApplicationFactoryFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Integration tests run against the real configured database.
            // If we ever need to swap services for testing (like pointing
            // the DbContext at a TestContainers database), we do it here.
        });
    }
}
