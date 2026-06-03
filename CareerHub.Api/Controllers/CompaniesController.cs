using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CareerHub.Api.Data;
using CareerHub.Api.DTOs;
using CareerHub.Api.Models;
using CareerHub.Api.Exceptions;

namespace CareerHub.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CompaniesController(CareerHubDbContext db) : ControllerBase
{
    // ── GET /companies ────────────────────────────────────────────────────
    // Anonymous — anyone can browse companies
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompanyResponse>>> GetCompaniesAsync(
        CancellationToken cancellationToken)
    {
        // AsNoTracking() — read-only query, no need to pay the change tracking cost
        var companies = await db.Companies
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CompanyResponse(c.Id, c.Name, c.Website, c.Industry))
            .ToListAsync(cancellationToken);

        return Ok(companies);
    }

    // ── GET /companies/{id} ───────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyResponse>> GetCompanyByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var company = await db.Companies
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CompanyResponse(c.Id, c.Name, c.Website, c.Industry))
            .FirstOrDefaultAsync(cancellationToken);

        if (company is null)
            throw new CompanyNotFoundException(id);

        return Ok(company);
    }

    // ── POST /companies ───────────────────────────────────────────────────
    // Only Employers can register companies
    [Authorize(Roles = "Employer")]
    [HttpPost]
    public async Task<ActionResult<CompanyResponse>> CreateCompanyAsync(
        [FromBody] CreateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        bool exists = await db.Companies.AnyAsync(
            c => c.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (exists)
            return Conflict(new { Message = $"A company named '{request.Name}' already exists." });

        var company = new Company
        {
            Id       = Guid.NewGuid(),
            Name     = request.Name,
            Website  = request.Website,
            Industry = request.Industry
        };

        db.Companies.Add(company);
        await db.SaveChangesAsync(cancellationToken);

        return Created($"/companies/{company.Id}",
            new CompanyResponse(company.Id, company.Name, company.Website, company.Industry));
    }
}
