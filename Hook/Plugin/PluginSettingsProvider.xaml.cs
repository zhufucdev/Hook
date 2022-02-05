using Hook.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using muxc = Microsoft.UI.Xaml.Controls;

namespace Hook.Plugin
{
    public sealed partial class PluginSettingsProvider : UserControl
    {
        public PluginSettingsProvider()
        {
            this.InitializeComponent();
        }

        private IPlugin _plugin;
        public IPlugin Plugin
        {
            get => _plugin;
            set
            {
                if (_plugin != null)
                {
                    _plugin.Settings.CollectionChanged -= Settings_CollectionChanged;
                }
                _plugin = value;
                List.ItemsSource = value.Settings;
                value.Settings.CollectionChanged += Settings_CollectionChanged;
                UpdateUI();
            }
        }

        private void Settings_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            _ = MainPage.Instance.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (Plugin.Settings.Count > 0)
                {
                    Visibility = Visibility.Visible;
                }
                else
                {
                    Visibility = Visibility.Collapsed;
                }
            });
        }

        public static UIElement GetControl(ISettingsItem settingsItem)
        {
            // use time interval to judge modification source
            var lastValueChanged = DateTime.Now;
            bool isExternalChange() => DateTime.Now - lastValueChanged > TimeSpan.FromMilliseconds(200);
            void markInternalChange() => lastValueChanged = DateTime.Now;

            var valueType = settingsItem.Type;
            if (valueType == typeof(string))
            {
                var textBox = new TextBox()
                {
                    Text = settingsItem.Value as string
                };
                settingsItem.ValueChanged += (sender, e) =>
                {
                    if (isExternalChange())
                    {
                        textBox.Text = e as string;
                    }
                };
                textBox.TextCompositionEnded += (sender, e) =>
                {
                    settingsItem.Value = textBox.Text;
                    markInternalChange();
                };
                return textBox;
            }
            else if (valueType == typeof(double) || valueType == typeof(int) || valueType == typeof(long))
            {
                Range valueRange = Range.Parse("[0,100]");
                if (settingsItem is JSSettingsItem js && js.ValueRange != null)
                {
                    valueRange = js.ValueRange;
                }
                var step = valueType == typeof(double) ? Range.Step : 1;

                var slider = new Slider()
                {
                    Value = (double)settingsItem.Value,
                    Minimum = (double)valueRange.Start + (valueRange.IncludeStart ? 0 : step),
                    Maximum = (double)valueRange.End - (valueRange.IncludeEnd ? 0 : step),
                    StepFrequency = step
                };

                slider.LostFocus += (sender, e) =>
                {
                    settingsItem.Value = slider.Value;
                    markInternalChange();
                };
                settingsItem.ValueChanged += (sender, e) =>
                {
                    if (isExternalChange())
                    {
                        slider.Value = (double)e;
                    }
                };
                return slider;
            }
            else if (valueType == typeof(bool))
            {
                var toggle = new ToggleSwitch()
                {
                    IsOn = (bool)settingsItem.Value
                };
                toggle.Toggled += (sender, e) =>
                {
                    settingsItem.Value = toggle.IsOn;
                    markInternalChange();
                };
                settingsItem.ValueChanged += (sender, e) =>
                {
                    if (isExternalChange())
                    {
                        toggle.IsOn = (bool)e;
                    }
                };
                return toggle;
            }
            throw new NotSupportedException(valueType.Name);
        }
    }
}
