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
            var manifestFile = await root.GetFileAsync(JSPlugin.PluginManifestFileName);
            var manifest = JObject.Parse(await FileIO.ReadTextAsync(manifestFile));
            var plugin = new JSPlugin(manifest, root);

            if (manifest.ContainsKey("require"))
            {
                var requirements = manifest["require"].Select(v => (string)v);
                if (requirements.Contains("startWithSystem"))
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

            plugin.OnLoad();
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

        public static async void Install(StorageFile file)
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
                if (entry.Name == JSPlugin.PluginManifestFileName)
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
            string name = (string)manifestJson["name"];

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
                                    .Replace("%new_author%", (string)manifestJson["author"])
                                    .Replace("%old_version%", oldPlugin.Version)
                                    .Replace("%new_version%", (string)manifestJson["version"]),
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
                zip.ExtractToDirectory(cache.Path);
                async void test(StorageFolder root)
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
                            async Task<StorageFolder> testParent(string item)
                            {
                                var suffix = Path.GetRelativePath(item, root.Path);
                                var full = Path.Combine(Installation.Path, name, suffix);
                                
                                try
                                {
                                    return await StorageFolder.GetFolderFromPathAsync(full);
                                }
                                catch
                                {
                                    var index = item.LastIndexOf('\\');
                                    if (index == -1)
                                    {
                                        index = 0;
                                    }
                                    var parentPath = item.Substring(0, index);
                                    await (await testParent(parentPath)).CreateFolderAsync(Path.GetDirectoryName(item));
                                    return await StorageFolder.GetFolderFromPathAsync(full);
                                }
                            }

                            var dest = await testParent((await piece.GetParentAsync()).Path);

                            await piece.MoveAsync(dest);
                        }
                    } 
                    else if (s.Count != 1)
                    {
                        throw new ArgumentException(string.Format("{0} is not a plugin structure", file.Name));
                    }
                    if (s.Count == 1 && p.Count == 0)
                    {
                        test(root);
                    }
                }
                test(cache);
            }

            var plugin = await Load(folder);
            Plugins.Add(plugin);
        }

        public static async Task Uninstall(IPlugin plugin)
        {
            if (plugin is JSPlugin)
            {
                plugin.OnUnload();
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

        private static bool denialInfoShown = false;
        private static void ShowStartupDenialInfo()
        {
            if (denialInfoShown)
            {
                return;
            }
            denialInfoShown = true;

            App.ShowInfoBar(
                title: Utility.GetResourceString("StartupDeny/Title"),
                message: Utility.GetResourceString("StartupDeny/Message"),
                severity: Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning
            );
        }
    }
}
