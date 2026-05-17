using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShutdownTimer.Models;
using ShutdownTimer.Services;

namespace ShutdownTimer.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private List<HistoryEntry> _allEntries = new();

    [ObservableProperty] private ObservableCollection<HistoryEntry> historyEntries = new();
    [ObservableProperty] private bool isEmpty = true;
    [ObservableProperty] private string statusText = "";
    [ObservableProperty] private int selectedFilterIndex;
    [ObservableProperty] private string summaryText = "No activity yet";
    public string[] FilterOptions { get; } = ["All", "Completed", "Cancelled", "Countdown", "Schedule", "Process", "Idle", "Battery", "Quick"];

    public HistoryViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Load()
    {
        _allEntries = _settingsService.Settings.History
            .OrderByDescending(entry => entry.Timestamp)
            .ToList();
        ApplyFilter();
    }

    partial void OnSelectedFilterIndexChanged(int value) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<HistoryEntry> query = _allEntries;
        var selected = FilterOptions[Math.Clamp(SelectedFilterIndex, 0, FilterOptions.Length - 1)];

        query = selected switch
        {
            "Completed" => query.Where(entry => !entry.WasCancelled),
            "Cancelled" => query.Where(entry => entry.WasCancelled),
            "Countdown" => query.Where(entry => entry.Trigger.Contains("Countdown", StringComparison.OrdinalIgnoreCase)),
            "Schedule" => query.Where(entry => entry.Trigger.Contains("Schedule", StringComparison.OrdinalIgnoreCase)),
            "Process" => query.Where(entry => entry.Trigger.Contains("Process", StringComparison.OrdinalIgnoreCase)),
            "Idle" => query.Where(entry => entry.Trigger.Contains("Idle", StringComparison.OrdinalIgnoreCase)),
            "Battery" => query.Where(entry => entry.Trigger.Contains("Battery", StringComparison.OrdinalIgnoreCase)),
            "Quick" => query.Where(entry => entry.Trigger.Contains("Quick", StringComparison.OrdinalIgnoreCase)),
            _ => query
        };

        var filtered = query.ToList();
        HistoryEntries = new ObservableCollection<HistoryEntry>(filtered);
        IsEmpty = HistoryEntries.Count == 0;
        SummaryText = _allEntries.Count == 0
            ? "No activity yet"
            : $"{filtered.Count} shown of {_allEntries.Count} total actions";
    }

    [RelayCommand]
    private async Task ClearHistory()
    {
        _settingsService.Settings.History.Clear();
        await _settingsService.SaveAsync();
        _allEntries.Clear();
        ApplyFilter();
        StatusText = "History cleared.";
    }
}
