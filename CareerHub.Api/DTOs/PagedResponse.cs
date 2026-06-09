namespace CareerHub.Api.DTOs;

// Standard pagination envelope returned by all paginated list endpoints.
// TotalCount lets the frontend show "Showing 1-20 of 843 listings" without parsing the body.
// It is also written to the X-Total-Count response header for lightweight header-only reads.
public record PagedResponse<T>(
    IEnumerable<T> Data,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage
)
{
    // Factory method — keeps the calculation in one place.
    public static PagedResponse<T> Create(IEnumerable<T> data, int page, int pageSize, int totalCount)
    {
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new PagedResponse<T>(
            data,
            page,
            pageSize,
            totalCount,
            totalPages,
            HasNextPage:     page < totalPages,
            HasPreviousPage: page > 1
        );
    }
}
