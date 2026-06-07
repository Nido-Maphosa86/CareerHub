using CareerHub.Api.DTOs;
using CareerHub.Api.Exceptions;
using CareerHub.Api.Models;
using CareerHub.Api.Repositories;

namespace CareerHub.Api.Services;

// ── Interface ────────────────────────────────────────────────────────────────

public interface ICompanyService
{
    Task<IEnumerable<CompanyResponse>> GetAllAsync(CancellationToken ct = default);
    Task<CompanyResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, CancellationToken ct = default);
}

// ── Implementation ───────────────────────────────────────────────────────────

// No Microsoft.EntityFrameworkCore imports — all persistence happens in the repository.

public class CompanyService(ICompanyRepository companyRepo) : ICompanyService
{
    public Task<IEnumerable<CompanyResponse>> GetAllAsync(CancellationToken ct = default) =>
        companyRepo.GetAllAsync(ct);

    public async Task<CompanyResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var company = await companyRepo.GetByIdAsync(id, ct);

        if (company is null)
            throw new CompanyNotFoundException(id);

        return new CompanyResponse(company.Id, company.Name, company.Website, company.Industry);
    }

    public async Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, CancellationToken ct = default)
    {
        // Business rule: company names must be unique.
        if (await companyRepo.NameExistsAsync(request.Name, ct))
            throw new DuplicateJobListingException(request.Name, "companies");

        var company = new Company
        {
            Id       = Guid.NewGuid(),
            Name     = request.Name,
            Website  = request.Website,
            Industry = request.Industry
        };

        await companyRepo.AddAsync(company, ct);

        return new CompanyResponse(company.Id, company.Name, company.Website, company.Industry);
    }
}
