using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CareerHub.Api.DTOs;

namespace CareerHub.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(IConfiguration configuration) : ControllerBase
{
    // POST /auth/login
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Step 1: Verify identity
        // Week 2: replace with await _dbContext.Users.FirstOrDefaultAsync(...)
        if (request.Username != "employer" || request.Password != "password123")
        {
            return Unauthorized(); // 401 — do not reveal which field was wrong
        }

        // Step 2: Build the claims payload
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.Username), // who this token is for
            new Claim(ClaimTypes.Role, "Employer")                    // what they are allowed to do
        };

        // Step 3: Create signing credentials — key read from config, never hardcoded
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]!)
        );
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Step 4: Construct and sign the token
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponse(tokenString));
    }

    // GET /auth/me
    // Requires a valid JWT — returns the decoded claims
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        // User is a ClaimsPrincipal — populated by UseAuthentication after JWT validation
        var username = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var role     = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new { username, role });
    }
}
