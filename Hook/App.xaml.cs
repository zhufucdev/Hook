using Hook.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using muxc = Microsoft.UI.Xaml.Controls;

namespace Hook
{
    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// 初始化单一实例应用程序对象。这是执行的创作代码的第一行，
        /// 已执行，逻辑上等同于 main() 或 WinMain()。
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// 在应用程序由最终用户正常启动时进行调用。
        /// 将在启动应用程序以打开特定文件等情况下使用。
        /// </summary>
        /// <param name="e">有关启动请求和过程的详细信息。</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Launch(e);
        }

        private void Launch(IActivatedEventArgs e, object param = null)
        {
            Grid rootGrid = Window.Current.Content as Grid;
            Frame rootFrame;

            // 不要在窗口已包含内容时重复应用程序初始化，
            // 只需确保窗口处于活动状态
            if (rootGrid == null)
            {
                // 创建要充当导航上下文的框架，并导航到第一页
                rootGrid = new Grid();
                rootFrame = new Frame();
                rootGrid.Children.Add(rootFrame);

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: 从之前挂起的应用程序加载状态
                }

                // 将框架放在当前窗口中
                Window.Current.Content = rootGrid;
            }
            else
            {
                rootFrame = rootGrid.Children[0] as Frame;
            }

            if (!(e is IPrelaunchActivatedEventArgs) || ((IPrelaunchActivatedEventArgs)e).PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // 当导航堆栈尚未还原时，导航到第一页，
                    // 并通过将所需信息作为导航参数传入来配置
                    // 参数，除非手动提供参数
                    if (param == null && e is ILaunchActivatedEventArgs)
                    {
                        rootFrame.Navigate(typeof(MainPage), (e as ILaunchActivatedEventArgs).Arguments);
                    }
                    else
                    {
                        rootFrame.Navigate(typeof(MainPage), param);
                    }
                }
                // 确保当前窗口处于活动状态
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// 导航到特定页失败时调用
        /// </summary>
        ///<param name="sender">导航失败的框架</param>
        ///<param name="e">有关导航失败的详细信息</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Handle: StarupTask
        /// </summary>
        /// <param name="args"></param>
        protected override void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.StartupTask)
            {
                Launch(args, PARAM_FROM_STARTUP);
            }
            else
            {
                Launch(args);
            }
        }

        /// <summary>
        /// 在将要挂起应用程序执行时调用。  在不知道应用程序
        /// 无需知道应用程序会被终止还是会恢复，
        /// 并让内存内容保持不变。
        /// </summary>
        /// <param name="sender">挂起的请求的源。</param>
        /// <param name="e">有关挂起请求的详细信息。</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: 保存应用程序状态并停止任何后台活动
            PluginManager.UnloadAll();
            deferral.Complete();
        }

        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            Launch(args, "--no-homescreen");
            foreach (var file in args.Files) {
                if (file is IStorageFile)
                {
                    var doc = DocumentInfo.Parse(file as IStorageFile);
                    doc.Open();
                }
            }
        }

        public static void ShowInfoBar(string title, string message, muxc.InfoBarSeverity severity)
        {
            var rootGrid = Window.Current.Content as Grid;
            if (rootGrid == null)
            {
                return;
            }
            var infoBar = rootGrid.FindName("mainInfoBar") as muxc.InfoBar;
            if (infoBar == null)
            {
                infoBar = new muxc.InfoBar()
                {
                    Name = "mainInfoBar",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 30, 0, 0)
                };
                rootGrid.Children.Add(infoBar);
            }
            infoBar.Title = title;
            infoBar.Message = message;
            infoBar.Severity = severity;
            infoBar.IsOpen = true;
        }

        public const string PARAM_NO_HOME = "--no-homescreen";
        public const string PARAM_FROM_STARTUP = "--startup";
    }
}
