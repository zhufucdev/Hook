using System;

namespace Hook.API
{
    public interface IDocument
    {
        /// <summary>
        /// Open the document and bring it to user.
        /// </summary>
        void Open();
        /// <summary>
        /// Full file name of this document.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Text displayed below the name section
        /// </summary>
        string Description { get; }
    }
}
