using Hook.API;
using Hook.Plugin.Interpret;
using Jint;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hook.Plugin
{
    internal class JSPlugin : IPlugin
    {
        private readonly string _name, _des, _author, _version;
        public readonly string[] Embedded, _depend = new string[0];
        private readonly string[] _require = new string[0];

        public readonly Engine Engine = new Engine();
        public readonly StorageFolder Root;
        public readonly JSSettings SettingsContainer;
        public readonly JSFuntions FunctionsContainer;
        public JSPlugin(JObject manifest, StorageFolder root)
        {
            _name = (string)manifest[MANIFEST_KEY_NAME];
            if (manifest.ContainsKey(MANIFEST_KEY_DESCRIPTION))
            {
                _des = (string)manifest[MANIFEST_KEY_DESCRIPTION];
            }
            else
            {
                _des = null;
            }
            _author = (string)manifest[MANIFEST_KEY_AUTHOR];
            _version = (string)manifest[MANIFEST_KEY_VERSION];
            if (manifest.ContainsKey(MANIFEST_KEY_EMBED))
            {
                var token = manifest[MANIFEST_KEY_EMBED];
                if (token is JArray)
                {
                    Embedded = ((JArray)manifest[MANIFEST_KEY_EMBED]).Select(c => (string)c).ToArray();
                }
                else
                {
                    Embedded = new string[] { token.ToString() };
                }
            }
            if (manifest.ContainsKey(MANIFEST_KEY_REQUIRE))
            {
                _require = ArrayOrString(manifest[MANIFEST_KEY_REQUIRE]);
            }
            if (manifest.ContainsKey(MANIFEST_KEY_DEPENDENCY))
            {
                _depend = ArrayOrString(manifest[MANIFEST_KEY_DEPENDENCY]);
            }
            Root = root;
            SettingsContainer = new JSSettings(this);
            FunctionsContainer = new JSFuntions(this);

            Initialize();
        }

        private string[] ArrayOrString(JToken token)
            => token is JArray ? ((JArray)token).Select(c => (string)c).ToArray() : (new string[] { token.ToString() });

        private async Task<JObject> ReadManifest()
        {
            var manifestFile = await Root.TryGetItemAsync(PLUGIN_MANIFEST_FILE_NAME);
            var json = await FileIO.ReadTextAsync(manifestFile as IStorageFile);
            return JObject.Parse(json);
        }

        /// <summary>
        /// Functions and values provided by API
        /// </summary>
        private void Initialize()
        {
            // function
            FunctionsContainer.Initialize();
            // field
            Engine.SetValue("window", JSWindow.Instance.GetWrapper());
            Engine.SetValue("plugin", GetWrapper());
        }

        public JSPluginWrapper GetWrapper() => new JSPluginWrapper(this);

        internal event EventHandler Unloaded;

        public override string Name => _name;

        public override string Description => _des;

        public override string Author => _author;

        public override string Version => _version;

        public override string[] Requirements => _require;

        public override IPlugin[] Dependencies => _depend.Select(p => PluginManager.Find(p) ?? throw new ArgumentNullException()).ToArray();

        public override async Task OnLoad()
        {
            if (Loaded)
            {
                return;
            }

            // dynamic update of settings manifest is supported
            var manifest = await ReadManifest();
            if (manifest.ContainsKey(MANIFEST_KEY_SETTINGS))
            {
                SettingsContainer.LoadSettingsDescriptor((JObject)manifest[MANIFEST_KEY_SETTINGS]);
            }

            var mainFile = await Root.GetFileAsync(PLUGIN_ENTRY_FILE_NAME);
            Engine.Execute(await FileIO.ReadTextAsync(mainFile));

            Loaded = true;
        }

        public override async Task OnUnload()
        {
            if (!Loaded)
            {
                return;
            }
            Loaded = false;
            Unloaded?.Invoke(this, new EventArgs());
            Unloaded = null;
        }

        public override async Task Uninstall()
        {
            await Root.DeleteAsync();
        }

        public const string PLUGIN_MANIFEST_FILE_NAME = "plugin.json";
        public const string PLUGIN_ENTRY_FILE_NAME = "main.js";
        public const string MANIFEST_KEY_NAME = "name";
        public const string MANIFEST_KEY_DESCRIPTION = "description";
        public const string MANIFEST_KEY_AUTHOR = "author";
        public const string MANIFEST_KEY_VERSION = "version";
        public const string MANIFEST_KEY_REQUIRE = "require";
        public const string MANIFEST_KEY_EMBED = "embed";
        public const string MANIFEST_KEY_DEPENDENCY = "dependsOn";
        public const string MANIFEST_KEY_SETTINGS = "settings";
        public static string[] NecessaryManifestOptions => new string[] { MANIFEST_KEY_NAME, MANIFEST_KEY_AUTHOR, MANIFEST_KEY_VERSION };
    }
}
