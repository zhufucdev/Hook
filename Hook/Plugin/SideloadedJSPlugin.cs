using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
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
            if (DateTime.Now - lastReload > TimeSpan.FromSeconds(1))
            {
                await query.GetFilesAsync();
                await OnUnload();
                await PluginManager.Load(this);
                lastReload = DateTime.Now;
            }
        }
    }
}
