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
using MediaBrowser.Models;
using Microsoft.Toolkit.Uwp;

using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace MediaBrowser
{
    public class UserSettings
    {
        public string ViewStyle { get; set; }
        public string SortStyle { get; set; }
        public string ColorTheme { get; set; }
        public bool IndividualPagesEnabled { get; set; }

        public UserSettings()
        {
            this.ViewStyle = "List";
            this.SortStyle = "Alphabetical";
            this.ColorTheme = "Dark";
            this.IndividualPagesEnabled = false;
        }
    }

    public class Settings
    {

        public static void SetColorTheme(string theme)
        {

            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.ApplicationView"))
            {
                var title = ApplicationView.GetForCurrentView().Title;
                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                if (titleBar != null && title != null)
                {
                    if (theme == "Dark")
                    {
                        titleBar.ButtonBackgroundColor = GetSolidColorBrush("#FF1D1D1D").Color;
                        titleBar.ButtonForegroundColor = Colors.White;
                        titleBar.BackgroundColor = GetSolidColorBrush("#FF1D1D1D").Color;
                        titleBar.ForegroundColor = Colors.White;
                        titleBar.InactiveBackgroundColor = GetSolidColorBrush("#FF1D1D1D").Color; ;
                        titleBar.ButtonInactiveBackgroundColor = GetSolidColorBrush("#FF1D1D1D").Color;
                        titleBar.ButtonHoverBackgroundColor = GetSolidColorBrush("#FFFFFFFF").Color;
                        titleBar.ButtonHoverForegroundColor = GetSolidColorBrush("#00000000").Color;
                    }
                    else if (theme == "Light")
                    {
                        titleBar.ButtonBackgroundColor = GetSolidColorBrush("#FFF3F3F3").Color;
                        titleBar.ButtonForegroundColor = Colors.Black;
                        titleBar.BackgroundColor = GetSolidColorBrush("#FFF3F3F3").Color;
                        titleBar.ForegroundColor = Colors.Black;
                        titleBar.InactiveBackgroundColor = GetSolidColorBrush("#FFF3F3F3").Color; ;
                        titleBar.ButtonInactiveBackgroundColor = GetSolidColorBrush("#FFF3F3F3").Color;
                        titleBar.ButtonHoverBackgroundColor = GetSolidColorBrush("#00000000").Color;
                        titleBar.ButtonHoverForegroundColor = GetSolidColorBrush("#FFFFFFFF").Color;
                    }
                }
            }
        }

        //Function to convert Hex to Color
        public static SolidColorBrush GetSolidColorBrush(string hex)
        {
            hex = hex.Replace("#", string.Empty);
            byte a = (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16));
            byte r = (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16));
            byte g = (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16));
            byte b = (byte)(Convert.ToUInt32(hex.Substring(6, 2), 16));
            SolidColorBrush myBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
            return myBrush;
        }

        public static async void SaveSettings(UserSettings currentSettings)
        {
            await XmlParser.SetViewStyle(currentSettings);
            await XmlParser.SetSortStyle(currentSettings);
            await XmlParser.SetColorTheme(currentSettings);
            await XmlParser.SetIndividualPagesEnabled(currentSettings);
        }
    }
}
