using System.Globalization;
using System.Text;
using SnusStop.Core.Models;

namespace SnusStop.Core.Services;

/// <summary>Builds an RFC-4180 CSV of the whole local dataset for the Settings → Export action.</summary>
public static class CsvExporter
{
    public static string Build(Profile p, IEnumerable<GoalItem> goals, IEnumerable<EventLog> events)
    {
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        sb.AppendLine("Puffy export");
        sb.AppendLine();

        sb.AppendLine("PROFILE");
        sb.AppendLine("Name,QuitUtc,PouchesPerDay,PouchesPerCan,CanPrice,Currency,Motivations");
        sb.AppendLine(string.Join(",",
            Q(p.Name), Q(p.QuitUtc.ToString("o", inv)), p.PouchesPerDay, p.PouchesPerCan,
            p.CanPrice.ToString(inv), p.Currency, Q(string.Join("; ", p.Motivations))));
        sb.AppendLine();

        sb.AppendLine("GOALS");
        sb.AppendLine("Name,Price,Funded,FundedOnDay,SortOrder");
        foreach (var g in goals)
            sb.AppendLine(string.Join(",",
                Q(g.Name), g.Price.ToString(inv), g.Funded, g.FundedOnDay?.ToString(inv) ?? "", g.SortOrder));
        sb.AppendLine();

        sb.AppendLine("EVENTS");
        sb.AppendLine("TimestampUtc,Type,Source,Trigger,XpDelta,Note,AmountSpent");
        foreach (var e in events)
            sb.AppendLine(string.Join(",",
                Q(e.TimestampUtc.ToString("o", inv)), e.Type, Q(e.Source ?? ""), e.Trigger, e.XpDelta, Q(e.Note ?? ""), e.AmountSpent.ToString(inv)));

        return sb.ToString();
    }

    private static string Q(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
}
