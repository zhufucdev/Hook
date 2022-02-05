using Hook.API;
using Jint.Native;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace Hook.Plugin
{
    internal class JSSettingsItem : ISettingsItem
    {
        public readonly Range ValueRange;
        public readonly string Key;
        public readonly JSSettings Parent;
        public JSSettingsItem(JObject descriptor, string key, JSSettings parent)
        {
            Key = key;
            Parent = parent;
            Name = (string)descriptor[KEY_TITLE];
            Description = (string)descriptor[KEY_DESCRIPTION];
            if (descriptor.ContainsKey(KEY_TYPE))
            {
                var target = (string)descriptor[KEY_TYPE];
                if (string.IsNullOrWhiteSpace(target))
                {
                    throw new ArgumentException(
                        "type is empty",
                        GetPath("type")
                    );
                }
                switch (target)
                {
                    case "string":
                        Type = typeof(string);
                        break;
                    case "double":
                        Type = typeof(double);
                        break;
                    case "int":
                        Type = typeof(int);
                        break;
                    case "long":
                        Type = typeof(long);
                        break;
                    case "bool":
                        Type = typeof(bool);
                        break;
                    default:
                        throw new ArgumentException(
                            string.Format("type {0} is not supported", target),
                            GetPath("type")
                        );
                }
            }
            else
            {
                throw new ArgumentException(GetPath("type") + " not found");
            }
            if (descriptor.ContainsKey(KEY_RANGE))
            {
                var rangeStr = (string)descriptor[KEY_RANGE];
                if (string.IsNullOrWhiteSpace(rangeStr))
                {
                    throw new ArgumentException(GetPath("range") + " is empty");
                }
                if (!Range.SupportedTypes.Contains(Type))
                {
                    throw new ArgumentException(
                        string.Format("type {0} doesn't support range", Type.Name),
                        GetPath("range")
                    );
                }
                ValueRange = Range.Parse(rangeStr);
            }
            if (descriptor.ContainsKey(KEY_ICON))
            {
                var str = (string)descriptor[KEY_ICON];
                Enum.TryParse(typeof(Symbol), str, true, out var symbol);
                if (symbol == null)
                {
                    throw new ArgumentException(string.Format("symbol named {0} not found", str));
                }
                IconSymbol = (Symbol)symbol;
            }
            else
            {
                IconSymbol = Symbol.Repair;
            }
        }

        private string GetPath(string item) => string.Format("{0}/settings/{1}/{2}", JSPlugin.PLUGIN_MANIFEST_FILE_NAME, Key, item);

        public string Name { get; private set; }
        public string Description { get; private set; }
        public Type Type { get; private set; }
        public Symbol IconSymbol { get; private set; }
        public object Value
        {
            get => Parent.Get(Key).ToObject();
            set {
                Parent.Put(Key, JsValue.FromObject(Parent.Plugin.Engine, value));
                _ = Parent.Save();
            }
        }

        public event EventHandler<object> ValueChanged;

        internal void NotifyValueChanged(object sender)
        {
            ValueChanged?.Invoke(sender, Value);
        }

        public const string KEY_NAME = "name";
        public const string KEY_RANGE = "range";
        public const string KEY_TITLE = "title";
        public const string KEY_DESCRIPTION = "description";
        public const string KEY_TYPE = "type";
        public const string KEY_ICON = "icon";
    }
}
