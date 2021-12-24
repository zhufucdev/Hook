﻿using System;
using System.Collections.Generic;
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
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace Hook
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class ContentPage : Page
    {
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
            await Task.Run(async () => {
                var path = await doc.BuildCache();
                await Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.High,
                    () => {
                        WebView.Source = new Uri("file://" + path.Path);
                        ConvertingLayout.Visibility = Visibility.Collapsed;
                    });
            });
        }
    }
}
