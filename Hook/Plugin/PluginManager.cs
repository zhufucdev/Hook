using Hook.API;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Xaml.Controls;

namespace Hook.Plugin
{
    internal class PluginManager
    {
        public static ObservableCollection<IPlugin> Plugins = new ObservableCollection<IPlugin>();
        public static StorageFolder Installation;

        public static readonly string[] SupportedFormats = { ".hplugin" };

        private static StartupTask startupTask;
        private static async Task<IPlugin> Load(StorageFolder root)
        {
            var manifestFile = await root.GetFileAsync(JSPlugin.PLUGIN_MANIFEST_FILE_NAME);
            var manifest = JObject.Parse(await FileIO.ReadTextAsync(manifestFile));
            var plugin = new JSPlugin(manifest, root);

            if (manifest.ContainsKey(JSPlugin.MANIFEST_KEY_REQUIRE))
            {
                var token = manifest[JSPlugin.MANIFEST_KEY_REQUIRE];
                var isArray = token is JArray;
                bool requires(string t) => isArray
                    && token.Select(v => (string)v).Contains(t)
                    || token.ToString() == t;

                if (requires(JSPlugin.REQUIRE_STARTUP))
                {
                    var state = startupTask.State;
                    if (state == StartupTaskState.Disabled)
                    {
                        // task is disabled but can be enabled
                        var newState = await startupTask.RequestEnableAsync();
                        if (newState != StartupTaskState.Enabled)
                        {
                            ShowStartupDenialInfo();
                        }
                    }
                    else if (state == StartupTaskState.DisabledByUser)
                    {
                        ShowStartupDenialInfo();
                    }
                }
            }

            try
            {
                await plugin.OnLoad();
            }
            catch (Exception ex)
            {
                await MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    App.ShowInfoBar(
                        Utility.GetResourceString("PlugInFailure/Title").Replace("%s", plugin.Name),
                        string.Format("{0}: {1}", ex.GetType().Name, ex.Message),
                        Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error
                    )
                );
            }
            return plugin;
        }

        public static async Task Initialize()
        {
            startupTask = await StartupTask.GetAsync("PluginStartUp");

            var local = ApplicationData.Current.LocalFolder;
            var ins = await local.TryGetItemAsync("plugins");
            if (ins != null && !(ins is StorageFolder))
            {
                await ins.DeleteAsync();
                ins = null;
            }
            if (ins == null)
            {
                ins = await local.CreateFolderAsync("plugins");
            }
            Installation = ins as StorageFolder;

            // load all installed
            var plugins = await Installation.GetFoldersAsync();
            foreach (var plugin in plugins)
            {
                var instance = await Load(plugin);
                Plugins.Add(instance);
            }
        }

        public static async Task Install(StorageFile file)
        {
            Stream stream = await file.OpenStreamForReadAsync();
            // unzip
            ZipArchive zip = new ZipArchive(stream);
            Stream manifest = null;
            var directUnzip = false;
            foreach (var entry in zip.Entries)
            {
                if (!entry.FullName.Contains("/"))
                {
                    directUnzip = true;
                }
                if (entry.Name == JSPlugin.PLUGIN_MANIFEST_FILE_NAME)
                {
                    manifest = entry.Open();
                    if (directUnzip)
                    {
                        break;
                    }
                }
            }

            if (manifest == null)
            {
                throw new ArgumentException(string.Format("{0} doesn't contain plugin.json", file.Name));
            }

            var manifestJson = JObject.Parse(new StreamReader(manifest).ReadToEnd());
            if (JSPlugin.NecessaryManifestOptions.Any(v => !manifestJson.ContainsKey(v)))
            {
                throw new ArgumentException(string.Format("{0} doesn't contain necessary info in manifest", file.Name));
            }

            string name = (string)manifestJson[JSPlugin.MANIFEST_KEY_NAME];

            async Task<bool> testExisting()
            {
                var item = await Installation.TryGetItemAsync(name);
                if (item == null || !(item is IStorageFolder))
                {
                    return true;
                }
                var oldPlugin = Plugins.FirstOrDefault(p => p.Name == name);

                var dialog = new ContentDialog()
                {
                    Title = Utility.GetResourceString("ReplacePlugin/Title"),
                    Content = Utility.GetResourceString("ReplacePlugin/Content")
                                    .Replace("%name%", name)
                                    .Replace("%old_author%", oldPlugin.Author)
                                    .Replace("%new_author%", (string)manifestJson[JSPlugin.MANIFEST_KEY_AUTHOR])
                                    .Replace("%old_version%", oldPlugin.Version)
                                    .Replace("%new_version%", (string)manifestJson[JSPlugin.MANIFEST_KEY_VERSION]),
                    PrimaryButtonText = Utility.GetResourceString("ReplaceButton/Text"),
                    SecondaryButtonText = Utility.GetResourceString("CloseButton/Text")
                };
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await Uninstall(oldPlugin);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            if (!await testExisting())
            {
                return;
            }
            var folder = await Installation.CreateFolderAsync(name);
            if (directUnzip)
            {
                zip.ExtractToDirectory(folder.Path);
            }
            else
            {
                var cache = await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync(name);
                async Task extractAndMove()
                {
                    zip.ExtractToDirectory(cache.Path);
                    async Task test(StorageFolder root)
                    {
                        var s = await root.GetFoldersAsync();
                        var p = await root.GetFilesAsync();
                        if (p.Count > 0)
                        {
                            // this is the desired root folder
                            var files = await root.CreateFileQueryWithOptions(new QueryOptions() { FolderDepth = FolderDepth.Deep }).GetFilesAsync();
                            // move it to installation folder
                            foreach (var piece in files)
                            {
                                string parentOf(string item)
                                {
                                    var index = item.LastIndexOf('\\');
                                    if (index == -1)
                                    {
                                        index = 0;
                                    }
                                    return item.Substring(0, index);
                                }

                                async Task<StorageFolder> testParent(string item)
                                {
                                    var suffix = Path.GetRelativePath(item, root.Path).Replace(".", string.Empty);
                                    var full = Path.Combine(Installation.Path, name, suffix);

                                    try
                                    {
                                        return await StorageFolder.GetFolderFromPathAsync(full);
                                    }
                                    catch
                                    {
                                        var parentPath = parentOf(item);
                                        await (await testParent(parentPath)).CreateFolderAsync(Path.GetDirectoryName(item));
                                        return await StorageFolder.GetFolderFromPathAsync(full);
                                    }
                                }

                                var dest = await testParent(parentOf(piece.Path));

                                await piece.MoveAsync(dest);
                            }
                        }
                        else if (s.Count != 1)
                        {
                            throw new ArgumentException(string.Format("{0} is not a plugin structure", file.Name));
                        }
                        if (s.Count == 1 && p.Count == 0)
                        {
                            await test(s.First());
                        }
                    }
                    await test(cache);
                    await cache.DeleteAsync();
                }

                try
                {
                    await extractAndMove();
                }
                catch (Exception ex)
                {
                    await folder.DeleteAsync();
                    await cache.DeleteAsync();
                    throw ex;
                }
            }

            var plugin = await Load(folder);
            Plugins.Add(plugin);
        }

        public static async Task Uninstall(IPlugin plugin)
        {
            if (plugin is JSPlugin)
            {
                await plugin.OnUnload();
                Plugins.Remove(plugin);

                var folder = await Installation.GetFolderAsync(plugin.Name);
                await folder.DeleteAsync();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static void UnloadAll()
        {
            foreach (var plugin in Plugins)
            {
                plugin.OnUnload();
            }
        }

        public static event EventHandler OnStartupTaskRecognized;
        public static void RecognizeStartupTask()
        {
            OnStartupTaskRecognized?.Invoke(MainPage.Instance, new EventArgs());
        }

        private static bool denialInfoShown = false;
        private static async void ShowStartupDenialInfo()
        {
            if (denialInfoShown)
            {
                return;
            }
            denialInfoShown = true;
            await MainPage.Instance.Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Low,
                () => App.ShowInfoBar(
                title: Utility.GetResourceString("StartupDeny/Title"),
                message: Utility.GetResourceString("StartupDeny/Message"),
                severity: Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning
            ));
        }
    }
}
