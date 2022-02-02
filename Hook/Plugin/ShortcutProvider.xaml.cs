using Hook.API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using muxc = Microsoft.UI.Xaml.Controls;

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
                HeaderBlock.Text = Utility.GetResourceString("ShortcutHeader/Pattern").Replace("%s", value.Name);
                List.ItemsSource = Plugin.Shortcuts;
                _ = UpdateVisibilityAsync();
                value.Shortcuts.CollectionChanged += Shortcuts_CollectionChanged;
            }
        }

        private async void Shortcuts_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // listen for ui changes
            await UpdateVisibilityAsync();
        }
        
        private async Task UpdateVisibilityAsync()
        {
            await MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (Plugin.Shortcuts.Count <= 0)
                {
                    Hide();
                }
                else
                {
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

        private void RemoveFlyoutItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ClearMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {            
            var btn = sender as Button;
            btn.IsEnabled = false;
            var progressBar = Utility.FindControl<muxc.ProgressBar>(btn, typeof(muxc.ProgressBar), "Progress");
            progressBar.Visibility = Visibility.Visible;
            (btn.FindName("FadeIn") as Storyboard).Begin();

            var shortcut = btn.Tag as Shortcut;
            shortcut.ProgressUpdater = (v) =>
            {
                progressBar.IsIndeterminate = false;
                progressBar.Value = v;
            };
            shortcut.Callback = () =>
            {
                _ = MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                    btn.IsEnabled = true;
                    var fadeOut = btn.FindName("FadeOut") as Storyboard;
                    fadeOut.Begin();
                });
            };
            shortcut.Open();
        }
    }
}
