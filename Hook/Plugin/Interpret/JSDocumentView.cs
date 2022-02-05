using Hook.API;
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

        public JSDocumentView(JSPlugin plugin, WebView2 webView2, IDocument doc) : base(webView2, doc)
        {
            this.plugin = plugin;
        }

        /// <summary>
        /// Run embedded script
        /// </summary>
        /// <param name="name">name of the js file</param>
        /// <param name="functionName">name of the function in that file to call</param>
        /// <param name="arguments">arguments of the evluation</param>
        public async Task<string> RunEmbedded(string name, string functionName = null, string arguments = null)
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

        public Wrapper GetWrapper() => new Wrapper(this);

        public class Wrapper
        {
            private JSDocumentView parent;
            public Wrapper(JSDocumentView parent)
            {
                this.parent = parent;
                info = new JSDocumentWrapper(parent.Info);
            }
#pragma warning disable IDE1006 // 命名样式
            public Uri source => parent.Source;
            public JSDocumentWrapper info;
            public double zoomFactor
            {
                get => parent.ZoomFactor;
                set => parent.ZoomFactor = value;
            }
            public void close() => parent.Close();

            public string runScript(string script)
            {
                using (var task = parent.RunScript(script))
                {
                    task.Wait();
                    return task.Result;
                }
            }

            public void runEmbedded(string name, string functionName = null, string arguments = null)
            {
                _ = parent.RunEmbedded(name, functionName, arguments);
            }
#pragma warning restore IDE1006 // 命名样式
        }
    }
}
