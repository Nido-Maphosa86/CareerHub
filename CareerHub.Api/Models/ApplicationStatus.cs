namespace CareerHub.Api.Models;

// The possible states an application can be in during the hiring workflow.
// Stored as a string in the database ("Submitted" not 0) — configured in the DbContext.
public enum ApplicationStatus
{
    Submitted,    // just arrived — default state
    UnderReview,  // recruiter has opened it
    Shortlisted,  // moving forward
    Rejected,     // not moving forward
    Offered       // job offered
}
