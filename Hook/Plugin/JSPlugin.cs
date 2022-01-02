using Hook.API;
using Jint;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hook.Plugin
{
    internal class JSPlugin : IPlugin
    {
        private readonly string _name, _des, _author, _version;
        public readonly string[] Embedded;

        public readonly Engine Engine = new Engine();
        public readonly StorageFolder Root;

        public JSPlugin(JObject manifest, StorageFolder root)
        {
            _name = (string)manifest["name"];
            _des = (string)manifest["description"];
            _author = (string)manifest["author"];
            _version = (string)manifest["version"];
            Embedded = ((JArray)manifest["embed"]).Select(c => (string)c).ToArray();
            Root = root;

            Initialize();
        }

        /// <summary>
        /// Functions and values provided by API
        /// </summary>
        private void Initialize()
        {
            Engine.SetValue("addEventListener", new Action<string, Jint.Native.JsValue>(J_addEventListener));
            Engine.SetValue("getOpenedDocuments", new Func<JSDocumentView[]>(J_getOpenedDocuments));
        }

        private event EventHandler Unloaded;
        private void J_addEventListener(string eventName, Jint.Native.JsValue callback)
        {
            if (callback == null)
            {
                throw new ArgumentException("callback is null");
            }

            void wrapCallback(params object[] args) => 
                callback.AsCallable().Call(args.Select((v) => Jint.Native.JsValue.FromObject(Engine, v)).ToArray());

            void wrapCallbackZeroArgument(object sender, object v) =>
                callback.AsCallable().Call();


            switch (eventName)
            {
                case "documentLoaded":
                    ContentPage.DocumentOpened += (s, v) => wrapCallback(new JSDocumentView(this, v.WebView, v.DocumentInfo));
                    break;
                case "documentClosed":
                    ContentPage.DocumentClosed += (s, v) => wrapCallback(new JSDocumentView(this, v.WebView, v.DocumentInfo));
                    break;
                case "unload":
                    Unloaded += wrapCallbackZeroArgument;
                    break;
                default:
                    throw new ArgumentException(string.Format("{0} isn't an event name", eventName));
            }
        }

        private JSDocumentView[] J_getOpenedDocuments() => 
            ContentPage.OpenedDocument.Select(p => new JSDocumentView(this, p.Value, p.Key)).ToArray();

        public string Name => _name;

        public string Description => _des;

        public string Author => _author;

        public string Version => _version;

        public async void OnLoad()
        {
            var mainFile = await Root.GetFileAsync(PluginEntryFileName);
            Engine.Execute(await FileIO.ReadTextAsync(mainFile));
        }

        public void OnUnload()
        {
            Unloaded?.Invoke(this, new EventArgs());
        }

        public const string PluginManifestFileName = "plugin.json";
        public const string PluginEntryFileName = "main.js";
    }
}
