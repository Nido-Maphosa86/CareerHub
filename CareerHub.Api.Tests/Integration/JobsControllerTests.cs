using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CareerHub.Api.DTOs;
using CareerHub.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CareerHub.Api.Tests.Integration;

// Integration tests for the Jobs endpoints.
// These tests start the REAL application — real middleware, real routing,
// real auth, real CORS, real rate limiting — and make REAL HTTP requests.
//
// The difference from unit tests:
//   Unit tests:        fake repositories, test one method in isolation
//   Integration tests: real pipeline, test that everything is wired together
//
// Pattern followed: Arrange → Act → Assert.

public class JobsControllerTests(WebApplicationFactoryFixture factory) : IClassFixture<WebApplicationFactoryFixture>
{
    // One HttpClient shared by all tests — talks to the in-memory test server
    private readonly HttpClient _client = factory.CreateClient();

    // FIX: The API serializes enums as strings ("FullTime" not 0).
    // The default JsonSerializerOptions does not know how to read string enums.
    // JsonStringEnumConverter tells the deserializer to accept string values
    // for enum properties. Without this, ReadFromJsonAsync throws:
    //   "The JSON value could not be converted to CareerHub.Api.DTOs.JobType"
    // We also set PropertyNameCaseInsensitive so "id" matches "Id" etc.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task GetJobs_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/jobs");

        // Assert — the endpoint must return 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetJobs_ResponseIsPagedEnvelope()
    {
        // Act — ask for page 1 with 5 results per page
        var response = await _client.GetAsync("/api/v1/jobs?page=1&pageSize=5");

        // FIX: Pass JsonOptions so the enum "FullTime" deserializes correctly
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<JobResponse>>(JsonOptions);

        // Assert — the envelope must exist and echo back what we asked for
        Assert.NotNull(body);
        Assert.Equal(1, body.Page);
        Assert.Equal(5, body.PageSize);
        Assert.True(body.TotalCount >= 0);
    }

    [Fact]
    public async Task GetJobs_ResponseIncludesXTotalCountHeader()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/jobs");

        // Assert — the X-Total-Count header must be present so the frontend
        // can read the total without parsing the body
        Assert.True(response.Headers.Contains("X-Total-Count"),
            "X-Total-Count header must be present on all paginated list responses");
    }

    [Fact]
    public async Task GetJobs_WithoutVersion_ReturnsSameStatusAsV1()
    {
        // Act — call BOTH the unversioned and the versioned URL
        var unversioned = await _client.GetAsync("/api/jobs");
        var versioned   = await _client.GetAsync("/api/v1/jobs");

        // Assert — both must return 200 OK.
        // The unversioned URL works because of:
        //   1. The second [Route("api/[controller]")] attribute on the controller
        //   2. AssumeDefaultVersionWhenUnspecified = true in Program.cs
        Assert.Equal(HttpStatusCode.OK, unversioned.StatusCode);
        Assert.Equal(HttpStatusCode.OK, versioned.StatusCode);
    }

    [Fact]
    public async Task GetJobs_ResponseIncludesApiSupportedVersionsHeader()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/jobs");

        // Assert — header must be present
        Assert.True(response.Headers.Contains("api-supported-versions"),
            "api-supported-versions header must appear on every versioned response");

        // And it must contain "1.0"
        var headerValue = response.Headers.GetValues("api-supported-versions").First();
        Assert.Contains("1.0", headerValue);
    }

    [Fact]
    public async Task PostJob_WithoutToken_Returns401()
    {
        // Arrange — valid body, but NO Authorization header.
        // The server must reject at the authentication step before reading the body.
        var request = new CreateJobRequest(
            "Test Developer Position",
            Guid.NewGuid(),
            "Bloemfontein",
            "A valid description that is long enough to pass validation",
            JobType.FullTime,
            40000,
            60000,
            DateTime.UtcNow.AddDays(30));

        // Act — POST with no token
        var response = await _client.PostAsJsonAsync("/api/v1/jobs", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostApplication_WithoutToken_Returns401()
    {
        // Act — try to apply with no Authorization header
        var response = await _client.PostAsync($"/api/v1/applications/{Guid.NewGuid()}", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetJobById_WithValidId_DoesNotReturn500()
    {
        // Act — random id, almost certainly does not exist
        var response = await _client.GetAsync($"/api/v1/jobs/{Guid.NewGuid()}");

        // Assert — 200 or 404 are both fine. 500 is NEVER acceptable.
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected 200 or 404, got {response.StatusCode}");
    }

    [Fact]
    public async Task GetJobById_ResponseIncludesETagHeader()
    {
        // Arrange — get a real listing id from the list endpoint first
        var listResponse = await _client.GetAsync("/api/v1/jobs?page=1&pageSize=1");

        // FIX: Pass JsonOptions so the enum deserializes correctly
        var listBody = await listResponse.Content
            .ReadFromJsonAsync<PagedResponse<JobResponse>>(JsonOptions);

        // If the database has no listings skip — nothing to test against
        if (listBody is null || !listBody.Data.Any())
            return;

        var listingId = listBody.Data.First().id;

        // Act — fetch the single listing
        var response = await _client.GetAsync($"/api/v1/jobs/{listingId}");

        // Assert — ETag header must be present and not empty
        Assert.NotNull(response.Headers.ETag);
        Assert.False(string.IsNullOrWhiteSpace(response.Headers.ETag.Tag),
            "ETag header must be present and non-empty on single listing responses");
    }

    [Fact]
    public async Task GetJobById_WithMatchingETag_Returns304()
    {
        // Arrange — get a real listing id and its ETag
        var listResponse = await _client.GetAsync("/api/v1/jobs?page=1&pageSize=1");

        // FIX: Pass JsonOptions so the enum deserializes correctly
        var listBody = await listResponse.Content
            .ReadFromJsonAsync<PagedResponse<JobResponse>>(JsonOptions);

        if (listBody is null || !listBody.Data.Any())
            return;

        var listingId = listBody.Data.First().id;

        // First request — capture the ETag
        var firstResponse = await _client.GetAsync($"/api/v1/jobs/{listingId}");
        var etag = firstResponse.Headers.ETag;
        Assert.NotNull(etag);

        // Act — second request sends the ETag back in If-None-Match.
        // This tells the server: "I already have this version, only send the
        // body if the listing changed since I last fetched it."
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/jobs/{listingId}");
        request.Headers.IfNoneMatch.Add(etag);
        var secondResponse = await _client.SendAsync(request);

        // Assert — listing has not changed so server must return 304 Not Modified
        // with no body. This proves the full ETag round-trip works end to end.
        Assert.Equal(HttpStatusCode.NotModified, secondResponse.StatusCode);
    }
}