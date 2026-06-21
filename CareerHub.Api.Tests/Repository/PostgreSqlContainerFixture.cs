using Testcontainers.PostgreSql;

namespace CareerHub.Api.Tests.Repository;

// Starts a real PostgreSQL Docker container once for all tests in a class.
// The container is completely isolated — it has no data from your dev database.
// When the tests finish, the container is destroyed. Nothing is left behind.
//
// Implements IAsyncLifetime so xUnit calls InitializeAsync before any test
// runs and DisposeAsync after the last test completes.
//
// Requirement: Docker Desktop must be running when you run these tests.

public class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")          // the official PostgreSQL 16 image
        .WithDatabase("careerhubtest")     // an isolated test database
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    // Expose the connection string so the DbContext can connect to this container.
    // The port is random — TestContainers picks a free one — so the connection
    // string is only known after the container starts.
    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync();
    public async Task DisposeAsync()    => await _container.DisposeAsync();
}
