using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public ObservableCollection<IDocument> Shortcuts = new ObservableCollection<IDocument>();
        public ObservableCollection<ISettingsItem> Settings = new ObservableCollection<ISettingsItem>();

        public bool Loaded { protected set; get; }

        public abstract Task OnLoad();
        public abstract Task OnUnload();
        public abstract Task Uninstall();

        public const string REQUIRE_STARTUP = "startWithSystem";
    }
}
