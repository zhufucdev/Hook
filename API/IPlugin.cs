using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hook.API
{
    public abstract class IPlugin
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string Author { get; }
        public abstract string Version { get; }
        public abstract IPlugin[] Dependencies { get; }
        public abstract string[] Requirements { get; }

        public bool Loaded { protected set; get; }

        public abstract Task OnLoad();
        public abstract Task OnUnload();

        public const string REQUIRE_STARTUP = "startWithSystem";
    }
}
