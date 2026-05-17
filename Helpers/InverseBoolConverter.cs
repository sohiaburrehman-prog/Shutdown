using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ShutdownTimer.Helpers;

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is bool b ? !b : value;
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility v && v == Visibility.Visible;
}

/// <summary>
/// Inverse of BoolToVisibilityConverter: true = Collapsed, false = Visible.
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility v && v == Visibility.Collapsed;
}

/// <summary>
/// Converts an int index to Visibility: index > 0 = Visible, 0 = Collapsed.
/// Used to show/hide the "wait for process start" text box.
/// </summary>
public class IndexToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is int i && i > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => 0;
}

/// <summary>
/// Converts a bool to a status color: false (executed) = green, true (cancelled) = red/orange.
/// Used by HistoryPage to show status indicator dots.
/// </summary>
public class BoolToStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool cancelled && cancelled
            ? Windows.UI.Color.FromArgb(255, 255, 82, 82)   // Red for cancelled
            : Windows.UI.Color.FromArgb(255, 0, 230, 118);  // Green for executed

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => false;
}

/// <summary>
/// Returns Visibility.Visible when the string is non-empty, Collapsed when null or whitespace.
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is string s && !string.IsNullOrWhiteSpace(s) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => DependencyProperty.UnsetValue;
}

public class BoolToInfoBarSeverityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool isError && isError
            ? Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error
            : Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => false;
}

public class PercentageStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is double d ? $"{d:0}%" : "0%";

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => DependencyProperty.UnsetValue;
}
