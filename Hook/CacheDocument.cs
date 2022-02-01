using Hook.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hook
{
    public abstract class CacheDocument : IDocument
    {
        public abstract string Name { get; }
        public abstract string Description { get; }

        public abstract void Open();

        public abstract Task<StorageFile> BuildCache();
    }
}
