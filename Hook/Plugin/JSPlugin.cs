using Hook.API;
using Jint;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml.Controls;
using muxc = Microsoft.UI.Xaml.Controls;

namespace Hook.Plugin
{
    internal class JSPlugin : IPlugin
    {
        private readonly string _name, _des, _author, _version;
        public readonly string[] Embedded, _require = new string[0], _depend = new string[0];

        public readonly Engine Engine = new Engine();
        public readonly StorageFolder Root;

        public JSPlugin(JObject manifest, StorageFolder root)
        {
            _name = (string)manifest[MANIFEST_KEY_NAME];
            if (manifest.ContainsKey(MANIFEST_KEY_DESCRIPTION))
            {
                _des = (string)manifest[MANIFEST_KEY_DESCRIPTION];
            }
            else
            {
                _des = null;
            }
            _author = (string)manifest[MANIFEST_KEY_AUTHOR];
            _version = (string)manifest[MANIFEST_KEY_VERSION];
            if (manifest.ContainsKey(MANIFEST_KEY_EMBED))
            {
                var token = manifest[MANIFEST_KEY_EMBED];
                if (token is JArray)
                {
                    Embedded = ((JArray)manifest[MANIFEST_KEY_EMBED]).Select(c => (string)c).ToArray();
                }
                else
                {
                    Embedded = new string[] { token.ToString() };
                }
            }
            if (manifest.ContainsKey(MANIFEST_KEY_REQUIRE))
            {
                _require = arrayOrString(manifest[MANIFEST_KEY_REQUIRE]);
            }
            if (manifest.ContainsKey(MANIFEST_KEY_DEPENDENCY))
            {
                _depend = arrayOrString(manifest[MANIFEST_KEY_DEPENDENCY]);
            }
            Root = root;

            Initialize();
        }

        private string[] arrayOrString(JToken token)
        {
            if (token is JArray)
            {
                return ((JArray)token).Select(c => (string)c).ToArray();
            }
            else
            {
                return new string[] { token.ToString() };
            }
        }

        /// <summary>
        /// Functions and values provided by API
        /// </summary>
        private void Initialize()
        {
            // function
            Engine.SetValue("addEventListener", new Action<string, Jint.Native.JsValue>(J_addEventListener));
            Engine.SetValue("getOpenedDocuments", new Func<JSDocumentView.Wrapper[]>(J_getOpenedDocuments));
            Engine.SetValue("getRecentDocuments", new Func<JSDocumentWrapper[]>(() => DocumentInfo.RecentDocs.Select(v => new JSDocumentWrapper(v)).ToArray()));
            Engine.SetValue("download", new Action<string, Jint.Native.JsValue, Jint.Native.JsValue>(J_download));
            Engine.SetValue("httpAsString", new Action<string, Jint.Native.JsValue>(J_httpAsString));
            Engine.SetValue("openDocument", new Action<string>(J_openDocument));
            Engine.SetValue("writeline", new Action<object>(J_writeline));
            Engine.SetValue("showInfoBar", new Action<string, string, string>(J_showInfoBar));

            // field
            Engine.SetValue("window", JSWindow.Instance.GetWrapper());
            Engine.SetValue("plugin", GetWrapper());
        }

        public JSPluginWrapper GetWrapper() => new JSPluginWrapper(this);

        private event EventHandler Unloaded;
        #region JS Functions
        private void J_addEventListener(string eventName, Jint.Native.JsValue callback)
        {
            if (callback == null)
            {
                throw new ArgumentException("callback is null");
            }

            void wrapCallback(params object[] args) => 
                callback.AsCallable().Call(Engine, args);

            void wrapCallbackZeroArgument(object sender, object v) =>
                callback.AsCallable().Call();

            switch (eventName)
            {
                case "documentLoaded":
                    Action<DocumentEventArgs> d = (v) => wrapCallback(new JSDocumentView(this, v.WebView, v.info).GetWrapper());
                    ContentPage.RegisterFor(this, ContentPage.DocumentOpenedForPlugin, d);
                    Unloaded += (s, v) => ContentPage.DocumentOpenedForPlugin.Remove(this);
                    break;
                case "documentClosed":
                    EventHandler<DocumentEventArgs> d2 = (s, v) => wrapCallback(new JSDocumentView(this, v.WebView, v.info).GetWrapper());
                    ContentPage.DocumentClosed += d2;
                    Unloaded += (s, v) => ContentPage.DocumentClosed -= d2;
                    break;
                case "unload":
                    Unloaded += wrapCallbackZeroArgument;
                    break;
                case "systemStartup":
                    CheckRequirement(REQUIRE_STARTUP);
                    PluginManager.OnStartupTaskRecognized += wrapCallbackZeroArgument;
                    Unloaded += (s, v) => PluginManager.OnStartupTaskRecognized -= wrapCallbackZeroArgument;
                    break;
                default:
                    throw new ArgumentException(string.Format("{0} isn't an event name", eventName));
            }
        }

        private JSDocumentView.Wrapper[] J_getOpenedDocuments() => 
            ContentPage.OpenedDocument.Select(p => new JSDocumentView(this, p.Value, p.Key).GetWrapper()).ToArray();

        private List<HttpClient> activeClients = new List<HttpClient>();
        private void J_download(string uri, Jint.Native.JsValue rename = null, Jint.Native.JsValue callback = null)
        {
            string mRename = null;
            Jint.Native.ICallable mCallback = null;
            if (rename != null && rename.IsCallable())
            {
                mCallback = rename.AsCallable();
                mRename = null;
            }
            else if (rename != null && rename.IsString())
            {
                if (callback.IsCallable())
                {
                    mCallback = callback?.AsCallable();
                }
                else
                {
                    mCallback = null;
                }
                mRename = rename.AsString();
            }

            async Task<string> download()
            {
                var client = new HttpClient();
                activeClients.Add(client);
                var result = await client.GetAsync(uri);
                if (!result.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(string.Format("HTTP: {0}", ((int)result.StatusCode)));
                }
                string name = null;
                if (!string.IsNullOrWhiteSpace(mRename))
                {
                    name = mRename;
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
                        var fileNamePrefix = "filename=";
                        name = contentDisposition.FirstOrDefault(v => v.Contains(fileNamePrefix));
                        if (name != null)
                        {
                            var start = name.IndexOf(fileNamePrefix) + fileNamePrefix.Length + 1;
                            name = name.Substring(start).Remove(name.Length - 1);
                        }
                    }
                    if (name == null)
                    {
                        name = Guid.NewGuid().ToString();
                    }
                }
                var file = await DownloadsFolder.CreateFileAsync(name, CreationCollisionOption.GenerateUniqueName);
                using (var fs = await file.OpenAsync(FileAccessMode.ReadWrite))
                using(result)
                using(client)
                {
                    var bf = await result.Content.ReadAsByteArrayAsync();
                    await fs.WriteAsync(CryptographicBuffer.CreateFromByteArray(bf));
                }
                activeClients.Remove(client);
                StorageApplicationPermissions.FutureAccessList.Add(file);
                return file.Path;
            }
            Task.Run(async () =>
            {
                object result = null;
                try
                {
                    result = await download();
                } 
                catch (HttpRequestException ex)
                {
                    var index = ex.Message.IndexOf("HTTP: ");
                    if (index != -1)
                    {
                        result = int.Parse(ex.Message.Substring(6));
                    }
                } finally {
                    mCallback?.Invoke(Engine, result);
                }
            });
        }

        private void J_httpAsString(string url, Jint.Native.JsValue callback)
        {
            if (!callback.IsCallable())
            {
                throw new ArgumentException("callback not a function");
            }

            var client = new HttpClient();
            activeClients.Add(client);

            async Task<string> download()
            {
                var request = await client.GetAsync(url);
                if (!request.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(string.Format("HTTP: {0}", (int) request.StatusCode));
                }
                return await request.Content.ReadAsStringAsync();
            };
            object result = null;
            try
            {
                var task = download();
                task.Wait();
                result = task.Result;
            }
            catch (HttpRequestException ex)
            {
                var index = ex.Message.IndexOf("HTTP: ");
                if (index != -1)
                {
                    result = int.Parse(ex.Message.Substring(6));
                }
            }
            finally
            {
                activeClients.Remove(client);
                callback.AsCallable().Invoke(Engine, result);
            }
        }

        private void J_openDocument(string path)
        {
            var document = DocumentInfo.Parse(path);
            document.Open();
        }

        private void J_writeline(object content) => Debug.WriteLine(content);
        private void J_showInfoBar(string title, string message, string severity = "")
        {
            var severityEnum = muxc.InfoBarSeverity.Informational;
            switch (severity)
            {
                case "error":
                    severityEnum = muxc.InfoBarSeverity.Error;
                    break;
                case "warning":
                    severityEnum = muxc.InfoBarSeverity.Warning;
                    break;
                case "success":
                    severityEnum = muxc.InfoBarSeverity.Success;
                    break;
            }
            _ = MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, 
                () => App.ShowInfoBar(title, message, severityEnum));
        }

        internal Shortcut CreateShortcut(string name, string description, string icon, Jint.Native.JsValue open)
        {
            Action<Action<double>, Action<string>> path;
            if (open.IsCallable())
            {
                path = (p, callback) =>
                {
                    var r = open.AsCallable().Invoke(Engine, p, callback);
                    if (r.IsString())
                    {
                        callback(r.AsString());
                    }
                };
            }
            else if (open.IsString())
            {
                path = (p, callback) => callback(open.AsString());
            }
            else
            {
                throw new ArgumentException();
            }
            var symbol = (Symbol) Enum.Parse(typeof(Symbol), icon, true);
            return new Shortcut(name, description, path, symbol);
        }
        #endregion

        public override string Name => _name;

        public override string Description => _des;

        public override string Author => _author;

        public override string Version => _version;

        public override string[] Requirements => _require;

        public override IPlugin[] Dependencies => _depend.Select(p => PluginManager.Find(p) ?? throw new ArgumentNullException()).ToArray();

        public override async Task OnLoad()
        {
            if (Loaded)
            {
                return;
            }

            var mainFile = await Root.GetFileAsync(PLUGIN_ENTRY_FILE_NAME);
            Engine.Execute(await FileIO.ReadTextAsync(mainFile));

            Loaded = true;
        }

        public override async Task OnUnload()
        {
            if (!Loaded)
            {
                return;
            }
            Loaded = false;
            Unloaded?.Invoke(this, new EventArgs());
            Unloaded = null;
            // cancel downloading tasks
            foreach (var download in activeClients)
            {
                try
                {
                    download.CancelPendingRequests();
                    download.Dispose();
                }
                catch
                {
                }
            }
            activeClients.Clear();

            await MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, Shortcuts.Clear);
        }

        private void CheckRequirement(string key)
        {
            if (!_require.Contains(key))
            {
                throw new InvalidOperationException(string.Format("not requiring {0}", key));
            }
        }

        public const string PLUGIN_MANIFEST_FILE_NAME = "plugin.json";
        public const string PLUGIN_ENTRY_FILE_NAME = "main.js";
        public const string MANIFEST_KEY_NAME = "name";
        public const string MANIFEST_KEY_DESCRIPTION = "description";
        public const string MANIFEST_KEY_AUTHOR = "author";
        public const string MANIFEST_KEY_VERSION = "version";
        public const string MANIFEST_KEY_REQUIRE = "require";
        public const string MANIFEST_KEY_EMBED = "embed";
        public const string MANIFEST_KEY_DEPENDENCY = "dependsOn";
        public static string[] NecessaryManifestOptions => new string[] { MANIFEST_KEY_NAME, MANIFEST_KEY_AUTHOR, MANIFEST_KEY_VERSION };
    }
}
