using Jint;
using Jint.Native;
using Jint.Native.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hook.Plugin
{
    internal class JSSettings
    {
        public readonly StorageFile Container;
        public readonly JSPlugin Plugin;
        private readonly Jint.Native.Object.ObjectInstance Json;
        public JSSettings(JSPlugin parent)
        {
            bool created = false;
            using (var task = parent.Root.TryGetItemAsync(SettingsFileName).AsTask())
            {
                task.Wait();
                var container = task.Result;
                if (container == null)
                {
                    using (var creation = parent.Root.CreateFileAsync(SettingsFileName).AsTask())
                    {
                        creation.Wait();
                        container = creation.Result;
                    }
                    created = true;
                }

                Container = container as StorageFile;
            }

            if (!created)
            {
                using (var read = FileIO.ReadTextAsync(Container).AsTask())
                {
                    read.Wait();
                    Json = new JsonParser(parent.Engine).Parse(read.Result).AsObject();
                }
            }
            else
            {
                Json = new Jint.Native.Object.ObjectInstance(parent.Engine);
            }
            Plugin = parent;

            parent.Unloaded += Parent_Unloaded;
        }

        private void Parent_Unloaded(object sender, EventArgs e)
        {
            _ = MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Plugin.Settings.Clear);
        }

        public void LoadSettingsDescriptor(JObject descriptor)
        {
            try
            {
                foreach (var property in descriptor)
                {
                    var item = new JSSettingsItem((JObject)property.Value, property.Key, this);
                    _ = MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        () => Plugin.Settings.Add(item));
                }
            }
            catch(Exception ex)
            {
                Parent_Unloaded(null, null);
                throw ex;
            }
        }

        public void Put(string name, JsValue value)
        {
            if (value == null)
            {
                Json.RemoveOwnProperty(name);
            }
            else
            {
                Json.Set(name, value);
            }
            var args = new SettingsEventArgs(name, value);
            SettingsChanged?.Invoke(this, args);

            var item = Plugin.Settings.First(x => (x as JSSettingsItem).Key == name) as JSSettingsItem;
            item.NotifyValueChanged(this);
        }

        public JsValue Get(string name) => Json.Get(name);

        public async Task Save()
        {
            await FileIO.WriteTextAsync(
                Container, 
                new JsonSerializer(Plugin.Engine).Serialize(Json, "", "").AsString()
            );
        }

        public Wrapper GetWrapper() => new Wrapper(this);

        public event EventHandler<SettingsEventArgs> SettingsChanged;

        public const string SettingsFileName = "settings.json";

        public class SettingsEventArgs : EventArgs
        {
            public readonly string Key;
            public readonly JsValue Value;
            public SettingsEventArgs(string name, JsValue value) : base()
            {
                Key = name;
                Value = value;
            }

            public Wrapper GetWrapper(string sender) => new Wrapper(Key, Value, sender);

            public class Wrapper
            {
                public readonly string key;
                public readonly JsValue value;
                public readonly string sender;
                public Wrapper(string key, JsValue value, string sender)
                {
                    this.key = key;
                    this.value = value;
                    this.sender = sender;
                }
            }
        }

        public class Wrapper
        {
            private readonly JSSettings parent;
            public Wrapper(JSSettings parent)
            {
                this.parent = parent;
            }

#pragma warning disable IDE1006 // 命名样式
            public void put(string name, JsValue value)
            {
                parent.Put(name, value);
            }
            public object get(string name) => parent.Get(name);
#pragma warning restore IDE1006 // 命名样式
        }
    }
}
