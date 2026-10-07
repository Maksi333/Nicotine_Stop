using SnusStop.Core.Models;

namespace SnusStop.Core.Services;

/// <summary>Evaluates which of the 24 badges the user has earned from stats + events.</summary>
public static class BadgeService
{
    public static IReadOnlyList<Badge> Earned(Profile p, IEnumerable<EventLog> events, Stats s)
    {
        var ev = events as IList<EventLog> ?? events.ToList();
        int cravings = ev.Count(e => e.Type == EventType.CravingWon);
        int goalsFunded = ev.Count(e => e.Type == EventType.GoalFunded);
        int checkins = ev.Count(e => e.Type == EventType.CheckIn);
        int totalXp = ev.Sum(e => e.XpDelta);
        bool anyBreathe = ev.Any(e => e.Type == EventType.CravingWon && e.Source == "breathe");
        bool anyGame = ev.Any(e => e.Type == EventType.CravingWon && e.Source == "game");
        int level = XpService.ForXp(totalXp).Number;
        var recorded = ev.Where(e => e.Type == EventType.BadgeEarned && e.Source is not null)
            .Select(e => e.Source!).ToHashSet();

        var earned = new List<Badge>();
        void Add(string key, bool cond)
        {
            // A slip can reset time or lower savings, but already-awarded badges stay earned.
            if (cond || recorded.Contains(key)) earned.Add(Badges.ByKey(key, p.Addiction, p.Currency));
        }

        Add("day1", s.Days >= 1);
        Add("day3", s.Days >= 3);
        Add("week1", s.Days >= 7);
        Add("week2", s.Days >= 14);
        Add("month1", s.Days >= 30);
        Add("month3", s.Days >= 91);
        Add("month6", s.Days >= 182);
        Add("year1", s.Days >= 365);
        Add("save250", s.Money >= 250m);
        Add("save500", s.Money >= 500m);
        Add("save1000", s.Money >= 1000m);
        Add("save5000", s.Money >= 5000m);
        Add("cravings10", cravings >= 10);
        Add("cravings25", cravings >= 25);
        Add("cravings50", cravings >= 50);
        Add("goal1", goalsFunded >= 1);
        Add("goal3", goalsFunded >= 3);
        Add("pouches500", s.PouchesAvoided >= 500);
        Add("pouches1000", s.PouchesAvoided >= 1000);
        Add("breathe1", anyBreathe);
        Add("game1", anyGame);
        Add("checkin7", checkins >= 7);
        Add("level5", level >= 5);
        // weekly1 is granted by the platform when the first weekly summary is delivered.

        return earned;
    }

    /// <summary>Badges ordered for the Journey grid: earned first (canonical order), then locked.</summary>
    public static (IReadOnlyList<Badge> Earned, IReadOnlyList<Badge> Locked) Split(
        Profile p, IEnumerable<EventLog> events, Stats s)
    {
        var earned = Earned(p, events, s);
        var earnedKeys = earned.Select(b => b.Key).ToHashSet();
        var all = Badges.For(p.Addiction, p.Currency);
        var ordered = all.Where(b => earnedKeys.Contains(b.Key)).ToList();
        var locked = all.Where(b => !earnedKeys.Contains(b.Key)).ToList();
        return (ordered, locked);
    }
}
