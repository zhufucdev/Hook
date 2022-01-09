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

            OpenedDocument[doc] = WebView;
            Current = doc;
            if (PluginManager.Plugins.Count > 0)
            {
                ConverterHeader.Text = Utility.GetResourceString("PlugInLoadHeader/Text");
                await WebView.EnsureCoreWebView2Async();
                var args = new DocumentEventArgs(WebView, Current);
                DocumentOpened?.Invoke(this, args);
                // somehow, an interval is a must
                await Task.Delay(500);
                PluginManager.UseWithDependency(p =>
                {
                    if (DocumentOpenedForPlugin.ContainsKey(p))
                    {
                        var handlers = DocumentOpenedForPlugin[p];
                        foreach (var action in handlers)
                        {
                            action(args);
                        }
                    }
                });
                ConvertingLayout.Visibility = Visibility.Collapsed;
            }
            
        }

        public void Close()
        {
            WebView.Close();
            DocumentClosed?.Invoke(this, new DocumentEventArgs(WebView, Current));
        }

        public static event EventHandler<DocumentEventArgs> DocumentOpened;
        public static PluginEventCollection<DocumentEventArgs> DocumentOpenedForPlugin
            = new PluginEventCollection<DocumentEventArgs>();
        public static event EventHandler<DocumentEventArgs> DocumentClosed;

        public static void RegisterFor<T>(IPlugin plugin, PluginEventCollection<T> e, Action<T> action)
        {
            List<Action<T>> handlers;
            if (!e.ContainsKey(plugin))
            {
                handlers = new List<Action<T>>();
                e[plugin] = handlers;
            }
            else
            {
                handlers = e[plugin];
            }
            handlers.Add(action);
        }
    }
}
