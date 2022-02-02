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
        public Action<double> ProgressUpdater { private get; set; }
        public Action Callback { private get; set; } = null;
        private readonly Action<Action<double>, Action<string>> Pathfinding;
        public Shortcut(string name, string description, Action<Action<double>, Action<string>> pathfinding, Symbol icon)
        {
            Name = name;
            Description = description;
            IconSymbol = icon;
            Pathfinding = pathfinding;
        }

        public void Open()
        {
            Action<string> callback = (p) =>
            {
                if (p != null)
                {
                    var doc = DocumentInfo.Parse(p);
                    _ = MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, doc.Open);
                }
                Callback?.Invoke();
            };
            Pathfinding(ProgressUpdater, callback);
        }
    }
}
