using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml.Controls;
using mucx = Microsoft.UI.Xaml.Controls;

namespace Hook
{
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();

            RecentList.ItemsSource = DocumentInfo.RecentDocs;
        }

        private async void Page_Drop(object sender, Windows.UI.Xaml.DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                foreach (var item in items)
                {
                    if (item.IsOfType(StorageItemTypes.File) && DocumentInfo.SupportedFormats.Contains(Path.GetExtension(item.Path).ToLower()))
                    {
                        TryOpen(item as StorageFile);
                    }
                }
            }
        }

        private async void Page_DragOver(object sender, Windows.UI.Xaml.DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void AddButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker()
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.List,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };
            foreach (var formt in DocumentInfo.SupportedFormats)
            {
                picker.FileTypeFilter.Add(formt);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                TryOpen(file);
            }
        }

        private async void TryOpen(StorageFile file)
        {
            try
            {
                DocumentInfo.Parse(file).Open();
            }
            catch (NotSupportedException)
            {
                await new ContentDialog()
                {
                    Title = Utility.GetResourceString("NotSupported/Title"),
                    Content = Utility.GetResourceString("NotSupported/Content").Replace("%s", System.IO.Path.GetExtension(file.Path)),
                    CloseButtonText = Utility.GetResourceString("CloseButton/Text")
                }.ShowAsync();
            }
            catch (Exception ex)
            {
                await new ContentDialog()
                {
                    Title = Utility.GetResourceString("ErrorOpen/Title"),
                    Content = ex.Message,
                    CloseButtonText = Utility.GetResourceString("CloseButton/Text")
                }.ShowAsync();
            }
        }

        private void RecentList_ItemClick(object sender, ItemClickEventArgs e)
        {
            MainPage.Instance.OpenDocument(e.ClickedItem as DocumentInfo);
        }

        private void SettingsButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            MainPage.Instance.OpenSettingsScreen();
        }

        private void RemoveMenuFlyoutItem_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var doc = (sender as MenuFlyoutItem).DataContext as DocumentInfo;
            DocumentInfo.RecentDocs.Remove(doc);
            DocumentInfo.SaveToDisk();
        }

        private void ClearMenuFlyoutItem_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            DocumentInfo.RecentDocs.Clear();
            DocumentInfo.SaveToDisk();
        }

        private void PluginButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            MainPage.Instance.OpenPluginScreen();
        }
    }
}
