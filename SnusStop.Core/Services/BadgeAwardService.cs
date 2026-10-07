using SnusStop.Core.Models;

namespace SnusStop.Core.Services;

/// <summary>
/// Works out which badges still owe the user XP. Badges are derived — <see cref="BadgeService"/>
/// recomputes them from stats on every read — so the events log is what records a payout: one
/// <see cref="EventType.BadgeEarned"/> entry per badge key, written once and never revisited.
/// Anything earned without a matching entry is pending.
///
/// Because the log is the record, a badge cannot pay twice: not when the app re-syncs, and not when
/// the user reaches a day or savings threshold again after a slip. Earned badges and XP stay earned.
/// </summary>
public static class BadgeAwardService
{
    public static IReadOnlyList<Badge> Pending(Profile p, IEnumerable<EventLog> events, Stats s)
    {
        var ev = events as IList<EventLog> ?? events.ToList();

        // Only the key matters, not the amount: a baseline entry (0 XP) also counts as paid.
        var paid = ev
            .Where(e => e.Type == EventType.BadgeEarned && e.Source is not null)
            .Select(e => e.Source!)
            .ToHashSet();

        return BadgeService.Earned(p, ev, s).Where(b => !paid.Contains(b.Key)).ToList();
    }
}
