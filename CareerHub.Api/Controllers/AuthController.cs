using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CareerHub.Api.Data;
using CareerHub.Api.DTOs;

namespace CareerHub.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(IConfiguration configuration) : ControllerBase
{
    // Hardcoded credentials — Week 3 replaces with real DB lookup
    // Employer:    username=employer,    password=password123
    // Applicant 1: username=applicant1,  password=password123
    // Applicant 2: username=applicant2,  password=password123

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Step 1: Verify credentials and determine role + claims
        var (role, extraClaims) = request.Username switch
        {
            "employer" when request.Password == "password123"
                => ("Employer", Array.Empty<Claim>()),

            "applicant1" when request.Password == "password123"
                => ("Applicant", new[] { new Claim("ApplicantId", CareerHubDbContext.Applicant1Id.ToString()) }),

            "applicant2" when request.Password == "password123"
                => ("Applicant", new[] { new Claim("ApplicantId", CareerHubDbContext.Applicant2Id.ToString()) }),

            _ => (null, null)
        };

        if (role is null)
            return Unauthorized(); // 401 — do not say which field was wrong

        // Step 2: Build claims
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.Username),
            new Claim(ClaimTypes.Role, role)
        };

        if (extraClaims is not null)
            claims.AddRange(extraClaims);

        // Step 3: Sign with secret key from config
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Step 4: Build and return the token
        var token = new JwtSecurityToken(
            claims:            claims,
            expires:           DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token)));
    }

    // GET /auth/me — returns the decoded claims from the current token
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var username    = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var role        = User.FindFirstValue(ClaimTypes.Role);
        var applicantId = User.FindFirstValue("ApplicantId");

        return Ok(new { username, role, applicantId });
    }
}
