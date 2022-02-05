using Jint;

namespace Hook.Plugin.Interpret
{
    internal class JSPluginWrapper
    {
        private readonly JSPlugin parent;
        public JSPluginWrapper(JSPlugin parent)
        {
            this.parent = parent;
        }

#pragma warning disable IDE1006 // 命名样式
        public void createShortcut(string name, string description, Jint.Native.JsValue icon, Jint.Native.JsValue pathfinding = null)
        {
            string finalIcon = "document";
            Jint.Native.JsValue finalPathfinding = pathfinding;
            if (!icon.IsCallable())
            {
                finalIcon = icon.ToString();
            }
            else
            {
                finalPathfinding = icon;
            }
            var shortcut = parent.FunctionsContainer.CreateShortcut(name, description, finalIcon, finalPathfinding);
            _ = MainPage.Instance.Dispatcher
                .RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => parent.Shortcuts.Add(shortcut));
        }

        public JSSettings.Wrapper settings => parent.SettingsContainer.GetWrapper();
#pragma warning restore IDE1006 // 命名样式
    }
}
