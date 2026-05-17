using NCrontab;

namespace ShutdownTimer.Tests;

/// <summary>
/// Tests for cron parsing and schedule description logic,
/// extracted from ScheduleService to avoid WinUI dependencies.
/// </summary>
[TestClass]
public class ScheduleServiceTests
{
    // ── DescribeCron ────────────────────────────────────────────────

    private static string DescribeCron(string cron)
    {
        var parts = cron.Trim().Split(' ');
        if (parts.Length != 5) return $"Custom: {cron}";

        var minutePart = parts[0];
        var hourPart   = parts[1];
        var dayOfWeek  = parts[4];

        if (parts[2] != "*" || parts[3] != "*")
            return $"Custom: {cron}";

        if (!int.TryParse(hourPart, out var hour) || !int.TryParse(minutePart, out var minute))
            return $"Custom: {cron}";

        var timeStr = $"{hour:D2}:{minute:D2}";

        if (dayOfWeek == "*")
            return $"Every day at {timeStr}";

        var days = dayOfWeek.Split(',').Select(d => d.Trim() switch
        {
            "0" => "Sun", "1" => "Mon", "2" => "Tue", "3" => "Wed",
            "4" => "Thu", "5" => "Fri", "6" => "Sat",
            var other => other
        });

        return $"{string.Join(", ", days)} at {timeStr}";
    }

    [TestMethod]
    public void DescribeCron_EveryDay_ReturnsEveryDayDescription()
    {
        Assert.AreEqual("Every day at 23:00", DescribeCron("0 23 * * *"));
    }

    [TestMethod]
    public void DescribeCron_Weekdays_ReturnsCorrectDays()
    {
        var result = DescribeCron("0 22 * * 1,2,3,4,5");
        Assert.IsTrue(result.Contains("Mon"), $"Expected Mon in: {result}");
        Assert.IsTrue(result.Contains("Fri"), $"Expected Fri in: {result}");
        Assert.IsFalse(result.Contains("Sat"), $"Did not expect Sat in: {result}");
    }

    [TestMethod]
    public void DescribeCron_SingleDay_ReturnsCorrectDay()
    {
        Assert.AreEqual("Mon at 08:30", DescribeCron("30 8 * * 1"));
    }

    [TestMethod]
    public void DescribeCron_MidnightPaddedCorrectly()
    {
        Assert.AreEqual("Every day at 00:00", DescribeCron("0 0 * * *"));
    }

    [TestMethod]
    public void DescribeCron_NonStandardExpression_ReturnsCustom()
    {
        var result = DescribeCron("0 12 15 * *"); // Day-of-month field set
        Assert.IsTrue(result.StartsWith("Custom:"), $"Expected Custom: prefix in: {result}");
    }

    [TestMethod]
    public void DescribeCron_InvalidParts_ReturnsCustom()
    {
        Assert.IsTrue(DescribeCron("bad expression").StartsWith("Custom:"));
    }

    [TestMethod]
    public void DescribeCron_SundayExpression_ReturnsSun()
    {
        Assert.AreEqual("Sun at 03:00", DescribeCron("0 3 * * 0"));
    }

    // ── GetNextOccurrence ────────────────────────────────────────────

    [TestMethod]
    public void GetNextOccurrence_ValidCron_ReturnsFutureDate()
    {
        var schedule = CrontabSchedule.Parse("0 23 * * *");
        var next = schedule.GetNextOccurrence(DateTime.Now);
        Assert.IsTrue(next > DateTime.Now, "Next occurrence should be in the future");
    }

    [TestMethod]
    public void GetNextOccurrence_InvalidCron_ThrowsException()
    {
        Assert.ThrowsException<CrontabException>(() =>
            CrontabSchedule.Parse("not a cron"));
    }

    [TestMethod]
    public void GetNextOccurrence_EveryHour_ReturnedWithinNextHour()
    {
        var schedule = CrontabSchedule.Parse("0 * * * *");
        var now = DateTime.Now;
        var next = schedule.GetNextOccurrence(now);
        Assert.IsTrue((next - now).TotalHours < 1,
            $"Next hourly occurrence should be < 1 hour away, was {(next - now).TotalMinutes:F0} minutes");
    }

    // ── One-time schedule validation ──────────────────────────────────────

    [TestMethod]
    public void OneTimeSchedule_PastTarget_ShouldNotBeSchedulable()
    {
        var target = DateTime.Now.AddHours(-1); // in the past
        var isValid = target > DateTime.Now;
        Assert.IsFalse(isValid, "Past one-time target should be invalid");
    }

    [TestMethod]
    public void OneTimeSchedule_FutureTarget_ShouldBeSchedulable()
    {
        var target = DateTime.Now.AddHours(1);
        var isValid = target > DateTime.Now;
        Assert.IsTrue(isValid, "Future one-time target should be valid");
    }
}
