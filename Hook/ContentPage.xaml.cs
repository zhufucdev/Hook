using Hook.API;
using Hook.Plugin;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

#pragma warning disable CS8305 // 类型仅用于评估，在将来的更新中可能会被更改或删除。
namespace Hook
{
    public sealed partial class ContentPage : Page
    {
        public static Dictionary<DocumentInfo, WebView2> OpenedDocument = new Dictionary<DocumentInfo, WebView2>();
        public DocumentInfo Current { get; private set; }

        public ContentPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            OpenDocument(e.Parameter as DocumentInfo);
        }

        private async void OpenDocument(DocumentInfo doc)
        {
            Windows.Storage.StorageFile cache = null;
            await Task.Run(async () =>
            {
                try
                {
                    cache = await doc.BuildCache();
                }
                catch (Exception ex)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        Prograss.ShowError = true;
                        var dialog = new ContentDialog()
                        {
                            Title = Utility.GetResourceString("ErrorOpen/Title"),
                            Content = ex.Message,
                            CloseButtonText = Utility.GetResourceString("CloseButton/Text")
                        };
                        dialog.CloseButtonClick += (sender, args) => MainPage.Instance.CloseDocument(doc);
                        await dialog.ShowAsync();
                    });
                    return;
                }
            });
            if (cache == null)
            {
                return;
            }

            WebView.Source = new Uri("file://" + cache.Path);
            ConvertingLayout.Visibility = Visibility.Collapsed;

            OpenedDocument[doc] = WebView;
            Current = doc;
            await WebView.EnsureCoreWebView2Async();
            // somehow, an interval is a must
            await Task.Delay(200);
            DocumentOpened?.Invoke(this, new DocumentOpenArgs(WebView, Current));
        }

        public void Close()
        {
            WebView.Close();
        }

        public static event EventHandler<DocumentOpenArgs> DocumentOpened;
    }
}
