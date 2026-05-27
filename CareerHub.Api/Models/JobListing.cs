namespace CareerHub.Api.Models;

public record JobListing
(
    Guid id,
    string Title,
    string Description,
    string Company,
    string Location,
    string Type
);
