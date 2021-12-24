using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hook
{
    public class DocumentInfo
    {
        public readonly string Path;
        public byte[] SHA
        {
            private set;
            get;
        }
        public DateTime LastTouched
        { 
            private set; 
            get; 
        }

        public DocumentInfo(string path)
        {
            Path = path;
            LastTouched = DateTime.Now;
        }

        private DocumentInfo(string path, DateTime lastTouched, byte[] sha)
        {
            Path = path;
            LastTouched = lastTouched;
            SHA = sha;
        }

        private static StorageFolder cache = ApplicationData.Current.RoamingFolder;

        public void Open() => MainPage.Instance.OpenDocument(this);

        public async Task<StorageFile> BuildCache()
        {
            var name = GetDesignedCacheName(this) + ".html";
            Thread.Sleep(3000);
            if (new FileInfo(System.IO.Path.Combine(cache.Path, name)).Exists)
            {
                return await cache.GetFileAsync(name);
            }
            var file = await cache.CreateFileAsync(name);
            await FileIO.WriteTextAsync(file, @"
<html>
<body>
<h1>Test Document</h1>
</body>
</html>
");
            return file;
        }

        public static ObservableCollection<DocumentInfo> recentDocs = new ObservableCollection<DocumentInfo>();
        public static readonly StorageFolder saveFolder = ApplicationData.Current.LocalFolder;

        public static async void LoadFromDisk()
        {
            recentDocs.Clear();
            var saves = await saveFolder.GetFilesAsync();
            foreach (var file in saves)
            {
                if (System.IO.Path.GetExtension(file.Name) != ".json")
                {
                    continue;
                }
                try
                {
                    var read = await FileIO.ReadTextAsync(file);
                    var obj = JObject.Parse(read);

                    var instance = new DocumentInfo((string)obj["Path"], (DateTime)obj["LastTouched"], (byte[])obj["SHA"]);
                    recentDocs.Add(instance);
                }
                catch (Exception _)
                {
                }
            }
        }

        private static async void SaveToDesk()
        {
            foreach (var doc in recentDocs)
            {
                var obj = new JObject();
                obj.Add("Path", doc.Path);
                obj.Add("LastTouched", doc.LastTouched);
                obj.Add("SHA", doc.SHA);

                var name = GetDesignedCacheName(doc) + ".json";
                var file = await saveFolder.GetFileAsync(name);
                await FileIO.WriteTextAsync(file, obj.ToString());
            }
        }

        /// <summary>
        /// Result: original name + base64(sha1(doc.Path))
        /// </summary>
        /// <param name="doc">The original one</param>
        /// <returns></returns>
        private static string GetDesignedCacheName(DocumentInfo doc)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(doc.Path);
            using (var sha1 = SHA1.Create())
            {
                byte[] sha = sha1.ComputeHash(Encoding.UTF8.GetBytes(doc.Path));
                name += Convert.ToBase64String(sha).Replace('/', '-');
            }
            return name;
        }
    }
}
