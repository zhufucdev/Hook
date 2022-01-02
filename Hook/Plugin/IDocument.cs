using System;

namespace Hook.Plugin
{
    internal interface IDocument
    {
        void Open();
        string Path { get; }
        string Name { get; }
        DateTime LastTouched { get; }
    }
}
