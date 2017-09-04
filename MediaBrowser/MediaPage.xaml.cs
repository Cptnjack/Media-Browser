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
using Microsoft.Toolkit.Uwp.UI.Controls;
using System.Collections.ObjectModel;
using Windows.Storage.FileProperties;
using Windows.Storage;
using Windows.Storage.AccessCache;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Toolkit.Uwp.UI;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MediaBrowser
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MediaPage : Page
    {
        private MediaInfo SpecificShow;
        private Information showInfo;

        ObservableCollection<string> TagsList;
        AdvancedCollectionView acvTagsList;

        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

        private struct Information
        {
            public string Title { get; set; }
            public double Rating { get; set; }
            public string MalLink { get; set; }
        }

        List<string> VideoList;
        List<List<string>> VideoGroups;

        public MediaPage()
        {
            this.InitializeComponent();

            string value = (string)localSettings.Values["ColorTheme"];

            VideoList = new List<string>();
            VideoGroups = new List<List<string>>();
            TagsList = new ObservableCollection<string>();

            Settings.SetColorTheme(value);

            showInfo = new Information();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var value = (MediaInfo)e.Parameter;

            SpecificShow = value;

            LoadInformation();
        }

        private async void LoadInformation()
        {
            ShowTagsListView.ItemsSource = SpecificShow.Tags;

            ObservableCollection<string> testing = new ObservableCollection<string>();
            VideoList.Clear();
            foreach (EpisodeInfo episode in SpecificShow.EpisodeList)
            {
                VideoList.Add(episode.FileName);
            }

            CreateListVideoGroup();

            ObservableCollection<string> TempTagList = new ObservableCollection<string>();
            TempTagList = await Tags.LoadUserDefinedTags();
            if (TempTagList.Count == 0)
            {
                // No user defined tags found. Load the default list and add it to the user xml
                TempTagList = await Tags.LoadDefaultTags();
                Tags.SaveUserDefinedTags(TempTagList);
            }

            foreach (var tag in TempTagList)
                TagsList.Add(tag);

            if (TagsList.Count > 0)
                TagsList = new ObservableCollection<string>(TagsList.OrderBy(i => i));

            // Set the fields
            TitleTextBlock.Text = SpecificShow.Title;
            VideosCountBox.Text = SpecificShow.EpisodeList.Count.ToString();
            if (SpecificShow.Rating == -1)
                RatingTextBlock.Text = "";
            else
                RatingTextBlock.Text = SpecificShow.Rating.ToString();

            FavoriteButton.Checked -= FavoriteButton_Checked;
            FavoriteButton.IsChecked = SpecificShow.Favorite;
            FavoriteButton.Checked += FavoriteButton_Checked;

            if (SpecificShow.ExternalLink != null)
                LinkBox.Text = SpecificShow.ExternalLink;
            else
                LinkBox.Text = "";

            GetVideoFiles();
        }

        private async void GetVideoFiles()
        {
            string token = "";
            StorageFolder folder = null;

            //MediaDir dir = MediaDirs.Where(x => x.LocalAssetFolder == SpecificShow.LocalAssestMediaDirectory).FirstOrDefault();

            Object value = localSettings.Values["CurrentToken"];
            token = value.ToString();

            folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);

            string dirName = new DirectoryInfo(SpecificShow.UserDirectory).Name;

            StorageFolder showFolder = await folder.GetFolderAsync(dirName);

            await RetrieveEpisodesFromMediaDir(showFolder);
        }

        private async Task RetrieveEpisodesFromMediaDir(StorageFolder parent)
        {
            foreach (var item in await parent.GetFilesAsync())
            {
                if (item.FileType == ".mp4"
                            || item.FileType == ".mkv"
                            || item.FileType == ".m4v")
                {

                    //Episode SingleEpisode = new Episode();

                    string Path = item.Path.ToString();
                    StorageFile EpisodeFile = item;

                    SpecificShow.EpisodeList.FirstOrDefault(x => x.Path == item.Path).EpisodeFile = item;
                }
            }

            foreach (var item in await parent.GetFoldersAsync())
            {
                await RetrieveEpisodesFromMediaDir(item);
            }
        }

        private void CreateListVideoGroup()
        {
            // Clear everything from the VideoListPanel
            if (VideoListPanel.Children.Count > 0)
            {
                while (VideoListPanel.Children.Count != 0)
                    VideoListPanel.Children.RemoveAt(0);
            }

            // Break the episodelist into groups based on parent folder
            // Create a new list for each unique parent folder found
            VideoGroups = new List<List<string>>();
            List<string> names = new List<string>();
            foreach (var episode in SpecificShow.EpisodeList)
            {
                string dirName = new DirectoryInfo(episode.Path).Parent.ToString();
                if (names.Count == 0 || !names.Contains(dirName))
                {
                    // This group of videos does not exists so add the name to the list
                    names.Add(dirName);

                    // Create a new episode list and add it to VideoGroups
                    VideoGroups.Add(new List<string>());
                }

                // Get the position of the dirName in names for the current episode
                // This tells me which element position to add the episode to
                var index = names.FindIndex(x => x.ToString() == dirName);

                // Add the episode to the correct video group list
                VideoGroups.ElementAt(index).Add(episode.FileName);
            }

            int i = 0;
            foreach (var group in names)
            {
                ListView newSeasonList = new ListView();

                TextBlock newSeasonText = new TextBlock();

                newSeasonText.Text = group;
                newSeasonText.Foreground = new SolidColorBrush(Windows.UI.Colors.GhostWhite);
                newSeasonText.FontSize = 22;
                newSeasonText.Margin = new Thickness(0, 20, 0, 0);
                newSeasonText.HorizontalAlignment = HorizontalAlignment.Center;

                VideoListPanel.Children.Add(newSeasonText);

                newSeasonList.Name = group; // This will be the parent folder name - same as newSeasonText.Text
                newSeasonList.Background = GetSolidColorBrush("#FF252525");
                newSeasonList.SelectionMode = ListViewSelectionMode.Single;
                newSeasonList.SelectionChanged += VideoListView_SelectionChanged;
                newSeasonList.Margin = new Thickness(0, 20, 0, 20);
                newSeasonList.RequestedTheme = ElementTheme.Dark;
                newSeasonList.MaxWidth = 500;
                newSeasonList.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Gray);
                newSeasonList.BorderThickness = new Thickness(0, 0, 0, 0);
                newSeasonList.HorizontalAlignment = HorizontalAlignment.Stretch;
                newSeasonList.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
                newSeasonList.SetValue(ScrollViewer.HorizontalScrollModeProperty, ScrollMode.Disabled);

                newSeasonList.ItemsSource = VideoGroups.ElementAt(i);

                VideoListPanel.Children.Add(newSeasonList);

                i++;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if(Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

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

        private async void VideoListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StorageFile selectedEpisode = null;

            var listview = sender as ListView;

            if (listview.SelectedIndex != -1)
            {
                foreach (var episode in SpecificShow.EpisodeList)
                {
                    if (episode.FileName == VideoList[listview.SelectedIndex])
                    {
                        // Check listview name to ensure the correct episode is found. Could be the same name but different group
                        string dirName = new DirectoryInfo(episode.Path).Parent.ToString();
                        if (dirName == listview.Name)
                            selectedEpisode = episode.EpisodeFile;
                    }
                }

                if (selectedEpisode != null)
                {
                    var success = await Windows.System.Launcher.LaunchFileAsync(selectedEpisode);
                }
            }
        }

        private async void InfoEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (InfoEditButton.IsChecked == true)
            {
                // Save the information that is currently being displaye to test against when the editing is done
                //showInfo.Rating = Convert.ToDouble(RatingTextBlock.Text);
                showInfo.Rating = SpecificShow.Rating;
                showInfo.MalLink = LinkTextBlock.Text;
                showInfo.Title = TitleTextBlock.Text;

                TitleBox.Visibility = Visibility.Visible;
                TitleBox.Text = showInfo.Title;
                //TitleTextBlock.Visibility = Visibility.Collapsed;
                TitleTextBlock.Text = "";

                RatingBox.Visibility = Visibility.Visible;
                RatingTextBlock.Visibility = Visibility.Collapsed;

                if (showInfo.Rating == -1)
                {
                    RatingBox.Text = "";
                }

                LinkBox.Visibility = Visibility.Visible;
                LinkTextBlock.Visibility = Visibility.Collapsed;

                ShowTagsListView.Visibility = Visibility.Collapsed;
                EditShowTagsButton.Visibility = Visibility.Visible;
            }
            else
            {
                if (showInfo.Title != TitleBox.Text)
                {
                    TitleTextBlock.Text = TitleBox.Text;
                    SpecificShow.Title = TitleTextBlock.Text;
                    await XmlParser.SetTitle(SpecificShow);
                }
                else
                {
                    TitleTextBlock.Text = showInfo.Title;
                }

                if (showInfo.Rating.ToString() != RatingBox.Text)
                {
                    double n;
                    bool isNumeric = double.TryParse(RatingBox.Text, out n);

                    if (isNumeric)
                    {
                        // Make sure the input is between 0 and 10 with a max decimal point of 2
                        double value = Convert.ToDouble(RatingBox.Text);
                        double truncatedValue = Math.Truncate(value * 100) / 100;

                        if (!(truncatedValue > 10.0) && !(truncatedValue < 0))
                        {
                            RatingTextBlock.Text = RatingBox.Text;
                            SpecificShow.Rating = truncatedValue;
                            await XmlParser.SetRating(SpecificShow);

                            SpecificShow.DisplayRating = SpecificShow.Rating.ToString();
                        }
                    }
                }

                if (showInfo.MalLink != LinkBox.Text)
                {
                    // Make sure the link is valid
                    bool result = Uri.CheckSchemeName(LinkBox.Text);

                    if (result == true)
                    {
                        LinkHyperLink.NavigateUri = new Uri(LinkBox.Text);
                        LinkHyperLink.Content = "Link";
                        LinkHyperLink.Visibility = Visibility.Visible;
                        LinkTextBlock.Visibility = Visibility.Collapsed;

                        //showInfo.MalLink = MalTextBlock.Text;
                        SpecificShow.ExternalLink = LinkTextBlock.Text;
                        await XmlParser.SetMal(SpecificShow);
                    }
                    else
                    {
                        if (LinkBox.Text == "")
                        {
                            // The user removed the link. Set the textblock back to say "Not set" and remove the hyperlink
                            LinkTextBlock.Text = "Not set";
                            LinkHyperLink.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            // The user input was invalid so set up an error message and revert the changes back to the original
                            LinkBox.Text = showInfo.MalLink;

                            // Set up the error message for the popup - NEED TO DO THIS STILL
                        }
                    }
                }

                TitleBox.Visibility = Visibility.Collapsed;
                TitleTextBlock.Visibility = Visibility.Visible;

                RatingBox.Visibility = Visibility.Collapsed;
                RatingTextBlock.Visibility = Visibility.Visible;

                LinkBox.Visibility = Visibility.Collapsed;
                LinkTextBlock.Visibility = Visibility.Visible;

                ShowTagsListView.Visibility = Visibility.Visible;
                EditShowTagsButton.Visibility = Visibility.Collapsed;

                // Need to set a check to only update if there was any changes made to the show info
                //UpdateShowInView(SpecificShow); // Change this to somehow to send message to BrwoserPage frame? Don't know if thats possible
            }

        }

        private void HomeTextBlock_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(BrowsePage));
        }

        private void EditShowTagsButton_Click(object sender, RoutedEventArgs e)
        {
            EditTagsPopup.IsOpen = true;

            // Set the new source for the Tags page if needed
            CurrentTagsListView.ItemsSource = SpecificShow.Tags;
        }

        private void ExitTagsEditButton_Click(object sender, RoutedEventArgs e)
        {
            // Exit the popup
            EditTagsPopup.IsOpen = false;
        }

        private void CurrentTagsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Reset the selection of AllTagsListView
            if (CurrentTagsListView.SelectedIndex != -1)
            {
                AllTagsListView.SelectedIndex = -1;

                // Disable the AddTag button. When selecting the current list the user can only remove
                AddButton.IsEnabled = false;
                RemoveButton.IsEnabled = true;
            }
        }

        private void AllTagsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Reset the selection of CurrentTagsList
            if (AllTagsListView.SelectedIndex != -1)
            {
                CurrentTagsListView.SelectedIndex = -1;

                // Disable the RemoveTag button. When selecting the current list the user can only Add
                AddButton.IsEnabled = true;
                RemoveButton.IsEnabled = false;
            }
        }

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentTagsListView.SelectedIndex != -1)
            {
                SpecificShow.Tags.RemoveAt(CurrentTagsListView.SelectedIndex);

                // Remove the tag to the show xml tags element
                await XmlParser.SetTags(SpecificShow);

                // Save the SpecificShow back into AllShows
                //UpdateShowInView(SpecificShow); // Change this to somehow to send message to BrwoserPage frame? Don't know if thats possible
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (AllTagsListView.SelectedIndex != -1)
            {
                // Check if the tag already exists in the show tags list
                bool exists = false;
                foreach (var tag in SpecificShow.Tags)
                {
                    if (tag == TagsList[AllTagsListView.SelectedIndex])
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    // Add the tag to the show
                    SpecificShow.Tags.Add(TagsList[AllTagsListView.SelectedIndex]);

                    // Re-sort the list
                    SpecificShow.Tags = new ObservableCollection<string>(SpecificShow.Tags.OrderBy(i => i));
                    CurrentTagsListView.ItemsSource = SpecificShow.Tags;

                    // Add the tag to the show xml tags element
                    await XmlParser.SetTags(SpecificShow);

                    // Save the SpecificShow back into AllShows
                    //UpdateShowInView(SpecificShow);
                }
            }
        }

        private async void FavoriteButton_Checked(object sender, RoutedEventArgs e)
        {
            SpecificShow.Favorite = true;
            await XmlParser.SetFavorite(SpecificShow);
            //UpdateShowInView(SpecificShow);
        }

        private async void FavoriteButton_Unchecked(object sender, RoutedEventArgs e)
        {
            SpecificShow.Favorite = false;
            await XmlParser.SetFavorite(SpecificShow);
            //UpdateShowInView(SpecificShow);
        }

        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            // Load all media that have favorite set to true
            //FilteredShows = AllShows.Where(x => x.Favorite == true).ToList();
            //MediaInfoGrid.ItemsSource = FilteredShows;
            //MediaInfoListView.ItemsSource = FilteredShows;
        }

        private void EditOverviewButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OnTagsLayoutUpdated(object sender, object e)
        {
            if (EditTagsPanel.ActualWidth == 0 && EditTagsPanel.ActualHeight == 0)
            {
                return;
            }

            double ActualHorizontalOffset = this.EditTagsPopup.HorizontalOffset;
            double ActualVerticalOffset = this.EditTagsPopup.VerticalOffset;

            double NewHorizontalOffset = (Window.Current.Bounds.Width - EditTagsPanel.ActualWidth) / 2;
            double NewVerticalOffset = (Window.Current.Bounds.Height - EditTagsPanel.ActualHeight) / 2;

            if (ActualHorizontalOffset != NewHorizontalOffset || ActualVerticalOffset != NewVerticalOffset)
            {
                this.EditTagsPopup.HorizontalOffset = NewHorizontalOffset;
                this.EditTagsPopup.VerticalOffset = NewVerticalOffset;
            }
        }
    }
}
