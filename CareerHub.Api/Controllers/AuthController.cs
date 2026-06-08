using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CareerHub.Api.Data;
using CareerHub.Api.DTOs;

namespace CareerHub.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController(IConfiguration configuration) : ControllerBase
{
    private static readonly (string Username, string Password, string Role, Guid? ApplicantId)[] _users =
    [
        ("employer",    "password123", "Employer",  null),
        ("applicant1",  "password123", "Applicant", CareerHubDbContext.Applicant1Id),
        ("applicant2",  "password123", "Applicant", CareerHubDbContext.Applicant2Id),
    ];

    [HttpPost("login")]
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
    public ActionResult GetCurrentUser()
    {
        var username = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var role     = User.FindFirstValue(ClaimTypes.Role);
        return Ok(new { username, role });
    }
}
