using System;
using Windows.UI.Xaml.Data;

namespace Hook
{
    public class DateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return null;
            }
            var date = (DateTime)value;
            if (DateTime.Now - DateTime.Today < TimeSpan.FromHours(24))
            {
                // it's today
                return date.ToString("hh:mm tt");
            }
            return date.ToString("MMMM dd");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return DateTime.Parse(value.ToString());
        }
    }
}
