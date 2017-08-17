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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MediaBrowser
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BrowsePage : Page
    {
        private List<MediaInfo> Shows;
        

        public BrowsePage()
        {
            this.InitializeComponent();

            Shows = MediaManager.GetMedia();
            Shows.Sort((x, y) => x.Title.CompareTo(y.Title));
        }

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var media = (MediaInfo)e.ClickedItem;
            
            // Open a new frame and send the media information that is being opened
        }

        private void SortBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update the order in which the shows are being displayed.

            if (Shows != null)
            {
                MediaInfoGrid.ItemsSource = null;
                Shows.Sort((x, y) => x.Title.CompareTo(y.Title));
                MediaInfoGrid.ItemsSource = Shows;
            }
                
        }

        private void ViewStyleBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update the data being displayed using a grid for tiles or listview for list
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            MySplitView.IsPaneOpen = !MySplitView.IsPaneOpen;
        }

        private void FilterResetButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset the TagListView list
            TagListView.SelectedItems.Clear();
        }

        private void TagListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update the grid list to display shows with the selected tags only
        }
    }
}
