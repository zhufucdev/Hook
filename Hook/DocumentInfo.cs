using Hook.API;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace Hook
{
    public class DocumentInfo : IDocument
    {
        private readonly string _path;
        public byte[] SHA
        {
            private set;
            get;
        }
        private DateTime _lastTouched;
        public Guid CacheMethod
        {
            private set;
            get;
        }

        public string Path => _path;

        public DateTime LastTouched => _lastTouched;

        public string Name => System.IO.Path.GetFileName(_path);


        private DocumentInfo(string path)
        {
            _path = path;
            _lastTouched = DateTime.Now;
        }

        private DocumentInfo(string path, DateTime lastTouched, byte[] sha, Guid cacheMethod)
        {
            _path = path;
            _lastTouched = lastTouched;
            SHA = sha;
            CacheMethod = cacheMethod;
        }

        public static StorageFolder Cache {
            get => ApplicationData.Current.LocalCacheFolder;
        }

        public void Open()
        {
            if (!SupportedFormats.Contains(System.IO.Path.GetExtension(Path).ToLower()))
            {
                throw new NotSupportedException();
            }
            _lastTouched = DateTime.Now;
            _ = MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => {
                MainPage.Instance.OpenDocument(this);

                // ensure this appears first in the list
                int index = RecentDocs.IndexOf(this);
                if (index != -1)
                {
                    RecentDocs.Move(index, 0);
                }
                else
                {
                    RecentDocs.Insert(0, this);
                }
            });

            Sync(this);
        }

        public async Task<StorageFile> BuildCache()
        {
            var name = GetDesignedCacheName(this) + ".html";
            var outPath = System.IO.Path.Combine(Cache.Path, name);
            var coverter = Utility.DefaultConverter;

            byte[] currentSHA = new byte[0];
            try
            {
                currentSHA = await CalcuateSHA(this);
            }
            catch (Exception)
            {
            }

            if (SHA == null || CacheMethod != coverter.ID || !currentSHA.SequenceEqual(SHA))
            {
                // cache method or origin file has changed, cleanbuild
                await coverter.Convert(Path, outPath);
                CacheMethod = coverter.ID;
                SHA = currentSHA;
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
            var list = new List<DocumentInfo>();
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
                    list.Add(instance);
                }
                catch (Exception)
                {
                }
            }
            list.Sort((c1, c2) => c2.LastTouched - c1.LastTouched > TimeSpan.Zero ? 1 : -1);
            foreach (var doc in list)
            {
                RecentDocs.Add(doc);
            }

            RecentDocs.CollectionChanged += RecentDocs_CollectionChanged;
        }


        private static List<DocumentInfo> Removed = new List<DocumentInfo>();
        private static void RecentDocs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (DocumentInfo doc in e.OldItems) {
                    Removed.Add(doc);
                }
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                ClearFromDisk();
            }
        }

        public static async void SaveToDisk()
        {
            foreach (var doc in RecentDocs)
            {
                Sync(doc);
            }
            foreach (var doc in Removed)
            {
                var name = GetDesignedCacheName(doc) + ".json";
                var file = await SaveFolder.TryGetItemAsync(name);
                if (file != null && file is StorageFile)
                {
                    await file.DeleteAsync();
                    
                }
            }
            Removed.Clear();
        }

        public static async void ClearFromDisk()
        {
            var files = await SaveFolder.GetFilesAsync();
            foreach (var file in files)
            {
                if (file.Name.EndsWith(".json"))
                {
                    await file.DeleteAsync();
                }
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

        public static DocumentInfo Parse(IStorageFile file)
        {
            StorageApplicationPermissions.FutureAccessList.Add(file);
            return Parse(file.Path);
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

        /// <summary>
        /// Calculate the checksum of a document from filesystem
        /// </summary>
        /// <param name="doc">The doc</param>
        /// <returns>Checksum via SHA256</returns>
        private static async Task<byte[]> CalcuateSHA(DocumentInfo doc)
        {
            byte[] result = null;
            var file = await StorageFile.GetFileFromPathAsync(doc.Path);
            using (var sha256 = SHA256.Create())
            {
                using (var stream = await file.OpenStreamForReadAsync())
                {
                    result = sha256.ComputeHash(stream);
                }
            }
            return result;
        }

        public static string[] SupportedFormats = { ".docx", ".doc" };
    }
}
