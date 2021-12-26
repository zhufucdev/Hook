using System;
using System.Threading.Tasks;

namespace Hook
{
    internal abstract class DocumentConvert
    {
        public abstract Task Convert(string path, string output);
        public abstract Guid ID { get; }
        public abstract string Name { get; }
        public abstract string Path { get; }
    }
}
