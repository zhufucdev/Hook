using Hook.API;
using Hook.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
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

            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            Window.Current.SetTitleBar(CustomDragRegion);

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

        private void TabView_TabCloseRequested(muxc.TabView sender, muxc.TabViewTabCloseRequestedEventArgs args)
        {
            CloseTab(args.Item);
        }

        private async void CloseTab(object item)
        {
            if (TabView.TabItems.Count == 1)
            {
                // last item remaining:
                //  close the app
                await ApplicationView.GetForCurrentView().TryConsolidateAsync();
            }
            else
            {
                // remove the given item by args
                TabView.TabItems.Remove(item);
                if (item is muxc.TabViewItem viewItem)
                {
                    var page = (viewItem.Content as Frame).Content;
                    if (page is ContentPage content)
                    {
                        content.Close();
                    }
                    GC.Collect();
                }
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

        public void OpenSettingsScreen()
        {
            var newTab = new muxc.TabViewItem()
            {
                Header = Utility.GetResourceString("SettingsHeader/Text"),
                IconSource = new muxc.SymbolIconSource() { Symbol = Symbol.Setting }
            };

            var frame = new Frame();
            newTab.Content = frame;
            frame.Navigate(typeof(SettingsPage));

            TabView.TabItems.Add(newTab);
            TabView.SelectedItem = newTab;
        }

        public void OpenPluginScreen()
        {
            var newTab = new muxc.TabViewItem()
            {
                Header = Utility.GetResourceString("PlugInsUIHeader/Text"),
                IconSource = new muxc.SymbolIconSource() { Symbol = Symbol.Repair }
            };

            var frame = new Frame();
            newTab.Content = frame;
            frame.Navigate(typeof(PluginPage));

            TabView.TabItems.Add(newTab);
            TabView.SelectedItem = newTab;
        }

        public void CloseDocument(IDocument doc)
        {
            foreach (muxc.TabViewItem item in TabView.TabItems)
            {
                if (item.Tag == doc)
                {
                    CloseTab(item);
                    break;
                }
            }
        }

        public void OpenDocument(CacheDocument doc)
        {
            var search = TabView.TabItems?.FirstOrDefault((item) => (item as muxc.TabViewItem).Tag == doc);
            if (search == null)
            {
                var newTab = new muxc.TabViewItem()
                {
                    Header = doc.Name,
                    IconSource = new muxc.SymbolIconSource() { Symbol = Symbol.Document },
                    Tag = doc
                };

                Frame frame = new Frame();
                newTab.Content = frame;
                frame.Navigate(typeof(ContentPage), doc);

                search = newTab;
                TabView.TabItems.Add(newTab);
            }
            TabView.SelectedItem = search;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            bool addHome = true;
            bool startupTask = false;

            if (e.Parameter is string)
            {
                var arg = e.Parameter as string;
                if (arg.Contains(App.PARAM_NO_HOME))
                {
                    addHome = false;
                }
                if (arg.Contains(App.PARAM_FROM_STARTUP))
                {
                    startupTask = true;
                }
            }

            if (addHome)
            {
                AddHomeScreen();
            }
        }
    }
}
