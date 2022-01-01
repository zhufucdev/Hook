using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI;

namespace Hook
{
    internal class Utility
    {

        public static string GetResourceString(string code)
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            var str = resourceLoader.GetString(code);
            return string.IsNullOrWhiteSpace(str) ? code : str;
        }

        public static DocumentConvert DefaultConverter;
        public static ObservableCollection<DocumentConvert> AvailableConverters = new ObservableCollection<DocumentConvert>();
    }
}
