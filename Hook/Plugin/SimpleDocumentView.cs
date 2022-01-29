using Hook.API;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Hook.Plugin
{
    internal class SimpleDocumentView : DocumentView
    {

        protected readonly WebView2 webView;
        protected readonly DocumentInfo doc;
        protected SimpleDocumentView(WebView2 webView, DocumentInfo doc)
        {
            this.webView = webView;
            this.doc = doc;
        }

        public double ZoomFactor
        {
            get
            {
                var task = RunScript("document.body.style.zoom");
                task.Wait();
                int.TryParse(task.Result, out int factor);
                return factor;
            }
            set => _ = RunScript(string.Format("document.body.style.zoom = {0}", value));
        }

        public Uri Source => webView.Source;

        public IDocument Info => doc;

        public void Close()
        {
            MainPage.Instance.CloseDocument(doc);
            webView.Close();
        }

        public virtual async Task<string> RunScript(string script) => await webView.CoreWebView2.ExecuteScriptAsync(script);
    }
}
