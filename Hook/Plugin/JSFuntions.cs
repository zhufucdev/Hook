using Jint;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    internal class JSFuntions
    {
        public readonly JSPlugin Plugin;
        private Engine Engine => Plugin.Engine;
        public JSFuntions(JSPlugin parent)
        {
            Plugin = parent;
        }

        public void Initialize()
        {
            Engine.SetValue("addEventListener", new Action<string, Jint.Native.JsValue>(J_addEventListener));
            Engine.SetValue("getOpenedDocuments", new Func<JSDocumentView.Wrapper[]>(J_getOpenedDocuments));
            Engine.SetValue("getRecentDocuments", new Func<JSDocumentWrapper[]>(() => DocumentInfo.RecentDocs.Select(v => new JSDocumentWrapper(v)).ToArray()));
            Engine.SetValue("download", new Action<string, Jint.Native.JsValue, Jint.Native.JsValue>(J_download));
            Engine.SetValue("httpAsString", new Action<string, Jint.Native.JsValue>(J_httpAsString));
            Engine.SetValue("openDocument", new Action<string>(J_openDocument));
            Engine.SetValue("writeline", new Action<object>(J_writeline));
            Engine.SetValue("showInfoBar", new Action<string, string, string>(J_showInfoBar));

            Plugin.Unloaded += Plugin_Unloaded;
        }

        private void Plugin_Unloaded(object sender, EventArgs e)
        {
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

            _ = MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, Plugin.Shortcuts.Clear);
        }

        private void CheckRequirement(string key)
        {
            if (!Plugin.Requirements.Contains(key))
            {
                throw new InvalidOperationException(string.Format("not requiring {0}", key));
            }
        }

        #region JS Functions
        private void J_addEventListener(string eventName, Jint.Native.JsValue callback)
        {
            if (callback == null)
            {
                throw new ArgumentException("callback is null");
            }

            void wrapCallback(params object[] args)
            {
                try
                {
                    callback.AsCallable().Call(Engine, args);
                }
                catch (Exception ex)
                {
                    _ = PluginManager.ReportPluginFailure(ex, Plugin);
                }
            }

            void wrapCallbackZeroArgument(object sender, object v)
            {
                try
                {
                    callback.AsCallable().Call();
                }
                catch (Exception ex)
                {
                    _ = PluginManager.ReportPluginFailure(ex, Plugin);
                }
            }

            switch (eventName)
            {
                case "documentLoaded":
                    Action<DocumentEventArgs> d = (v) => wrapCallback(new JSDocumentView(Plugin, v.WebView, v.info).GetWrapper());
                    ContentPage.RegisterFor(Plugin, ContentPage.DocumentOpenedForPlugin, d);
                    Plugin.Unloaded += (s, v) => ContentPage.DocumentOpenedForPlugin.Remove(Plugin);
                    break;
                case "documentClosed":
                    EventHandler<DocumentEventArgs> d2 = (s, v) => wrapCallback(new JSDocumentView(Plugin, v.WebView, v.info).GetWrapper());
                    ContentPage.DocumentClosed += d2;
                    Plugin.Unloaded += (s, v) => ContentPage.DocumentClosed -= d2;
                    break;
                case "settingsChanged":
                    EventHandler<JSSettings.SettingsEventArgs> d3 = (s, v) => wrapCallback(v.GetWrapper(s.GetType().Name.ToLower()));
                    Plugin.SettingsContainer.SettingsChanged += d3;
                    Plugin.Unloaded += (s, v) => Plugin.SettingsContainer.SettingsChanged -= d3;
                    break;
                case "unload":
                    Plugin.Unloaded += wrapCallbackZeroArgument;
                    break;
                case "systemStartup":
                    CheckRequirement(API.IPlugin.REQUIRE_STARTUP);
                    PluginManager.OnStartupTaskRecognized += wrapCallbackZeroArgument;
                    Plugin.Unloaded += (s, v) => PluginManager.OnStartupTaskRecognized -= wrapCallbackZeroArgument;
                    break;
                default:
                    throw new ArgumentException(string.Format("{0} isn't an event name", eventName));
            }
        }

        private JSDocumentView.Wrapper[] J_getOpenedDocuments() =>
            ContentPage.OpenedDocument.Select(p => new JSDocumentView(Plugin, p.Value, p.Key).GetWrapper()).ToArray();

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
                using (result)
                using (client)
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
                catch (System.Net.Http.HttpRequestException ex)
                {
                    var index = ex.Message.IndexOf("HTTP: ");
                    if (index != -1)
                    {
                        result = int.Parse(ex.Message.Substring(6));
                    }
                }
                finally
                {
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
                    throw new System.Net.Http.HttpRequestException(string.Format("HTTP: {0}", (int)request.StatusCode));
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
            var symbol = (Symbol)Enum.Parse(typeof(Symbol), icon, true);
            return new Shortcut(name, description, path, symbol);
        }
        #endregion

    }
}
