namespace CareerHub.Api.Models;

// An applicant registers once and can apply to many job listings.
// Username matches the JWT sub claim so we can identify the caller
// from their token without a database lookup on every request.
//
// No data annotations — all configuration is in CareerHubDbContext Fluent API.

public class Applicant
{
    public Applicant() { }

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Matches the JWT "sub" (subject) claim — used to find the applicant
    // record from the logged-in user's token without extra lookups.
    public string Username { get; set; } = string.Empty;

    // Collection navigation — all the applications this person has submitted
    public ICollection<Application> Applications { get; set; } = [];
}
