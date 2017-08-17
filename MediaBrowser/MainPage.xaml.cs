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
        }

        //private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        //{
        //    MySplitView.IsPaneOpen = !MySplitView.IsPaneOpen;
        //}

        //private void IconsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (HomeListBoxItem.IsSelected) { MyFrame.Navigate(typeof(BrowsePage)); }
        //    else if (FavoritesListBoxItem.IsSelected) { MyFrame.Navigate(typeof(BrowsePage)); }
        //}

        //private void Tags_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{

        //}

        //private void FilterListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (FilterListBoxItem.IsSelected)
        //    {
        //        MySplitView.IsPaneOpen = true;
        //        Tags.Visibility = Visibility.Visible;
        //    }
        //    else
        //        Tags.Visibility = Visibility.Collapsed;
        //}

        //private void MySplitView_PaneClosing(SplitView sender, SplitViewPaneClosingEventArgs args)
        //{
        //    if (Tags.Visibility == Visibility.Visible)
        //    {
        //        Tags.Visibility = Visibility.Collapsed;
        //        FilterListBoxItem.IsSelected = false;
                
        //    }
        //}
    }
}
