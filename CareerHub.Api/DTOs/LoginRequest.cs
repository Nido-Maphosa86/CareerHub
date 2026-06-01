namespace CareerHub.Api.DTOs;

// What the client sends to authenticate
public record LoginRequest(
    string Username,
    string Password
);
