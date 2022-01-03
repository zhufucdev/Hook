using Hook.API;
using Hook.Plugin;
using System;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace Hook
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class PluginPage : Page
    {
        public PluginPage()
        {
            this.InitializeComponent();
            PluginItems.ItemsSource = PluginManager.Plugins;
        }

        private async void Tip_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                foreach (var item in items)
                {
                    if (item is StorageFile && PluginManager.SupportedFormats.Contains(Path.GetExtension(item.Path)))
                    {
                        TryInstall(item as StorageFile);
                    }
                }
            }
        }

        private void Tip_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void UninstallMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog()
            {
                Title = Utility.GetResourceString("Uninstallation/Title"),
                Content = Utility.GetResourceString("Uninstallation/Content"),
                CloseButtonText = Utility.GetResourceString("CancelButton/Text"),
                PrimaryButtonText = Utility.GetResourceString("UninstallButton/Text")
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _ = PluginManager.Uninstall((sender as MenuFlyoutItem).Tag as IPlugin);
            }
        }

        private async void TryInstall(StorageFile file)
        {
            try
            {
                await PluginManager.Install(file);
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog()
                {
                    Title = Utility.GetResourceString("PlugInInstallationFailure/Title"),
                    Content = Utility.GetResourceString("PlugInInstallationFailure/Content")
                                    .Replace("%e%", ex.GetType().Name)
                                    .Replace("%m", ex.Message),
                    CloseButtonText = Utility.GetResourceString("CloseButton/Text")
                };
                await dialog.ShowAsync();
            }
        }
    }
}
