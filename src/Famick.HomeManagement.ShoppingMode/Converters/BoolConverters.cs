using System.Globalization;

namespace Famick.HomeManagement.ShoppingMode.Converters;

/// <summary>
/// Converts a boolean to TextDecorations (strikethrough when true).
/// </summary>
public class BoolToStrikethroughConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isPurchased && isPurchased)
        {
            return TextDecorations.Strikethrough;
        }
        return TextDecorations.None;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a boolean to opacity (0.5 when true, 1.0 when false).
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isPurchased && isPurchased)
        {
            return 0.5;
        }
        return 1.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a string to boolean (true if not null or empty).
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value as string);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
