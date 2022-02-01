using Hook.API;
using System;
using Windows.UI.Xaml.Controls;

namespace Hook.Plugin
{
    public class Shortcut : IDocument
    {
        public string Name { get; }
        public string Description { get; }
        public Symbol IconSymbol;
        private readonly Func<string> Pathfinding;
        public Shortcut(string name, string description, Func<string> pathfinding, Symbol icon)
        {
            Name = name;
            Description = description;
            IconSymbol = icon;
            Pathfinding = pathfinding;
        }

        public void Open()
        {
            var path = Pathfinding();
            var doc = DocumentInfo.Parse(path);
            doc.Open();
        }
    }
}
