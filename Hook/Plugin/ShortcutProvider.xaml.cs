using Hook.API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Hook.Plugin
{
    public sealed partial class ShortcutProvider : UserControl
    {
        public ShortcutProvider()
        {
            this.InitializeComponent();
        }

        private IPlugin _plugin;
        public IPlugin Plugin
        {
            get => _plugin;
            set
            {
                // remove the previous listener
                if (_plugin != null)
                {
                    _plugin.Shortcuts.CollectionChanged -= Shortcuts_CollectionChanged;
                }

                _plugin = value;
                HeaderBlock.Text = value.Name;
                value.Shortcuts.CollectionChanged += Shortcuts_CollectionChanged;
            }
        }

        private async void Shortcuts_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // listen for ui changes
            await MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (Plugin.Shortcuts.Count <= 0)
                {
                    Hide();
                }
                else
                {
                    List.ItemsSource = Plugin.Shortcuts;
                    Show();
                }
            });
        }

        private void Hide()
        {
            List.Visibility = Visibility.Collapsed;
            HeaderBlock.Visibility = Visibility.Collapsed;
        }

        private void Show()
        {
            HeaderBlock.Visibility = Visibility.Visible;
            List.Visibility = Visibility.Visible;
        }

        private void UpdatePrograss(double value)
        {
            
        }

        private void RemoveFlyoutItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ClearMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
