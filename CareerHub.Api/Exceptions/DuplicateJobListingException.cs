namespace CareerHub.Api.Exceptions;

public class DuplicateJobListingException : Exception
{
    public DuplicateJobListingException(string title, string company)
        : base($"A job listing for '{title}' at '{company}' already exists.")
    {
    }
}
