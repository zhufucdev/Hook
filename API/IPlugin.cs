using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hook.API
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        string Author { get; }
        string Version { get; }

        Task OnLoad();
        Task OnUnload();
    }
}
