namespace CareerHub.Api.Models;

// Replaces the plain string Type field from Assignment 1.1.
// Constrains the type to four valid values — "banana" is now impossible.
public enum JobType
{
    FullTime,
    PartTime,
    Contract,
    Internship
}
