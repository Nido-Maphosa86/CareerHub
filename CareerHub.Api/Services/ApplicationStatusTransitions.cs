using CareerHub.Api.Models;

namespace CareerHub.Api.Services;

// ══════════════════════════════════════════════════════════════════════
// STATUS TRANSITION VALIDATOR — Part 6 of Assignment 2.3
//
// Design decisions (required by the assignment):
//
// 1. Rules defined in exactly ONE place — this dictionary is the only
//    definition of what is and is not a valid transition.
//
// 2. No database query required — IsValid() is a pure function that
//    operates entirely in memory. The service calls it without any
//    async/await or repository access.
//
// 3. Adding a new valid transition requires changing ONE line.
//    Example: allow Offered → Accepted
//    Before: [ApplicationStatus.Offered] = new HashSet<ApplicationStatus>()
//    After:  [ApplicationStatus.Offered] = new HashSet<ApplicationStatus> { ApplicationStatus.Accepted }
//    No switch statements, no if/else chains to update anywhere else.
//
// Valid workflow:
//   Submitted → UnderReview
//   UnderReview → Shortlisted | Rejected
//   Shortlisted → Offered | Rejected
//   Offered → (terminal — no further transitions)
//   Rejected → (terminal — no further transitions)
// ══════════════════════════════════════════════════════════════════════

public static class ApplicationStatusTransitions
{
    // The transition table. Each key is the "from" status.
    // Each value is the set of "to" statuses that are permitted from there.
    private static readonly IReadOnlyDictionary<ApplicationStatus, IReadOnlySet<ApplicationStatus>> _transitions
        = new Dictionary<ApplicationStatus, IReadOnlySet<ApplicationStatus>>
        {
            [ApplicationStatus.Submitted]   = new HashSet<ApplicationStatus> { ApplicationStatus.UnderReview },
            [ApplicationStatus.UnderReview] = new HashSet<ApplicationStatus> { ApplicationStatus.Shortlisted, ApplicationStatus.Rejected },
            [ApplicationStatus.Shortlisted] = new HashSet<ApplicationStatus> { ApplicationStatus.Offered, ApplicationStatus.Rejected },
            [ApplicationStatus.Offered]     = new HashSet<ApplicationStatus>(), // terminal state
            [ApplicationStatus.Rejected]    = new HashSet<ApplicationStatus>(), // terminal state
        };

    // Returns true if moving from → to is a permitted transition.
    // Call this from ApplicationService before updating the status.
    // This method can be unit-tested completely independently of the database.
    public static bool IsValid(ApplicationStatus from, ApplicationStatus to) =>
        _transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
