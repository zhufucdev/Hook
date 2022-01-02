using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI;
using Windows.UI.Core;

namespace Hook
{
    internal class Utility
    {

        public static string GetResourceString(string code, UIContext context = null)
        {
            ResourceLoader resourceLoader;
            if (CoreWindow.GetForCurrentThread() != null) {
                resourceLoader = ResourceLoader.GetForCurrentView();
            }
            else if (context != null)
            {
                resourceLoader = ResourceLoader.GetForUIContext(context);
            }
            else
            {
                return code;
            }
            var str = resourceLoader.GetString(code);
            return string.IsNullOrWhiteSpace(str) ? code : str;
        }

        public static DocumentConvert DefaultConverter;
        public static ObservableCollection<DocumentConvert> AvailableConverters = new ObservableCollection<DocumentConvert>();
    }
}
