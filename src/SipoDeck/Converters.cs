using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SipoDeck;

/// <summary>Kaynak anahtarı (ör. "IconHome") → Application kaynağı (Geometry). Bulunamazsa null.</summary>
public sealed class ResourceKeyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string key && !string.IsNullOrWhiteSpace(key) ? System.Windows.Application.Current?.TryFindResource(key) : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>null veya boş metin → Collapsed, aksi halde Visible.</summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null || value is string { Length: 0 } ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
