using CareerHub.Api.Data;
using CareerHub.Api.DTOs;
using CareerHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.Api.Repositories;

public interface ICompanyRepository
{
    Task<IEnumerable<CompanyResponse>> GetAllAsync(CancellationToken ct = default);
    Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default);

    // True if a company with this ID exists — called by JobListingService
    // before creating a listing to ensure the company is real.
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    // True if a company with this name already exists (case-insensitive).
    Task<bool> NameExistsAsync(string name, CancellationToken ct = default);

    Task AddAsync(Company company, CancellationToken ct = default);
}

public class CompanyRepository(CareerHubDbContext db) : ICompanyRepository
{
    public async Task<IEnumerable<CompanyResponse>> GetAllAsync(CancellationToken ct = default) =>
        await db.Companies
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CompanyResponse(c.Id, c.Name, c.Website, c.Industry))
            .ToListAsync(ct);

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Companies.FindAsync([id], ct);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        await db.Companies.AnyAsync(c => c.Id == id, ct);

    public async Task<bool> NameExistsAsync(string name, CancellationToken ct = default) =>
        await db.Companies.AnyAsync(c => c.Name.ToLower() == name.ToLower(), ct);

    public async Task AddAsync(Company company, CancellationToken ct = default)
    {
        db.Companies.Add(company);
        await db.SaveChangesAsync(ct);
    }
}
