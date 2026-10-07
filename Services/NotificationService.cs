using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using SnusStop.Core.Models;
using SnusStop.Core.Services;

namespace Nicotine_Stop.Services;

/// <summary>Schedules the app's local notifications (milestone / daily 08:00 / weekly Sun 18:00).</summary>
public class NotificationService
{
    private const int DailyId = 1000;
    private const int WeeklyId = 1001;
    private const int MilestoneBase = 2000;

    public async Task<bool> EnsurePermissionAsync()
    {
        try
        {
            if (await LocalNotificationCenter.Current.AreNotificationsEnabled())
                return true;
            return await LocalNotificationCenter.Current.RequestNotificationPermission();
        }
        catch
        {
            return false;
        }
    }

    public async Task ApplyAllAsync(Profile p, DateTime? lastSlipUtc = null)
    {
        await EnsurePermissionAsync();
        ScheduleDaily(p.NotifyDaily, string.IsNullOrWhiteSpace(p.Name) ? "there" : p.Name);
        ScheduleWeekly(p.NotifyWeekly);
        ScheduleMilestones(p, p.NotifyMilestone, lastSlipUtc);
    }

    public void ScheduleDaily(bool on, string name)
    {
        LocalNotificationCenter.Current.Cancel(DailyId);
        if (!on) return;
        _ = LocalNotificationCenter.Current.Show(new NotificationRequest
        {
            NotificationId = DailyId,
            Title = $"Good morning, {name} ☀️",
            Description = "Check in for today's +10 XP and keep your streak alive.",
            Schedule = new NotificationRequestSchedule { NotifyTime = NextTime(8, 0), RepeatType = NotificationRepeat.Daily },
        });
    }

    public void ScheduleWeekly(bool on)
    {
        LocalNotificationCenter.Current.Cancel(WeeklyId);
        if (!on) return;
        _ = LocalNotificationCenter.Current.Show(new NotificationRequest
        {
            NotificationId = WeeklyId,
            Title = "Your week in review 📊",
            Description = "See what you saved and how many cravings you beat this week.",
            Schedule = new NotificationRequestSchedule { NotifyTime = NextWeekly(DayOfWeek.Sunday, 18, 0), RepeatType = NotificationRepeat.Weekly },
        });
    }

    public void ScheduleMilestones(Profile p, bool on, DateTime? lastSlipUtc = null)
    {
        for (int i = 0; i < Milestones.Progress.Count; i++)
            LocalNotificationCenter.Current.Cancel(MilestoneBase + i);
        if (!on) return;

        var now = DateTime.Now;
        var quitLocal = StatsCalculator.CleanSinceUtc(p, lastSlipUtc).ToLocalTime();
        for (int i = 0; i < Milestones.Progress.Count; i++)
        {
            var m = Milestones.Progress[i];
            var at = quitLocal + m.At;
            if (at <= now) continue;
            _ = LocalNotificationCenter.Current.Show(new NotificationRequest
            {
                NotificationId = MilestoneBase + i,
                Title = $"Milestone unlocked: {m.Title} free! 🏆",
                Description = "Your body is measurably better. Come grab your badge — +50 XP is waiting.",
                Schedule = new NotificationRequestSchedule { NotifyTime = at },
            });
        }
    }

    public void CancelAll() => LocalNotificationCenter.Current.CancelAll();

    private static DateTime NextTime(int hour, int minute)
    {
        var now = DateTime.Now;
        var t = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
        return t <= now ? t.AddDays(1) : t;
    }

    private static DateTime NextWeekly(DayOfWeek day, int hour, int minute)
    {
        var t = NextTime(hour, minute);
        while (t.DayOfWeek != day) t = t.AddDays(1);
        return t;
    }
}
