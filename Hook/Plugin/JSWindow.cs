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
            _ = Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _window = CoreWindow.GetForCurrentThread();
                _appView = ApplicationView.GetForCurrentView();
            });
        }

        public void Activate()
        {
            _window?.Activate();
        }
        public void TryEnterFullscreen()
        {
            _appView?.TryEnterFullScreenMode();
        }

        public static JSWindow Instance = new JSWindow();
    }
}
