using Microsoft.AspNetCore.Mvc;
using CareerHub.Api.Models;

namespace CareerHub.Api.Controllers;

[ApiController]
[Route("[controller]")]   // resolves to /Jobs (case-insensitive, so /jobs works too)
public class JobsController : ControllerBase
{

    // GET /jobs
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobListing>>> GetJobsAsync()
    {
        // await does NOT block a thread
        // pause this method, return the thread to the pool
        // return here when the I/O is done
        await Task.Delay(200);   // await _dbContext.Jobs.ToListAsync();
        return JobListingStore.jobs;
    }

    // GET /jobs/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<JobListing>> GetJobByIdAsync(Guid id)
    {
        await Task.Delay(200);   // simulate I/O like the class example

        // FirstOrDefault returns null when nothing matches -
        // we use that to detect "not found" and return a 404
        var job = JobListingStore.jobs.FirstOrDefault(j => j.id == id);

        if (job is null)
            return NotFound();   // 404 - HTTP contract for "resource does not exist"

        return job;              // 200 - implicit conversion to ActionResult<JobListing>
    }

}
