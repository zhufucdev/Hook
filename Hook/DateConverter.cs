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
            string testPattern = "MM-dd-yyyy";
            if (date.ToString(testPattern) == DateTime.Now.ToString(testPattern))
            {
                // it's today
                return date.ToString("hh:mm tt");
            }
            return date.ToString("MMMM dd hh:mm tt");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return DateTime.Parse(value.ToString());
        }
    }
}
