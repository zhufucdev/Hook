using Hook.API;
using Jint;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;

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
            Engine.SetValue("getRecentDocuments", new Func<IDocument[]>(() => DocumentInfo.RecentDocs.ToArray()));
            Engine.SetValue("download", new Func<string, string, string>(J_download));
            Engine.SetValue("openDocument", new Action<string>(J_openDocument));
        }

        private event EventHandler Unloaded;
        #region JS Functions
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
                    EventHandler<DocumentEventArgs> d = (s, v) => wrapCallback(new JSDocumentView(this, v.WebView, v.DocumentInfo));
                    ContentPage.DocumentOpened += d;
                    Unloaded += (s, v) => ContentPage.DocumentOpened -= d;
                    break;
                case "documentClosed":
                    EventHandler<DocumentEventArgs> d2 = (s, v) => wrapCallback(new JSDocumentView(this, v.WebView, v.DocumentInfo));
                    ContentPage.DocumentClosed += d2;
                    Unloaded += (s, v) => ContentPage.DocumentClosed -= d2;
                    break;
                case "unload":
                    Unloaded += wrapCallbackZeroArgument;
                    break;
                case "systemStartup":
                    PluginManager.OnStartupTaskRecognized += wrapCallbackZeroArgument;
                    Unloaded += (s, v) => PluginManager.OnStartupTaskRecognized -= wrapCallbackZeroArgument;
                    break;
                default:
                    throw new ArgumentException(string.Format("{0} isn't an event name", eventName));
            }
        }

        private JSDocumentView[] J_getOpenedDocuments() => 
            ContentPage.OpenedDocument.Select(p => new JSDocumentView(this, p.Value, p.Key)).ToArray();

        private string J_download(string uri, string rename = null)
        {
            async Task<string> download()
            {
                var client = new HttpClient();
                var result = await client.GetAsync(uri);
                if (!result.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(string.Format("HTTP: {0}", result.StatusCode));
                }
                string name = null;
                if (!string.IsNullOrWhiteSpace(rename))
                {
                    name = rename;
                }
                else
                {
                    IEnumerable<string> contentDisposition = null;

                    try
                    {
                        contentDisposition = result.Content.Headers.GetValues("Content-Disposition");
                    }
                    catch
                    {
                    }

                    if (contentDisposition != null)
                    {
                        name = contentDisposition.FirstOrDefault(v => v.StartsWith("filename="));
                        if (name != null)
                        {
                            var start = name.IndexOf('"');
                            name = name.Substring(start).Remove(name.Length - 1);
                        }
                    }
                    if (name == null)
                    {
                        name = Guid.NewGuid().ToString();
                    }
                }
                var file = await DownloadsFolder.CreateFileAsync(name);
                using (var fs = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await result.Content.CopyToAsync(fs.AsStreamForWrite());
                    await fs.FlushAsync();
                }
                StorageApplicationPermissions.FutureAccessList.Add(file);
                return file.Path;
            }
            var task = download();
            task.Wait();
            return task.Result;
        }

        private void J_openDocument(string path)
        {
            var document = DocumentInfo.Parse(path);
            document.Open();
        }
        #endregion

        public string Name => _name;

        public string Description => _des;

        public string Author => _author;

        public string Version => _version;

        public async void OnLoad()
        {
            var mainFile = await Root.GetFileAsync(PluginEntryFileName);
            try
            {
                Engine.Execute(await FileIO.ReadTextAsync(mainFile));
            }
            catch (Exception ex)
            {
                await MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
                    App.ShowInfoBar(
                        Utility.GetResourceString("PlugInFailure/Title").Replace("%s", Name),
                        string.Format("{0}: {1}", ex.GetType().Name, ex.Message),
                        Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error
                    )
                );
            }
        }

        public void OnUnload()
        {
            Unloaded?.Invoke(this, new EventArgs());
            Unloaded = null;
        }

        public const string PluginManifestFileName = "plugin.json";
        public const string PluginEntryFileName = "main.js";
    }
}
