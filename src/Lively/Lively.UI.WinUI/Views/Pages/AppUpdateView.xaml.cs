﻿using Lively.Common;
using Lively.Grpc.Client;
using Lively.UI.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI.Views.Pages
{
    public sealed partial class AppUpdateView : Page
    {
        private readonly AppUpdateViewModel viewModel;

        public AppUpdateView()
        {
            this.InitializeComponent();
            this.viewModel = App.Services.GetRequiredService<AppUpdateViewModel>();
            this.DataContext = this.viewModel;

            // Error when setting in xaml
            WebView.DefaultBackgroundColor = ((SolidColorBrush)App.Current.Resources["ApplicationPageBackgroundThemeBrush"]).Color;
            // Set website theme to reflect app setting
            var pageTheme = App.Services.GetRequiredService<IUserSettingsClient>().Settings.ApplicationTheme switch
            {
                AppTheme.Auto => string.Empty, // Website handles theme change based on WebView change.
                AppTheme.Light => "&theme=light",
                AppTheme.Dark => "&theme=dark",
                _ => string.Empty,
            };
            var url = !viewModel.IsBetaBuild ? 
                $"https://www.rocksdanister.com/lively/changelog/?source=app{pageTheme}" : 
                $"https://www.rocksdanister.com/lively-webpage/changelog/?source=app{pageTheme}";
            WebView.Source = LinkUtil.SanitizeUrl(url);
        }

        // ref: https://github.com/MicrosoftEdge/WebView2Samples
        private async void WebView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            if (args.Exception != null)
            {
                viewModel.UpdateChangelogError = args.Exception.ToString();
            }
            else
            {
                WebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
                // Theme need to set css, ref: https://github.com/MicrosoftEdge/WebView2Feedback/issues/4426
                WebView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
                // Don't allow contextmenu and devtools
                WebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            }
        }

        private void CoreWebView2_NewWindowRequested(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs args)
        {
            // Prevent popups
            if (!args.IsUserInitiated)
                return;

            // Open hyperlinks in default browser
            args.Handled = true;
            LinkUtil.OpenBrowser(args.Uri);
        }

        private void WebView_NavigationStarting(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
        {
            // Stay in page
            if (args.IsRedirected)
                args.Cancel = true;
            else
                WebViewProgress.Visibility = Visibility.Visible;
        }

        private void WebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            WebViewProgress.Visibility = Visibility.Collapsed;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            BackgroundGridShadow.Receivers.Add(BackgroundGrid);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            WebView.Close();
        }
    }
}
