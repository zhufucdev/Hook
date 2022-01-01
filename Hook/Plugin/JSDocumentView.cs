using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hook.Plugin
{
    internal class JSDocumentView : SimpleDocumentView
    {
        private readonly JSPlugin plugin;

        public JSDocumentView(JSPlugin plugin, WebView2 webView2, DocumentInfo doc) : base(webView2, doc)
        {
            this.plugin = plugin;
        }

        /// <summary>
        /// Run embedded script
        /// </summary>
        /// <param name="name">name of the js file</param>
        /// <param name="functionName">name of the function in that file to call</param>
        /// <param name="arguments">arguments of the evluation</param>
        public async Task<string> RunScript(string name, string functionName, string arguments)
        {
            var scriptFile = await plugin.Root.GetFileAsync(name + ".js");
            var content = await FileIO.ReadTextAsync(scriptFile);
            if (functionName == null) {
                return await RunScript(content);
            }
            else
            {
                await RunScript(content);
                return await RunScript(string.Format("{0}({1})", functionName, arguments));
            }
        }
    }
}
