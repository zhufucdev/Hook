using Hook.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hook.Plugin
{
    internal class JSDocumentWrapper
    {
        IDocument parent;
        public JSDocumentWrapper(IDocument parent)
        {
            this.parent = parent;
        }

#pragma warning disable IDE1006 // 命名样式
        public void open() => parent.Open();
        public string path => parent.Path;
        public string name => parent.Name;
        public DateTime lastTouched => parent.LastTouched;
#pragma warning restore IDE1006 // 命名样式
    }
}
