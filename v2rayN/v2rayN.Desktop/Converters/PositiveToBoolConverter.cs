using Avalonia.Data.Converters;

namespace v2rayN.Desktop.Converters;

/// <summary>
/// 数值大于 0 时返回 true，用于隐藏"未测试"的条形指示器（延迟 -1 / 速度 0）
/// </summary>
public class PositiveToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => false,
            int i => i > 0,
            long l => l > 0,
            double d => d > 0,
            float f => f > 0,
            decimal m => m > 0,
            string s => decimal.TryParse(s, out var sv) && sv > 0,
            _ => false
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
