using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace MusicDl.Converters
{
    public class FileSizeConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int sizeInBytes)
            {
                double sizeInKB = sizeInBytes / 1024.0;
                if (sizeInKB < 1024)
                {
                    return $"{sizeInKB:N2} KB";
                }
                else
                {
                    double sizeInMB = sizeInKB / 1024.0;
                    return $"{sizeInMB:N2} MB";
                }
            }
            return "0 KB";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}