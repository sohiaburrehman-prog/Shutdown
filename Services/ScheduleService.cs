using System.Diagnostics;
using Microsoft.UI.Dispatching;
using NCrontab;
using ShutdownTimer.Models;

namespace ShutdownTimer.Services;

public interface IScheduleService
{
    event Action<ScheduleEntry>? ScheduleTriggered;

    bool IsRunning { get; }

    void Start(ISettingsService settings);
    void Stop();
    DateTime? GetNextOccurrence(string cron);
    string DescribeCron(string cron);
}

public class ScheduleService : IScheduleService
{
    private CancellationTokenSource? _cts;
    private readonly DispatcherQueue _dispatcher;
    private ISettingsService? _settings;

    public event Action<ScheduleEntry>? ScheduleTriggered;
    public bool IsRunning { get; private set; }

    public ScheduleService()
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
    }

    public void Start(ISettingsService settings)
    {
        Stop();
        _settings = settings;
        IsRunning = true;
        _cts = new CancellationTokenSource();
        _ = CheckSchedulesAsync(_cts.Token).ContinueWith(t =>
        {
            if (t.IsFaulted)
                Debug.WriteLine($"[ScheduleService] CheckSchedulesAsync failed: {t.Exception?.InnerException?.Message}");
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        IsRunning = false;
    }

    public DateTime? GetNextOccurrence(string cron)
    {
        try
        {
            var schedule = CrontabSchedule.Parse(cron);
            return schedule.GetNextOccurrence(DateTime.Now);
        }
        catch
        {
            return null;
        }
    }

    public string DescribeCron(string cron)
    {
        var parts = cron.Trim().Split(' ');
        if (parts.Length != 5) return $"Custom: {cron}";

        var minutePart = parts[0];
        var hourPart   = parts[1];
        var dayOfWeek  = parts[4];

        // Only describe simple "minute hour * * days" patterns
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

    private async Task CheckSchedulesAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);

                if (_settings == null) continue;

                var now = DateTime.Now;

                // Use ToList() to allow modification during iteration
                foreach (var entry in _settings.Settings.Schedules.ToList())
                {
                    if (!entry.IsEnabled) continue;

                    try
                    {
                        // Handle one-time schedules
                        if (entry.IsOneTime && entry.OneTimeTarget.HasValue)
                        {
                            entry.NextRun = entry.OneTimeTarget;

                            if (entry.OneTimeTarget.Value <= now && (entry.LastRun == null || entry.LastRun < entry.OneTimeTarget))
                            {
                                entry.LastRun = now;
                                entry.IsEnabled = false; // Auto-disable one-time schedule
                                _ = _settings.SaveAsync().ContinueWith(t =>
                                {
                                    if (t.IsFaulted)
                                        Debug.WriteLine($"[ScheduleService] SaveAsync failed: {t.Exception?.InnerException?.Message}");
                                }, TaskContinuationOptions.OnlyOnFaulted);

                                _dispatcher.TryEnqueue(() => ScheduleTriggered?.Invoke(entry));
                            }
                            continue;
                        }

                        // Handle recurring cron schedules
                        var schedule = CrontabSchedule.Parse(entry.CronExpression);
                        var nextRun = entry.LastRun.HasValue
                            ? schedule.GetNextOccurrence(entry.LastRun.Value)
                            : schedule.GetNextOccurrence(now.AddMinutes(-1));

                        entry.NextRun = schedule.GetNextOccurrence(now);

                        if (nextRun <= now && (entry.LastRun == null || entry.LastRun < nextRun))
                        {
                            entry.LastRun = now;
                            _ = _settings.SaveAsync();

                            _dispatcher.TryEnqueue(() => ScheduleTriggered?.Invoke(entry));
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Invalid cron: {ex.Message}";
                        entry.CronError = errorMsg;
                        Debug.WriteLine($"[ScheduleService] Schedule '{entry.FriendlyDescription ?? entry.Id}' error: {ex.Message}");
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }
}
