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

        public Wrapper GetWrapper() => new Wrapper(this);

        public static JSWindow Instance = new JSWindow();
        
        public class Wrapper
        {
            private JSWindow parent;
            public Wrapper(JSWindow parent)
            {
                this.parent = parent;
            }

#pragma warning disable IDE1006 // 命名样式
            public void activate() => parent.Activate();
            public void tryEntryFullscreen() => parent.TryEnterFullscreen();
#pragma warning restore IDE1006 // 命名样式
        }
    }
}
