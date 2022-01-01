using System;
using System.Threading.Tasks;

namespace Hook.API
{
    public interface DocumentView
    {
        Task<string> RunScript(string script);
        double ZoomFactor { get; set; }
        Uri Source { get; }
        void Close();
    }
}
