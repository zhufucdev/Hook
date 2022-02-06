using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;

namespace Hook
{
    internal class WordInteropConverter : DocumentConvert
    {
        public override Guid ID => Guid.Parse("b69b3d06-8745-11ec-a6ec-00155dd9bc81");

        public override string Name => Utility.GetResourceString("WordInteropConverter/Name");

        public override string Path => "Microsoft Word";

        public override async Task Convert(string path, string output)
        {
            ApplicationData.Current.LocalSettings.Values[KEY_TARGET_PATH] = path;
            ApplicationData.Current.LocalSettings.Values[KEY_OUTPUT_PATH] = output;
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();

            // the cache file shall appear soon
            var outName = System.IO.Path.GetFileName(output);
            while (true)
            {
                var r = await LocalDocument.Cache.TryGetItemAsync(outName);
                if (r != null)
                {
                    break;
                }
                await Task.Delay(100);
            }
        }

        public const string KEY_TARGET_PATH = "WICTargetPath";
        public const string KEY_OUTPUT_PATH = "WICOutputPath";
    }
}
