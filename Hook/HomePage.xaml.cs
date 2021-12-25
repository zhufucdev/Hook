﻿using System;
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
                    if (item.IsOfType(StorageItemTypes.File))
                    {
                        TryOpen(item as StorageFile);
                    }
                }
            }
        }

        private void Page_DragOver(object sender, Windows.UI.Xaml.DragEventArgs e)
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
            StorageApplicationPermissions.FutureAccessList.Add(file);
            try
            {
                DocumentInfo.Parse(file.Path).Open();
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
                    Title = Utility.GetResourceString("ErrorOpen.Title"),
                    Content = ex.Message,
                    CloseButtonText = Utility.GetResourceString("CloseButton/Text")
                }.ShowAsync();
            }
        }

        private void RecentList_ItemClick(object sender, ItemClickEventArgs e)
        {
            MainPage.Instance.OpenDocument(e.ClickedItem as DocumentInfo);
        }
    }
}
