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
using Windows.Storage;
using Windows.Storage.Search;

namespace Hook.Plugin
{
    internal class PluginManager
    {
        public static ObservableCollection<IPlugin> Plugins = new ObservableCollection<IPlugin>();
        public static StorageFolder Installation;

        public static readonly string[] SupportedFormats = { ".hplugin" };

        private static async Task<IPlugin> Load(StorageFolder root)
        {
            var manifestFile = await root.GetFileAsync(JSPlugin.PluginManifestFileName);
            var manifest = JObject.Parse(await FileIO.ReadTextAsync(manifestFile));
            var plugin = new JSPlugin(manifest, root);
            plugin.OnLoad();

            return plugin;
        }

        public static async void Initialize()
        {
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

        public static async void Uninstall(IPlugin plugin)
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
    }
}
