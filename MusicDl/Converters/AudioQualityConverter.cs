using MusicDl.Models;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Markup;

namespace MusicDl.Converters
{
    public class AudioQualityConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AudioQuality audioQuality)
            {
                // Get the Description attribute of the enum value
                var fieldInfo = audioQuality.GetType().GetField(audioQuality.ToString());
                if (fieldInfo != null)
                {
                    var attribute = fieldInfo.GetCustomAttribute<DescriptionAttribute>();
                    if (attribute != null)
                    {
                        return attribute.Description;
                    }
                }
                return audioQuality.ToString();
            }
            return string.Empty;
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