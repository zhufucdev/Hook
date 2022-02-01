using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hook
{
    public class DocumentEventArgs : EventArgs
    {
        public readonly WebView2 WebView;
        public readonly CacheDocument info;

        public DocumentEventArgs(WebView2 webView, CacheDocument doc) : base()
        {
            WebView = webView;
            info = doc;
        }
    }
}
