using Jint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hook.Plugin
{
    internal class JSPluginWrapper
    {
        private JSPlugin parent;
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
            var shortcut = parent.CreateShortcut(name, description, finalIcon, finalPathfinding);
            MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => parent.Shortcuts.Add(shortcut));
        }
#pragma warning restore IDE1006 // 命名样式
    }
}
