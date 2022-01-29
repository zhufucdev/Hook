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
        /// The absolute path of this document.
        /// </summary>
        string Path { get; }
        /// <summary>
        /// Full file name of this document.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Represents the last time when this document was opened.
        /// </summary>
        DateTime LastTouched { get; }
    }
}
