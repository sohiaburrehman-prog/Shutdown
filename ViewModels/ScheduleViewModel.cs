using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShutdownTimer.Models;
using ShutdownTimer.Services;

namespace ShutdownTimer.ViewModels;

public partial class ScheduleViewModel : ObservableObject
{
    private readonly IScheduleService _scheduleService;
    private readonly ISystemActionService _actionService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty] private ObservableCollection<ScheduleEntry> schedules = new();
    [ObservableProperty] private TimerAction newAction = TimerAction.Shutdown;
    [ObservableProperty] private int newHour = 23;
    [ObservableProperty] private int newMinute;
    [ObservableProperty] private bool monSelected = true;
    [ObservableProperty] private bool tueSelected = true;
    [ObservableProperty] private bool wedSelected = true;
    [ObservableProperty] private bool thuSelected = true;
    [ObservableProperty] private bool friSelected = true;
    [ObservableProperty] private bool satSelected;
    [ObservableProperty] private bool sunSelected;
    [ObservableProperty] private string statusText = "";
    [ObservableProperty] private bool isOneTimeMode;
    [ObservableProperty] private string schedulePreview = "Shutdown at 23:00 on weekdays.";

    // One-time schedule fields
    [ObservableProperty] private DateTimeOffset oneTimeDate = DateTimeOffset.Now;
    [ObservableProperty] private int oneTimeHour = DateTime.Now.Hour;
    [ObservableProperty] private int oneTimeMinute;

    public TimerAction[] AvailableActions { get; } = Enum.GetValues<TimerAction>();

    /// <summary>
    /// Returns upcoming schedules for the next 7 days, grouped by date, for the calendar view.
    /// </summary>
    public ObservableCollection<CalendarDayGroup> CalendarWeek { get; } = new();

    public ScheduleViewModel(
        IScheduleService scheduleService,
        ISystemActionService actionService,
        ISettingsService settingsService)
    {
        _scheduleService = scheduleService;
        _actionService = actionService;
        _settingsService = settingsService;

        _scheduleService.ScheduleTriggered += OnScheduleTriggered;
        UpdatePreview();
    }

    partial void OnNewActionChanged(TimerAction value) => UpdatePreview();
    partial void OnNewHourChanged(int value) => UpdatePreview();
    partial void OnNewMinuteChanged(int value) => UpdatePreview();
    partial void OnIsOneTimeModeChanged(bool value) => UpdatePreview();
    partial void OnOneTimeDateChanged(DateTimeOffset value) => UpdatePreview();
    partial void OnOneTimeHourChanged(int value) => UpdatePreview();
    partial void OnOneTimeMinuteChanged(int value) => UpdatePreview();
    partial void OnMonSelectedChanged(bool value) => UpdatePreview();
    partial void OnTueSelectedChanged(bool value) => UpdatePreview();
    partial void OnWedSelectedChanged(bool value) => UpdatePreview();
    partial void OnThuSelectedChanged(bool value) => UpdatePreview();
    partial void OnFriSelectedChanged(bool value) => UpdatePreview();
    partial void OnSatSelectedChanged(bool value) => UpdatePreview();
    partial void OnSunSelectedChanged(bool value) => UpdatePreview();

    private void UpdatePreview()
    {
        if (IsOneTimeMode)
        {
            SchedulePreview = $"{NewAction} once on {OneTimeDate:ddd MMM d} at {OneTimeHour:00}:{OneTimeMinute:00}.";
            return;
        }

        var selected = new List<string>();
        if (MonSelected) selected.Add("Mon");
        if (TueSelected) selected.Add("Tue");
        if (WedSelected) selected.Add("Wed");
        if (ThuSelected) selected.Add("Thu");
        if (FriSelected) selected.Add("Fri");
        if (SatSelected) selected.Add("Sat");
        if (SunSelected) selected.Add("Sun");

        var days = selected.Count switch
        {
            0 => "no days selected",
            5 when MonSelected && TueSelected && WedSelected && ThuSelected && FriSelected => "weekdays",
            2 when SatSelected && SunSelected => "weekends",
            7 => "every day",
            _ => string.Join(", ", selected)
        };

        SchedulePreview = $"{NewAction} at {NewHour:00}:{NewMinute:00} on {days}.";
    }

    [RelayCommand]
    private void SelectAllDays()
    {
        MonSelected = TueSelected = WedSelected = ThuSelected = FriSelected = SatSelected = SunSelected = true;
    }

    [RelayCommand]
    private void SelectWeekdays()
    {
        MonSelected = TueSelected = WedSelected = ThuSelected = FriSelected = true;
        SatSelected = SunSelected = false;
    }

    [RelayCommand]
    private void SelectWeekends()
    {
        MonSelected = TueSelected = WedSelected = ThuSelected = FriSelected = false;
        SatSelected = SunSelected = true;
    }

    [RelayCommand]
    private async Task Add11pmWeeknights()
    {
        NewHour = 23;
        NewMinute = 0;
        NewAction = TimerAction.Shutdown;
        SelectWeekdays();
        await AddSchedule();
    }

    [RelayCommand]
    private async Task AddMidnightDaily()
    {
        NewHour = 0;
        NewMinute = 0;
        NewAction = TimerAction.Shutdown;
        SelectAllDays();
        await AddSchedule();
    }

    [RelayCommand]
    private async Task AddWeekendMornings1am()
    {
        NewHour = 1;
        NewMinute = 0;
        NewAction = TimerAction.Restart;
        SelectWeekends();
        await AddSchedule();
    }

    [RelayCommand]
    private async Task AddBedtimeWeekdays()
    {
        NewHour = 21;
        NewMinute = 0;
        NewAction = TimerAction.Shutdown;
        SelectWeekdays();
        await AddSchedule();
    }

    public void LoadSchedules()
    {
        Schedules = new ObservableCollection<ScheduleEntry>(_settingsService.Settings.Schedules);
        RefreshCalendarWeek();
    }

    [RelayCommand]
    private async Task AddSchedule()
    {
        if (IsOneTimeMode)
        {
            await AddOneTimeSchedule();
            return;
        }

        var days = new List<string>();
        if (MonSelected) days.Add("1");
        if (TueSelected) days.Add("2");
        if (WedSelected) days.Add("3");
        if (ThuSelected) days.Add("4");
        if (FriSelected) days.Add("5");
        if (SatSelected) days.Add("6");
        if (SunSelected) days.Add("0");

        if (days.Count == 0)
        {
            StatusText = "Select at least one day.";
            return;
        }

        var daysPart = days.Count == 7 ? "*" : string.Join(",", days);
        var cron = $"{NewMinute} {NewHour} * * {daysPart}";

        var entry = new ScheduleEntry
        {
            CronExpression = cron,
            Action = NewAction,
            IsEnabled = true,
            FriendlyDescription = _scheduleService.DescribeCron(cron),
            NextRun = _scheduleService.GetNextOccurrence(cron)
        };

        _settingsService.Settings.Schedules.Add(entry);
        await _settingsService.SaveAsync();
        Schedules.Add(entry);
        RefreshCalendarWeek();

        StatusText = $"Added: {entry.FriendlyDescription}";
    }

    private async Task AddOneTimeSchedule()
    {
        var targetDate = OneTimeDate.Date;
        var target = new DateTime(targetDate.Year, targetDate.Month, targetDate.Day,
                                  OneTimeHour, OneTimeMinute, 0);

        if (target <= DateTime.Now)
        {
            StatusText = "One-time schedule must be in the future.";
            return;
        }

        var entry = new ScheduleEntry
        {
            Action = NewAction,
            IsEnabled = true,
            IsOneTime = true,
            OneTimeTarget = target,
            FriendlyDescription = $"Once: {target:ddd MMM dd, HH:mm}",
            NextRun = target
        };

        _settingsService.Settings.Schedules.Add(entry);
        await _settingsService.SaveAsync();
        Schedules.Add(entry);
        RefreshCalendarWeek();

        StatusText = $"Added one-time schedule: {target:ddd MMM dd, HH:mm}";
    }

    [RelayCommand]
    private async Task RemoveSchedule(ScheduleEntry? entry)
    {
        if (entry is null) return;

        _settingsService.Settings.Schedules.Remove(entry);
        await _settingsService.SaveAsync();
        Schedules.Remove(entry);
        RefreshCalendarWeek();

        StatusText = "Schedule removed.";
    }

    [RelayCommand]
    private async Task DuplicateSchedule(ScheduleEntry? entry)
    {
        if (entry is null) return;

        var clone = new ScheduleEntry
        {
            CronExpression = entry.CronExpression,
            Action = entry.Action,
            IsEnabled = true,
            FriendlyDescription = entry.FriendlyDescription,
            IsOneTime = entry.IsOneTime,
            OneTimeTarget = entry.OneTimeTarget?.AddDays(1),
            NextRun = entry.IsOneTime && entry.OneTimeTarget.HasValue
                ? entry.OneTimeTarget.Value.AddDays(1)
                : _scheduleService.GetNextOccurrence(entry.CronExpression)
        };

        _settingsService.Settings.Schedules.Add(clone);
        await _settingsService.SaveAsync();
        Schedules.Add(clone);
        RefreshCalendarWeek();
        StatusText = $"Duplicated: {clone.FriendlyDescription}";
    }

    [RelayCommand]
    private async Task ToggleSchedule(ScheduleEntry? entry)
    {
        if (entry is null) return;

        entry.IsEnabled = !entry.IsEnabled;
        await _settingsService.SaveAsync();

        var idx = Schedules.IndexOf(entry);
        if (idx >= 0)
        {
            Schedules.RemoveAt(idx);
            Schedules.Insert(idx, entry);
        }
        RefreshCalendarWeek();
    }

    private void OnScheduleTriggered(ScheduleEntry entry)
    {
        StatusText = $"Schedule triggered: {entry.FriendlyDescription}";
        _actionService.Execute(entry.Action, "Schedule", $"Schedule: {entry.FriendlyDescription}");

        // Refresh UI if it was a one-time schedule (now disabled)
        if (entry.IsOneTime)
        {
            LoadSchedules();
        }
    }

    /// <summary>
    /// Builds the calendar week view showing upcoming scheduled events for the next 7 days.
    /// </summary>
    public void RefreshCalendarWeek()
    {
        CalendarWeek.Clear();
        var today = DateTime.Today;

        for (int i = 0; i < 7; i++)
        {
            var date = today.AddDays(i);
            var dayGroup = new CalendarDayGroup
            {
                Date = date,
                DayLabel = date.ToString("ddd dd"),
                IsToday = i == 0
            };

            foreach (var schedule in _settingsService.Settings.Schedules.Where(s => s.IsEnabled))
            {
                if (schedule.IsOneTime && schedule.OneTimeTarget.HasValue)
                {
                    if (schedule.OneTimeTarget.Value.Date == date)
                    {
                        dayGroup.Events.Add(new CalendarEvent
                        {
                            Time = schedule.OneTimeTarget.Value.ToString("HH:mm"),
                            Action = schedule.Action,
                            Description = schedule.FriendlyDescription ?? "",
                            IsOneTime = true
                        });
                    }
                }
                else
                {
                    // Check cron schedule
                    try
                    {
                        var cron = NCrontab.CrontabSchedule.Parse(schedule.CronExpression);
                        var start = date;
                        var end = date.AddDays(1);
                        var occurrences = cron.GetNextOccurrences(start.AddSeconds(-1), end);
                        foreach (var occ in occurrences)
                        {
                            dayGroup.Events.Add(new CalendarEvent
                            {
                                Time = occ.ToString("HH:mm"),
                                Action = schedule.Action,
                                Description = schedule.FriendlyDescription ?? ""
                            });
                        }
                    }
                    catch { }
                }
            }

            dayGroup.Events = new ObservableCollection<CalendarEvent>(
                dayGroup.Events.OrderBy(e => e.Time));
            CalendarWeek.Add(dayGroup);
        }
    }
}

/// <summary>
/// Represents a day in the 7-day calendar view with its scheduled events.
/// </summary>
public class CalendarDayGroup
{
    public DateTime Date { get; set; }
    public string DayLabel { get; set; } = "";
    public bool IsToday { get; set; }
    public ObservableCollection<CalendarEvent> Events { get; set; } = new();
}

public class CalendarEvent
{
    public string Time { get; set; } = "";
    public TimerAction Action { get; set; }
    public string Description { get; set; } = "";
    public bool IsOneTime { get; set; }
}
