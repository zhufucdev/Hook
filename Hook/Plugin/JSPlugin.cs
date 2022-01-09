﻿using Hook.API;
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
            Engine.SetValue("getOpenedDocuments", new Func<JSDocumentView[]>(J_getOpenedDocuments));
            Engine.SetValue("getRecentDocuments", new Func<IDocument[]>(() => DocumentInfo.RecentDocs.ToArray()));
            Engine.SetValue("download", new Action<string, Jint.Native.JsValue, Jint.Native.JsValue>(J_download));
            Engine.SetValue("openDocument", new Action<string>(J_openDocument));

            // field
            Engine.SetValue("window", JSWindow.Instance);
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
                    Action<DocumentEventArgs> d = (v) => wrapCallback(new JSDocumentView(this, v.WebView, v.DocumentInfo));
                    ContentPage.RegisterFor(this, ContentPage.DocumentOpenedForPlugin, d);
                    Unloaded += (s, v) => ContentPage.DocumentOpenedForPlugin.Remove(this);
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
                    CheckRequirement(REQUIRE_STARTUP);
                    PluginManager.OnStartupTaskRecognized += wrapCallbackZeroArgument;
                    Unloaded += (s, v) => PluginManager.OnStartupTaskRecognized -= wrapCallbackZeroArgument;
                    break;
                default:
                    throw new ArgumentException(string.Format("{0} isn't an event name", eventName));
            }
        }

        private JSDocumentView[] J_getOpenedDocuments() => 
            ContentPage.OpenedDocument.Select(p => new JSDocumentView(this, p.Value, p.Key)).ToArray();

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
                var result = await client.GetAsync(uri);
                if (!result.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(string.Format("HTTP: {0}", result.StatusCode));
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
                {
                    var bf = await result.Content.ReadAsByteArrayAsync();
                    await fs.WriteAsync(CryptographicBuffer.CreateFromByteArray(bf));
                }
                StorageApplicationPermissions.FutureAccessList.Add(file);
                return file.Path;
            }
            Task.Run(async () =>
            {
                var path = await download();
                mCallback?.Call(new Jint.Native.JsValue[] { Jint.Native.JsValue.FromObject(Engine, path) });
            });
        }

        private void J_openDocument(string path)
        {
            var document = DocumentInfo.Parse(path);
            document.Open();
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
            Unloaded?.Invoke(this, new EventArgs());
            Unloaded = null;
            Loaded = false;
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
