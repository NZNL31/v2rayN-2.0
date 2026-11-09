using System.Globalization;
using Avalonia.Data.Converters;
using ServiceLib.Models.Entities;

namespace v2rayN.Desktop.Converters;

/// <summary>
/// 左栏「订阅分组」右侧的汇总：<c>12 · 78ms</c>（节点数 · 平均延迟）。
/// 数据由 <see cref="ProfilesViewModel"/> 每次刷新订阅时一次性查库填进来，
/// 避免为每个分组单独查询。
/// </summary>
public class GroupSummaryConverter : IValueConverter
{
    private static Dictionary<string, (int Count, double AvgDelay)> _summary = [];

    /// <summary>由 ViewModel 在刷新订阅列表时调用</summary>
    public static void Update(IEnumerable<(string SubId, int Count, double AvgDelay)> items)
    {
        _summary = items
            .Where(t => t.SubId.IsNotEmpty())
            .GroupBy(t => t.SubId)
            .ToDictionary(g => g.Key, g => (g.Sum(x => x.Count), g.Average(x => x.AvgDelay)));
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var id = value switch
        {
            SubItem sub => sub.Id,
            string s => s,
            _ => null
        };

        if (id.IsNullOrEmpty() || !_summary.TryGetValue(id, out var sum) || sum.Count <= 0)
        {
            return string.Empty;
        }

        return sum.AvgDelay > 0
            ? $"{sum.Count} · {sum.AvgDelay:F0}ms"
            : $"{sum.Count}";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
