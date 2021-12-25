using System;
using System.Threading.Tasks;

namespace Hook
{
    internal interface DocumentConvert
    {
        Task Convert(string path, string output);
        Guid GetID();
    }
}
