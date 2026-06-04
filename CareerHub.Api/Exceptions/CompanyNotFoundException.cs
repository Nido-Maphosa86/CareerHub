namespace CareerHub.Api.Exceptions;

public class CompanyNotFoundException : Exception
{
    public CompanyNotFoundException(Guid id)
        : base($"The company with ID {id} was not found.") { }
}
