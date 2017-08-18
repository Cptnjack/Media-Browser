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

using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.ViewManagement;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MediaBrowser
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            MyFrame.Navigate(typeof(BrowsePage));

            InitiateTitleColor();
        }

    
        private void InitiateTitleColor()
        {
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.ApplicationView"))
            {
                var title = ApplicationView.GetForCurrentView().Title;
                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                if (titleBar != null && title!= null)
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
            }
        }

        //Function to convert Hex to Color
        private SolidColorBrush GetSolidColorBrush(string hex)
        {
            hex = hex.Replace("#", string.Empty);
            byte a = (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16));
            byte r = (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16));
            byte g = (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16));
            byte b = (byte)(Convert.ToUInt32(hex.Substring(6, 2), 16));
            SolidColorBrush myBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(a, r, g, b));
            return myBrush;
        }
    }
}
