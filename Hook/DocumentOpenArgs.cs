using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hook
{
    public class DocumentOpenArgs : EventArgs
    {
        public readonly WebView2 WebView;
        public readonly DocumentInfo DocumentInfo;

        public DocumentOpenArgs(WebView2 webView, DocumentInfo doc) : base()
        {
            WebView = webView;
            DocumentInfo = doc;
        }
    }
}
