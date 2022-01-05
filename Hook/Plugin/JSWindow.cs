using System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Hook.Plugin
{
    internal class JSWindow
    {
        private CoreWindow _window = null;
        private ApplicationView _appView = null;
        private JSWindow()
        {
            
        }

        public void Activate()
        {
            if (_window == null)
            {
                Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    _window = CoreWindow.GetForCurrentThread();
                }).AsTask().Wait();
            }
            _window.Activate();
        }
        public void TryEnterFullscreen()
        {
            if (_appView == null)
            {
                Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    _appView = ApplicationView.GetForCurrentView();
                }).AsTask().Wait();
            }
            _appView.TryEnterFullScreenMode();
        }

        public static JSWindow Instance = new JSWindow();
    }
}
