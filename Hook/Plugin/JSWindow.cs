using System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace Hook.Plugin
{
    internal class JSWindow
    {
        private CoreWindow _window = null;
        private ApplicationView _appView = null;
        private JSWindow()
        {
            MainPage.Instance.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _window = CoreWindow.GetForCurrentThread();
                _appView = ApplicationView.GetForCurrentView();
            }).AsTask().Wait();
        }

        public void Activate() => _window.Activate();
        public void TryEnterFullscreen() => _appView.TryEnterFullScreenMode();

        public static JSWindow Instance = new JSWindow();
    }
}
