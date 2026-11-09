using System.Globalization;
using Avalonia.Data.Converters;
using ServiceLib.Models.Dto;
using ServiceLib.ViewModels;

namespace v2rayN.Desktop.Converters;

/// <summary>判断一行是否是「分组标题行」（用于给标题行套不同样式）</summary>
public class GroupHeaderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ProfileItemModel item && ProfilesViewModel.IsGroupHeader(item);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
