using System.Diagnostics;
using ShutdownTimer.Models;

namespace ShutdownTimer.Services;

public interface IBatteryAutomationService
{
    bool IsRunning { get; }
    void Start();
    void Stop();
}

public class BatteryAutomationService : IBatteryAutomationService
{
    private readonly IPowerService _powerService;
    private readonly ISettingsService _settingsService;
    private readonly ISystemActionService _actionService;
    private bool _lowTriggered;
    private bool _criticalTriggered;

    public bool IsRunning { get; private set; }

    public BatteryAutomationService(
        IPowerService powerService,
        ISettingsService settingsService,
        ISystemActionService actionService)
    {
        _powerService = powerService;
        _settingsService = settingsService;
        _actionService = actionService;
    }

    public void Start()
    {
        if (IsRunning) return;

        _powerService.BatteryStatusChanged += OnBatteryStatusChanged;
        IsRunning = true;
        _powerService.Refresh();
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _powerService.BatteryStatusChanged -= OnBatteryStatusChanged;
        IsRunning = false;
    }

    private void OnBatteryStatusChanged(double percentage, bool isCharging)
    {
        var settings = _settingsService.Settings;

        if (isCharging || percentage > settings.LowBatteryThreshold + 5)
        {
            _lowTriggered = false;
        }

        if (isCharging || percentage > settings.CriticalBatteryThreshold + 5)
        {
            _criticalTriggered = false;
        }

        if (settings.BatteryAutomationOnlyWhenUnplugged && isCharging)
        {
            return;
        }

        if (settings.CriticalBatteryAutomationEnabled &&
            !_criticalTriggered &&
            percentage <= settings.CriticalBatteryThreshold)
        {
            _criticalTriggered = true;
            _lowTriggered = true;
            ExecuteBatteryAction(settings.CriticalBatteryAction, "Critical battery", percentage);
            return;
        }

        if (settings.LowBatteryAutomationEnabled &&
            !_lowTriggered &&
            percentage <= settings.LowBatteryThreshold)
        {
            _lowTriggered = true;
            ExecuteBatteryAction(settings.LowBatteryAction, "Low battery", percentage);
        }
    }

    private void ExecuteBatteryAction(TimerAction action, string trigger, double percentage)
    {
        try
        {
            _actionService.ExecuteWithWarning(
                action,
                "Battery Automation",
                $"{trigger}: {percentage:0}% battery remaining");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BatteryAutomationService] Failed to execute battery action: {ex.Message}");
        }
    }
}
