using Hook.API;
using System;

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
        /// <summary>
        /// Only <see cref="LocalDocument"/> and its subclasses implement this property.
        /// </summary>
        public string path {
            get
            {
                if (parent is LocalDocument local)
                {
                    return local.Path;
                }
                else
                {
                    throw new NotImplementedException(string.Format("{0} does not implement path", parent.GetType().Name));
                }
            }
        }
        public string name => parent.Name;
        public string description => parent.Description;
#pragma warning restore IDE1006 // 命名样式
    }
}
