using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Hook.API
{
    public interface ISettingsItem
    {
        string Name { get; }
        string Description { get; }
        Type Type { get; }
        object Value { get; set; }
        Symbol IconSymbol { get; }
        event EventHandler<object> ValueChanged;
    }
}
