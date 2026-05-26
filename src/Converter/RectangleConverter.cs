using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ReFlex.Apps.DeepZoom.Converter;

public class RectangleConverter: IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 5)
            return new Rect(0,0,0,0);
        
        var converted = Array.ConvertAll(values, v => (double)v);
        
        return new Rect(converted[0] - converted[4], converted[1] - converted[4], converted[2], converted[3]);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}