using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace MusicDl.Converters
{
    [Obsolete]
    public class DurationConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int durationMs)
            {
                TimeSpan time = TimeSpan.FromMilliseconds(durationMs);
                return time.TotalHours >= 1 
                    ? string.Format("{0:D2}:{1:D2}:{2:D2}", (int)time.TotalHours, time.Minutes, time.Seconds) 
                    : string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);
            }
            return "00:00";
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