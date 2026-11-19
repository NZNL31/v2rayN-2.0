using Avalonia.Data.Converters;

namespace v2rayN.Desktop.Converters;

/// <summary>
/// 数值大于 0 时返回 true，用于隐藏"未测试"的条形指示器（延迟 -1 / 速度 0）。
/// ConverterParameter：
///   Invert   —— 取反，也就是 &lt;= 0（没测 / 失败 / 跳过）
///   Zero     —— 恰好等于 0（没有结果是 0 的状态文字，比如「跳过测试」）
///   Negative —— 小于 0（测试失败，界面上用 -1 表示）
/// </summary>
public class PositiveToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var number = value switch
        {
            null => (decimal?)null,
            int i => i,
            long l => l,
            double d => (decimal)d,
            float f => (decimal)f,
            decimal m => m,
            string s => decimal.TryParse(s, out var sv) ? sv : (decimal?)null,
            _ => null
        };

        var mode = parameter as string;
        if (mode is not null && mode.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            return number is null || number <= 0;
        }
        if (mode is not null && mode.Equals("Zero", StringComparison.OrdinalIgnoreCase))
        {
            return number is null || number == 0;
        }
        if (mode is not null && mode.Equals("Negative", StringComparison.OrdinalIgnoreCase))
        {
            return number is not null && number < 0;
        }

        return number is > 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
