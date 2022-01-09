using Hook.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hook.Plugin
{
    public class PluginEventCollection<T> : Dictionary<IPlugin, List<Action<T>>>
    {
    }
}
