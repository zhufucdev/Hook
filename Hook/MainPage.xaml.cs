using System;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using muxc = Microsoft.UI.Xaml.Controls;

namespace Hook
{
    /// <summary>
    /// This page should be navigated to only once.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Instance { get; private set; }

        public MainPage()
        {
            this.InitializeComponent();

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

            Window.Current.SetTitleBar(CustomDragRegion);

            DocumentInfo.LoadFromDisk();
            AddHomeScreen();

            Instance = this;
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            if (FlowDirection == FlowDirection.LeftToRight)
            {
                CustomDragRegion.MinWidth = sender.SystemOverlayRightInset;
                ShellTitlebarInset.MinWidth = sender.SystemOverlayLeftInset;
            }
            else
            {
                CustomDragRegion.MinWidth = sender.SystemOverlayLeftInset;
                ShellTitlebarInset.MinWidth = sender.SystemOverlayRightInset;
            }

            CustomDragRegion.Height = ShellTitlebarInset.Height = sender.Height;
        }

        private void TabView_AddTabButtonClick(muxc.TabView sender, object args)
        {
            AddHomeScreen();
        }

        private async void TabView_TabCloseRequested(muxc.TabView sender, muxc.TabViewTabCloseRequestedEventArgs args)
        {
            if (sender.TabItems.Count == 1)
            {
                // last item remaining:
                //  close the app
                await ApplicationView.GetForCurrentView().TryConsolidateAsync();
            }
            else
            {
                // remove the given item by args
                sender.TabItems.Remove(args.Item);
            }
        }

        public void AddHomeScreen()
        {
            var newTab = new muxc.TabViewItem()
            {
                Header = Utility.GetResourceString("Home/Header"),
                IconSource = new muxc.SymbolIconSource() { Symbol = Symbol.Home }
            };

            Frame frame = new Frame();
            newTab.Content = frame;
            frame.Navigate(typeof(HomePage));

            TabView.TabItems.Add(newTab);
        }

        public void OpenDocument(DocumentInfo doc)
        {
            var path = doc.Path;
            var search = TabView.TabItems?.FirstOrDefault((item) => (item as muxc.TabViewItem).Tag?.ToString() == path);
            if (search == null)
            {
                var newTab = new muxc.TabViewItem()
                {
                    Header = Path.GetFileName(path),
                    IconSource = new muxc.SymbolIconSource() { Symbol = Symbol.Document },
                    Tag = path
                };

                Frame frame = new Frame();
                newTab.Content = frame;
                frame.Navigate(typeof(ContentPage), doc);

                search = newTab;
                TabView.TabItems.Add(newTab);
            }
            TabView.SelectedItem = search;
        }
    }
}
