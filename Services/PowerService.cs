using System.Collections.ObjectModel;
using Windows.Devices.Power;

namespace ShutdownTimer.Services;

public interface IPowerService
{
    event Action<double, bool> BatteryStatusChanged;
    double RemainingPercentage { get; }
    bool IsCharging { get; }
    void Refresh();
}

public class PowerService : IPowerService
{
    public event Action<double, bool>? BatteryStatusChanged;

    public double RemainingPercentage { get; private set; } = 100;
    public bool IsCharging { get; private set; } = true;

    public PowerService()
    {
        Battery.AggregateBattery.ReportUpdated += (s, e) => { Refresh(); };
        Refresh();
    }

    public void Refresh()
    {
        var report = Battery.AggregateBattery.GetReport();
        
        if (report.FullChargeCapacityInMilliwattHours.HasValue && 
            report.RemainingCapacityInMilliwattHours.HasValue)
        {
            RemainingPercentage = (double)report.RemainingCapacityInMilliwattHours.Value / 
                                  report.FullChargeCapacityInMilliwattHours.Value * 100.0;
        }
        else
        {
            RemainingPercentage = 100; // Assume 100 if no battery
        }

        IsCharging = report.Status == Windows.System.Power.BatteryStatus.Charging || 
                     report.Status == Windows.System.Power.BatteryStatus.Idle; // Idle usually means plugged in but full

        BatteryStatusChanged?.Invoke(RemainingPercentage, IsCharging);
    }
}
