using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Hook
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();

            ConverterList.ItemsSource = Utility.AvailableConverters;
            LoadSettings();
        }

        private void LoadSettings()
        {        
            var defaultIndex = Utility.AvailableConverters.IndexOf(Utility.DefaultConverter);
            ConverterList.SelectedIndex = defaultIndex;
        }

        private string ActuallLanguageCode = Utility.LanguageOverride;
        private void AppLanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var code = (e.AddedItems[0] as Language).Code;
            Utility.SetAppLanguage(code);
            if (code != ActuallLanguageCode)
            {
                Utility.LanguageOverride = code;

                App.ShowInfoBar(
                    Utility.GetResourceString("ReloadToApply/Title"),
                    Utility.GetResourceString("ReloadToApply/Message"),
                    Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational
                );
            }
        }

        private void AppLanguageCombo_Loaded(object sender, RoutedEventArgs e)
        {
            var combo = sender as ComboBox;
            for (int i = 0; i < Utility.LanguageCodes.Count; i++)
            {
                if (ActuallLanguageCode == Utility.LanguageCodes[i].Code)
                {
                    combo.SelectedIndex = i;
                    break;
                }
            }
        }
    }
}
