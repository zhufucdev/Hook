using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Resources;
using Windows.Globalization;
using Windows.Storage;
using Windows.System.UserProfile;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Hook
{
    internal class Utility
    {
        public static string GetResourceString(string code, UIContext context = null, string resources = null)
        {
            ResourceLoader resourceLoader;
            if (CoreWindow.GetForCurrentThread() != null)
            {
                if (string.IsNullOrEmpty(resources))
                {
                    resourceLoader = ResourceLoader.GetForCurrentView();
                } 
                else
                {
                    resourceLoader = ResourceLoader.GetForCurrentView(resources);
                }
            }
            else if (context != null)
            {
                if (string.IsNullOrEmpty(resources))
                {
                    resourceLoader = ResourceLoader.GetForUIContext(context);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                return code;
            }
            var str = resourceLoader.GetString(code);
            return string.IsNullOrWhiteSpace(str) ? code : str;
        }

        public static T FindControl<T>(UIElement parent, Type targetType, string ControlName) where T : FrameworkElement
        {

            if (parent == null) return null;

            if (parent.GetType() == targetType && ((T)parent).Name == ControlName)
            {
                return (T)parent;
            }
            T result = null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                UIElement child = (UIElement)VisualTreeHelper.GetChild(parent, i);
                var find = FindControl<T>(child, targetType, ControlName);
                if (find != null)
                {
                    result = find;
                    break;
                }
            }
            return result;
        }

        public static DocumentConvert DefaultConverter;
        public static ObservableCollection<DocumentConvert> AvailableConverters = new ObservableCollection<DocumentConvert>();

        public static ApplicationDataStorageHelper DataStorageHelper = new ApplicationDataStorageHelper(ApplicationData.Current, new JsonObjectSerializer());

        public static ApplicationDataContainer RoamingSettings => ApplicationData.Current.RoamingSettings;
        public static ApplicationDataContainer LocalSettings => ApplicationData.Current.LocalSettings;

        public static T GetSettings<T>(string key)
        {
            if (DataStorageHelper.KeyExists(key))
            {
                return DataStorageHelper.Read<T>(key);
            }
            return default;
        }
        public static void ModifySettings<T>(string key, T value)
        {
            DataStorageHelper.Save(key, value);
        }

        public static bool DeveloperMode
        {
            get => GetSettings<bool>(KEY_DEVELOPER_MODE);
            set => ModifySettings(KEY_DEVELOPER_MODE, value);
        }

        public static string[] Sideloaders
        {
            get => GetSettings<string[]>(KEY_SIDELOADERS) ?? new string[0];
            set => ModifySettings(KEY_SIDELOADERS, value);
        }

        public static Collection<Language> LanguageCodes => new Collection<Language>()
        {
            new Language(GetResourceString("AppLanguageCombo/PlaceholderText"), string.Empty),
            new Language("简体中文", "zh-CN"),
            new Language("American English", "en-US")
        };
        public static string LanguageOverride
        {
            get => GetSettings<string>(KEY_LANGUAGE) ?? string.Empty;
            set => ModifySettings(KEY_LANGUAGE, value);
        }

        public static void SetAppLanguage(string code)
        {
            ApplicationLanguages.PrimaryLanguageOverride = string.IsNullOrEmpty(code) ? string.Empty : code;
        }

        public const string KEY_SIDELOADERS = "Sideloaders";
        public const string KEY_LANGUAGE = "Language";
        public const string KEY_DEVELOPER_MODE = "DeveloperMode";
        public const string KEY_DEFAULT_CONVERTER = "DefaultCoverter";
    }

    public class Language
    {
        public readonly string Name;
        public readonly string Code;

        public Language(string name, string code)
        {
            Name = name;
            Code = code;
        }

        public override string ToString() => Name;
    }
}
