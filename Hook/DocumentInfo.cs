using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
        public Guid CacheMethod
        {
            private set;
            get;
        }

        public string Name
        {
            get => System.IO.Path.GetFileNameWithoutExtension(Path);
        }

        private DocumentInfo(string path)
        {
            Path = path;
            LastTouched = DateTime.Now;
        }

        private DocumentInfo(string path, DateTime lastTouched, byte[] sha, Guid cacheMethod)
        {
            Path = path;
            LastTouched = lastTouched;
            SHA = sha;
            CacheMethod = cacheMethod;
        }

        private static StorageFolder cache {
            get => ApplicationData.Current.RoamingFolder;
        }

        public void Open()
        {
            if (!SupportedFormats.Contains(System.IO.Path.GetExtension(Path).ToLower()))
            {
                throw new NotSupportedException();
            }
            MainPage.Instance.OpenDocument(this);
            LastTouched = DateTime.Now;
            
            RecentDocs.Remove(this);
            RecentDocs.Add(this);
            Sync(this);
        }

        public async Task<StorageFile> BuildCache()
        {
            var name = GetDesignedCacheName(this) + ".html";
            var outPath = System.IO.Path.Combine(cache.Path, name);

            var coverter = Utility.Converter;
            await coverter.Convert(Path, outPath);

            if (CacheMethod != coverter.GetID())
            {
                // sync if cache method has change
                CacheMethod = coverter.GetID();
                Sync(this);
            }

            var file = await StorageFile.GetFileFromPathAsync(outPath);
            return file;
        }

        public static ObservableCollection<DocumentInfo> RecentDocs = new ObservableCollection<DocumentInfo>();
        public static StorageFolder SaveFolder {
            get => ApplicationData.Current.LocalFolder;
        }

        public static async void LoadFromDisk()
        {
            var saves = await SaveFolder.GetFilesAsync();
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

                    var tmp = obj["SHA"];
                    byte[] SHA = null;
                    if (tmp != null && tmp is byte[])
                    {
                        SHA = (byte[])tmp;
                    }
                    var instance = new DocumentInfo((string)obj["Path"], (DateTime)obj["LastTouched"], SHA, (Guid)obj["CacheMethod"]);
                    RecentDocs.Add(instance);
                }
                catch (Exception)
                {
                }
            }
        }

        public static void SaveToDesk()
        {
            foreach (var doc in RecentDocs)
            {
                Sync(doc);
            }
        }

        private static async void Sync(DocumentInfo doc)
        {
            var obj = new JObject();
            obj.Add("Path", doc.Path);
            obj.Add("LastTouched", doc.LastTouched);
            obj.Add("SHA", doc.SHA);
            obj.Add("CacheMethod", doc.CacheMethod);

            var name = GetDesignedCacheName(doc) + ".json";
            var file = await SaveFolder.TryGetItemAsync(name);
            if (file == null)
            {
                file = await SaveFolder.CreateFileAsync(name);
            }
            await FileIO.WriteTextAsync(file as StorageFile, obj.ToString());
        }

        public static DocumentInfo Parse(string path) => 
            RecentDocs.FirstOrDefault((e) => e.Path == path) 
            ?? new DocumentInfo(path);

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

        public static string[] SupportedFormats = { ".docx", ".doc" };
    }
}
