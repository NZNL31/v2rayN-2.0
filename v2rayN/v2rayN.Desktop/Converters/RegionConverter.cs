using System.Globalization;
using Avalonia.Data.Converters;

namespace v2rayN.Desktop.Converters;

/// <summary>
/// 从别名（备注）里推断地区代码，用于表格里的地区徽章。
/// 支持 <c>HK HK1</c> / <c>US-LN</c> / <c>JP JP1 [Home]</c> 这类写法，也支持中文地区名。
/// ConverterParameter 传 <c>bool</c> 时返回是否识别到（用于 IsVisible）。
/// </summary>
public class RegionConverter : IValueConverter
{
    /// <summary>常见地区代码（按长度优先匹配，避免 UK/US 误判）</summary>
    private static readonly HashSet<string> Codes = new(StringComparer.OrdinalIgnoreCase)
    {
        "HK", "TW", "MO", "JP", "KR", "SG", "MY", "TH", "VN", "PH", "ID", "IN",
        "US", "CA", "MX", "BR", "AR", "CL",
        "GB", "UK", "IE", "FR", "DE", "NL", "BE", "LU", "CH", "AT", "IT", "ES", "PT",
        "SE", "NO", "FI", "DK", "IS", "PL", "CZ", "SK", "HU", "RO", "BG", "GR", "HR", "SI", "RS", "UA", "RU",
        "TR", "IL", "AE", "SA", "QA", "KW", "EG", "ZA", "NG", "KE",
        "AU", "NZ", "KZ", "AM", "GE", "AZ", "MN", "PK", "BD", "LK", "NP"
    };

    /// <summary>中文地区名 → 代码</summary>
    private static readonly (string Name, string Code)[] ChineseNames =
    [
        ("香港", "HK"), ("台湾", "TW"), ("台灣", "TW"), ("澳门", "MO"), ("澳門", "MO"),
        ("日本", "JP"), ("韩国", "KR"), ("韓國", "KR"), ("新加坡", "SG"), ("马来", "MY"),
        ("泰国", "TH"), ("越南", "VN"), ("菲律宾", "PH"), ("印度尼西亚", "ID"), ("印尼", "ID"), ("印度", "IN"),
        ("美国", "US"), ("美國", "US"), ("加拿大", "CA"), ("巴西", "BR"),
        ("英国", "GB"), ("英國", "GB"), ("德国", "DE"), ("德國", "DE"), ("法国", "FR"), ("法國", "FR"),
        ("荷兰", "NL"), ("荷蘭", "NL"), ("瑞士", "CH"), ("意大利", "IT"), ("西班牙", "ES"), ("葡萄牙", "PT"),
        ("瑞典", "SE"), ("挪威", "NO"), ("芬兰", "FI"), ("芬蘭", "FI"), ("丹麦", "DK"), ("丹麥", "DK"),
        ("波兰", "PL"), ("波蘭", "PL"), ("捷克", "CZ"), ("匈牙利", "HU"), ("罗马尼亚", "RO"), ("罗马尼亞", "RO"),
        ("俄罗斯", "RU"), ("俄羅斯", "RU"), ("乌克兰", "UA"), ("烏克蘭", "UA"), ("土耳其", "TR"),
        ("以色列", "IL"), ("阿联酋", "AE"), ("迪拜", "AE"), ("澳大利亚", "AU"), ("澳洲", "AU"), ("新西兰", "NZ"),
        ("南非", "ZA"), ("哈萨克", "KZ")
    ];

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var code = Detect(value as string);
        if (parameter is string p && p.Equals("bool", StringComparison.OrdinalIgnoreCase))
        {
            return code is not null;
        }
        return code;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    /// <summary>推断地区代码；识别不到返回 null</summary>
    public static string? Detect(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        // 1) 中文地区名优先（更不容易误判）
        foreach (var (name, code) in ChineseNames)
        {
            if (text.Contains(name, StringComparison.Ordinal))
            {
                return code;
            }
        }

        // 2) 按分隔符切词，找独立的两字母代码
        var tokens = text.Split([' ', '-', '_', '|', '/', '[', ']', '(', ')', '.', ',', ':'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in tokens)
        {
            // 允许 "HK1" 这种带编号的写法：取前两位字母
            if (token.Length < 2)
            {
                continue;
            }
            var head = token[..2];
            if (Codes.Contains(head) && char.IsLetter(token[0]) && char.IsLetter(token[1]))
            {
                return head.ToUpperInvariant();
            }
        }

        return null;
    }
}
