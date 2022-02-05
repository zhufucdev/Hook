using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Core;

namespace Hook.Plugin
{
    internal class SideloadedJSPlugin : JSPlugin
    {
        private StorageFileQueryResult query;
        public SideloadedJSPlugin(JObject manifest, StorageFolder root) : base(manifest, root)
        {
            query = root.CreateFileQuery();
            query.GetFilesAsync();
            query.ContentsChanged += Query_ContentsChangedAsync;
        }

        private DateTime lastReload = DateTime.Now;
        private async void Query_ContentsChangedAsync(IStorageQueryResultBase sender, object args)
        {
            if (DateTime.Now - lastReload > TimeSpan.FromSeconds(2))
            {
                await query.GetFilesAsync();
                await OnUnload();
                await PluginManager.Load(this);
                lastReload = DateTime.Now;
            }
        }

        public override async Task Uninstall()
        {
            var list = Utility.Sideloaders.ToList();
            list.Remove(Root.Path);
            Utility.Sideloaders = list.ToArray();
        }
    }
}
