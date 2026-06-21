using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CareerHub.Api.Data;
using CareerHub.Api.DTOs;

namespace CareerHub.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
public class AuthController(IConfiguration configuration) : ControllerBase
{
    private static readonly (string Username, string Password, string Role, Guid? ApplicantId)[] _users =
    [
        ("employer",   "password123", "Employer",  null),
        ("applicant1", "password123", "Applicant", CareerHubDbContext.Applicant1Id),
        ("applicant2", "password123", "Applicant", CareerHubDbContext.Applicant2Id),
    ];

    [HttpPost("login")]
    [EndpointSummary("Log in and receive a JWT")]
    [EndpointDescription(
        "Validates a username and password against the seeded demo users and returns a JWT bearer " +
        "token valid for 8 hours. Use this token in the Authorization header as 'Bearer {token}' " +
        "for any endpoint marked Authorize.")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        var user = _users.FirstOrDefault(u =>
            u.Username == request.Username && u.Password == request.Password);

        if (user == default)
            return Unauthorized();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(System.Security.Claims.ClaimTypes.Role, user.Role)
        };

        if (user.ApplicantId.HasValue)
            claims.Add(new Claim("ApplicantId", user.ApplicantId.Value.ToString()));

        var key   = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]!));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token)));
    }

    [HttpGet("me")]
    [Authorize]
    [EndpointSummary("Get the current authenticated user")]
    [EndpointDescription(
        "Returns the username and role of the currently authenticated user, " +
        "read directly from the JWT claims. " +
        "Use this to confirm a token is valid and to check which role the user has. " +
        "Requires a valid Bearer token.")]
    public ActionResult GetCurrentUser()
    {
        var username = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var role     = User.FindFirstValue(ClaimTypes.Role);
        return Ok(new { username, role });
    }
}
