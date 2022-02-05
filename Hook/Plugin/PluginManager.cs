using Hook.API;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.AccessCache;
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
        private static async Task<IPlugin> CreateInstance(StorageFolder root, bool sideload = false)
        {
            var manifestFile = await root.TryGetItemAsync(JSPlugin.PLUGIN_MANIFEST_FILE_NAME);
            if (manifestFile == null || !(manifestFile is IStorageFile))
            {
                throw new ArgumentException(Utility.GetResourceString("Exception/Manifest"));
            }

            var manifest = JObject.Parse(await FileIO.ReadTextAsync(manifestFile as IStorageFile));
            var plugin = sideload ? new SideloadedJSPlugin(manifest, root) : new JSPlugin(manifest, root);

            if (manifest.ContainsKey(JSPlugin.MANIFEST_KEY_REQUIRE))
            {
                var token = manifest[JSPlugin.MANIFEST_KEY_REQUIRE];
                var isArray = token is JArray;
                bool requires(string t) => isArray
                    && token.Select(v => (string)v).Contains(t)
                    || token.ToString() == t;

                if (requires(IPlugin.REQUIRE_STARTUP))
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

            return plugin;
        }

        public static async Task Load(IPlugin plugin)
        {
            var notLoaded = "";
            try
            {
                foreach (var p in plugin.Dependencies)
                {
                    if (!p.Loaded)
                    {
                        notLoaded += p.Name + ", ";
                    }
                }
            }
            catch (ArgumentNullException)
            {
                if (plugin is JSPlugin)
                {
                    foreach (var name in (plugin as JSPlugin).Dependencies)
                    {
                        notLoaded += name + ", ";
                    }
                }
                else
                {
                    notLoaded = "?";
                }
            }
            if (!string.IsNullOrEmpty(notLoaded))
            {
                notLoaded = notLoaded.Remove(notLoaded.Length - 2);
                App.ShowInfoBar(
                    Utility.GetResourceString("PlugInLoadDependNotFound/Title"),
                    Utility.GetResourceString("PlugInLoadDependNotFound/Message").Replace("%s%", plugin.Name).Replace("%d%", notLoaded),
                    Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning);
                return;
            }
            try
            {
                await plugin.OnLoad();
            }
            catch (Exception ex)
            {
                _ = ReportPluginFailure(ex, plugin);
            }
        }

        public static async Task ReportPluginFailure(Exception ex, IPlugin plugin)
        {
            await MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                App.ShowInfoBar(
                    Utility.GetResourceString("PlugInFailure/Title").Replace("%s", plugin.Name),
                    string.Format("{0}: {1}", ex.GetType().Name, ex.Message),
                    Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error
                )
            );
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
            var plugins = (await Installation.GetFoldersAsync()).ToList();
            foreach (var plugin in plugins)
            {
                var instance = await CreateInstance(plugin);
                Plugins.Add(instance);
            }
            // load sideloaders only if developer mode is on
            if (Utility.DeveloperMode)
            {
                var sideloaders = Utility.Sideloaders.ToList();
                foreach (var sideload in Utility.Sideloaders)
                {
                    StorageFolder folder;
                    try
                    {
                        folder = await StorageFolder.GetFolderFromPathAsync(sideload);
                    }
                    catch
                    {
                        // delete redundance
                        sideloaders.Remove(sideload);
                        continue;
                    }
                    var instance = await CreateInstance(folder, sideload: true);
                    Plugins.Add(instance);
                }
                if (Utility.Sideloaders.Count() > sideloaders.Count)
                {
                    Utility.Sideloaders = sideloaders.ToArray();
                }
            }

            LoadAll();
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

            var plugin = await CreateInstance(folder);
            Plugins.Add(plugin);
            await Load(plugin);
        }

        public static async Task Sideload(StorageFolder folder)
        {
            if (Utility.Sideloaders.Contains(folder.Path))
            {
                return;
            }
            
            var plugin = await CreateInstance(folder, sideload: true);
            Plugins.Add(plugin);
            await Load(plugin);

            StorageApplicationPermissions.FutureAccessList.Add(folder);
            Utility.Sideloaders = Utility.Sideloaders.Append(folder.Path).ToArray();
        }

        public static async Task Uninstall(IPlugin plugin)
        {
            await plugin.OnUnload();
            Plugins.Remove(plugin);

            await plugin.Uninstall();
        }

        public static void UnloadAll()
        {
            foreach (var plugin in Plugins)
            {
                try
                {
                    using (var unloadTask = plugin.OnUnload())
                    {
                        unloadTask.Wait();
                    }
                }
                catch
                {
                }
            }
        }

        public static void UseWithDependency(Action<IPlugin> action, bool useCache = true)
        {
            void useRecursively(PluginDependency relation)
            {
                action(relation.Parent);
                foreach (var d in relation.Children)
                {
                    var resurse = PluginDependency.Instances.ContainsKey(d);
                    if (resurse)
                    {
                        useRecursively(PluginDependency.Instances[d]);
                    }
                    else
                    {
                        action(d);
                    }
                }
            }
            List<IPlugin> rootPlugins; // plugins that should be loaded first
            EnsureDependencyStructure(useCache, out rootPlugins);

            foreach (var plugin in rootPlugins)
            {
                var relation = PluginDependency.Parse(plugin);
                useRecursively(relation);
            }
        }

        private static void EnsureDependencyStructure(bool useCache, out List<IPlugin> rootPlugins)
        {
            if (!useCache && PluginDependency.IsRelationStructureBuilt)
            {
                PluginDependency.Instances.Clear();
            }

            rootPlugins = new List<IPlugin>();
            foreach (var plugin in Plugins)
            {
                IPlugin[] dependsOn = Array.Empty<IPlugin>();
                try
                {
                    dependsOn = plugin.Dependencies;
                }
                catch (ArgumentNullException)
                {
                }
                if (dependsOn.Count() > 0)
                {
                    if (!PluginDependency.IsRelationStructureBuilt)
                    {
                        foreach (var depeneded in dependsOn)
                        {
                            var relation = PluginDependency.Parse(depeneded);
                            relation.Children.Add(plugin);
                        }
                    }
                }
                else
                {
                    rootPlugins.Add(plugin);
                }
            }
            PluginDependency.IsRelationStructureBuilt = true;
        }

        public static void LoadAll(bool useCache = true)
        {
            Task lastTask = null;
            UseWithDependency(p =>
            {
                if (lastTask != null)
                {
                    lastTask.ContinueWith(t => Load(p));
                }
                else
                {
                    lastTask = Load(p);
                }
            }, useCache);
        }

        public static IPlugin Find(string name) => Plugins.FirstOrDefault(x => x.Name == name);

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

        class PluginDependency
        {
            public IPlugin Parent;
            public List<IPlugin> Children;

            private PluginDependency(IPlugin parent)
            {
                Parent = parent;
                Children = new List<IPlugin>();
            }

            public static readonly Dictionary<IPlugin, PluginDependency> Instances = new Dictionary<IPlugin, PluginDependency>();
            public static bool IsRelationStructureBuilt = false;
            public static PluginDependency Parse(IPlugin depended)
            {
                if (Instances.ContainsKey(depended))
                {
                    return Instances[depended];
                }
                else
                {
                    var d = new PluginDependency(depended);
                    Instances[depended] = d;
                    return d;
                }
            }
        }
    }
}
