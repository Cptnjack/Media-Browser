using System;
using System.IO;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using MediaBrowser.Models;
using System.Threading.Tasks;
using Windows.Storage;
using System.Collections.ObjectModel;
using Windows.Storage.AccessCache;
using Microsoft.Toolkit.Uwp.UI;
using System.Collections.Generic;
using Windows.UI.Xaml.Media;
using System.Xml.Linq;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;


// TO DO:
// 7. The way I'm testing against file types needs to be updated. Seems it can be done better.
// 8. Store the access tokens in the password vault
// 9. Clean and pull out redundant code

namespace MediaBrowser
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BrowsePage : Page
    {
        //private MediaManager allMedia;
        private UserSettings appSettings;

        private MediaInfo SpecificShow;
        private ObservableCollection<MediaInfo> AllShows;
        private List<MediaInfo> FilteredShows;

        private ObservableCollection<MediaDir> MediaDirs;
        private ObservableCollection<string> AllExcludedMedia;

        List<string> VideoList;
        List<List<string>> VideoGroups;
        private Information showInfo;

        ObservableCollection<string> TagsList;
        AdvancedCollectionView acvTagsList;

        List<string> ErrorLog;

        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        private struct Information
        {
            public string Title { get; set; }
            public double Rating { get; set; }
            public string MalLink { get; set; }
        }

        public BrowsePage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;

            string value = (string) localSettings.Values["ColorTheme"];

            Settings.SetColorTheme(value);

            LoadMedia();
        }

        private async void LoadMedia()
        {
            VideoList = new List<string>();
            TagsList = new ObservableCollection<string>();

            // Get all media related information 
            AllShows = await XmlParser.LoadXmlMediaInfo();
            FilteredShows = new List<MediaInfo>();
            FilteredShows = AllShows.ToList();
            MediaDirs = await XmlParser.LoadXmlMediaDirs();

            // Check to make sure the media directories still exist - if the user moved them shit blows up
            foreach (var mediaDir in MediaDirs)
            {
                try
                {
                    StorageFolder folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(mediaDir.AccessToken);
                }
                catch
                {
                    // The token did not work. The user most likely moved the folder location. Delete the media directory from the xml
                    XmlParser.DeleteFromXML(mediaDir);
                }
            }

            // Check for new media added manually to each of the media directories
            CheckForDirectoryUpdates();

            // Load the last used settings
            appSettings = await XmlParser.LoadSettings();

            // Load the tags - yes not putting the info directly into TagsList is on purpose. Work around
            // since returning a list changes the object TagsList points to resulting in losing the 
            // binding on the observable collection
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

            acvTagsList = new AdvancedCollectionView(TagsList);
            acvTagsList.SortDescriptions.Add(new SortDescription(SortDirection.Ascending));
            TagsListView.ItemsSource = acvTagsList;

            // Set the sort and view style based on the settings loaded
            if (appSettings.ViewStyle == "Tile")
            {
                ViewStyleBox.SelectedIndex = 0;
                localSettings.Values["ViewStyle"] = "Tile";
            }
            else if (appSettings.ViewStyle == "List")
            {
                ViewStyleBox.SelectedIndex = 1;
                localSettings.Values["ViewStyle"] = "List";
            }
            else if (appSettings.ViewStyle == "Mix")
            {
                ViewStyleBox.SelectedIndex = 2;
                localSettings.Values["ViewStyle"] = "Mix";
            }
            else
            {
                ViewStyleBox.SelectedIndex = 1;
                localSettings.Values["ViewStyle"] = "List";
            }

            if (appSettings.SortStyle == "Alphabetical")
            {
                SortBox.SelectedIndex = 0;
                localSettings.Values["SortStyle"] = "Alphabetical";
            }
            else if (appSettings.SortStyle == "Rating")
            {
                SortBox.SelectedIndex = 1;
                localSettings.Values["SortStyle"] = "Rating";
            }
            else
            {
                SortBox.SelectedIndex = 0;
                localSettings.Values["SortStyle"] = "Alphabetical";
            }
        }

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var media = (MediaInfo)e.ClickedItem;

            SpecificShow = media;

            MediaInfoListView.IsItemClickEnabled = false;
            MediaInfoGrid.IsItemClickEnabled = false;

            List<string> testing = new List<string>();
            foreach (EpisodeInfo episode in media.EpisodeList)
            {
                testing.Add(episode.FileName);
            }

            VideoList = testing;
            CreateListVideoGroup();

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

            // Reset the hyperlink connection
            LinkHyperLink.NavigateUri = null;
            LinkHyperLink.Content = "";
            LinkHyperLink.Visibility = Visibility.Collapsed;

            if (SpecificShow.ExternalLink != "")
            {
                Uri externalLink = new Uri(SpecificShow.ExternalLink);

                LinkHyperLink.Content = "Link";
                LinkHyperLink.NavigateUri = externalLink;
                LinkHyperLink.Visibility = Visibility.Visible;
                LinkTextBlock.Visibility = Visibility.Collapsed;
                LinkBox.Visibility = Visibility.Collapsed;
            }
            else
                LinkBox.Text = "";

            GetVideoFiles();

            CompactMediaPopup.IsOpen = true;
        }

        private void SortBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update the order in which the shows are being displayed.
            if (AllShows != null)
            {
                String value = SortBox.SelectedValue.ToString();

                if(value == "Alphabetical")
                {
                    AllShows = new ObservableCollection<MediaInfo>(AllShows.OrderBy(i => i.Title));
                    FilteredShows = new List<MediaInfo>(FilteredShows.OrderBy(i => i.Title));
                    MediaInfoGrid.ItemsSource = FilteredShows;
                    MediaInfoListView.ItemsSource = FilteredShows;
                    appSettings.SortStyle = value;
                    localSettings.Values["SortStyle"] = value;
                }
                else if (value == "Rating")
                {
                    AllShows = new ObservableCollection<MediaInfo>(AllShows.OrderByDescending(i => i.Rating));
                    FilteredShows = new List<MediaInfo>(FilteredShows.OrderByDescending(i => i.Rating));
                    MediaInfoGrid.ItemsSource = FilteredShows;
                    MediaInfoListView.ItemsSource = FilteredShows;
                    appSettings.SortStyle = value;
                    localSettings.Values["SortStyle"] = value;
                }
            }            
        }

        private async void ViewStyleBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update the data being displayed using a grid for tiles or listview for list
            string value = ((ComboBoxItem)ViewStyleBox.SelectedItem).Content.ToString();

            if (value == "Tile" && MediaInfoListView != null && MediaInfoGrid != null)
            {
                MediaInfoGrid.Visibility = Visibility.Visible;
                MediaInfoGridDropShadow.Visibility = Visibility.Visible;
                ListViewStackPanel.Visibility = Visibility.Collapsed;
                MediaInfoListViewDropShadow.Visibility = Visibility.Collapsed;
                TagsStackPanel.Margin = new Thickness(0, 0, 0, 20);
                appSettings.ViewStyle = value;
                localSettings.Values["ViewStyle"] = value;
            }
            else if(value == "List" && MediaInfoListView != null && MediaInfoGrid != null)
            {
                MediaInfoGrid.Visibility = Visibility.Collapsed;
                MediaInfoGridDropShadow.Visibility = Visibility.Collapsed;

                //MediaInfoListView.ItemTemplate = (DataTemplate)Resources["ListViewWithoutCovers"];
                ListViewWithoutCoversGid.Visibility = Visibility.Visible;
                ListViewWithCoversGid.Visibility = Visibility.Collapsed;
                await LoadListView("ListViewWithoutCovers");

                ListViewStackPanel.Visibility = Visibility.Visible;
                MediaInfoListViewDropShadow.Visibility = Visibility.Visible;

                TagsStackPanel.Margin = new Thickness(0, 60, 0, 20);
                appSettings.ViewStyle = value;
                localSettings.Values["ViewStyle"] = value;
            }
            else if (value == "Mix" && MediaInfoListView != null && MediaInfoGrid != null)
            {
                MediaInfoGrid.Visibility = Visibility.Collapsed;
                MediaInfoGridDropShadow.Visibility = Visibility.Collapsed;

                //MediaInfoListView.ItemTemplate = (DataTemplate)Resources["ListViewWithCovers"];
                ListViewWithCoversGid.Visibility = Visibility.Visible;
                ListViewWithoutCoversGid.Visibility = Visibility.Collapsed;
                await LoadListView("ListViewWithCovers");

                ListViewStackPanel.Visibility = Visibility.Visible;
                MediaInfoListViewDropShadow.Visibility = Visibility.Visible;

                TagsStackPanel.Margin = new Thickness(0, 60, 0, 20);
                appSettings.ViewStyle = value;
                localSettings.Values["ViewStyle"] = value;
            }
        }

        private async Task LoadListView(string type)
        {
            MediaInfoListView.ItemTemplate = (DataTemplate)Resources[type];
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            FilterSplitView.IsPaneOpen = !FilterSplitView.IsPaneOpen;
        }

        private void FilterResetButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset the TagListView list
            TagListView.SelectedItems.Clear();
        }

        private void TagListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update the grid/list view to display shows with the selected tags only
            UpdateMediaViews();
        }

        private void FilterSplitView_PaneClosing(SplitView sender, SplitViewPaneClosingEventArgs args)
        {

            string value = ((ComboBoxItem)ViewStyleBox.SelectedItem).Content.ToString();

            if (value == "Tile" && MediaInfoListView != null && MediaInfoGrid != null)
            {
                TagsStackPanel.Margin = new Thickness(0, 0, 0, 20);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MediaInfoGrid.IsItemClickEnabled = false;
            MediaInfoListView.IsItemClickEnabled = false;

            MediaManagerPanel.Visibility = Visibility.Visible;
            ExclusionManagerPanel.Visibility = Visibility.Collapsed;
            TagsManagerPanel.Visibility = Visibility.Collapsed;

            SettingsPopup.IsOpen = !SettingsPopup.IsOpen;
        }

        private async void AddMediaDirButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorLog = new List<string>();

            // Ask the user for the directory
            StorageFolder folder = await GrabDirectory();

            if (folder != null)
            {
                // Make sure a media folder of the same name does not already exist. Not allowing two different folders
                // with the same name from two different drives to be used. **ADD SOME KIND OF ERROR HERE**
                if (await ApplicationData.Current.LocalFolder.TryGetItemAsync(folder.Name) == null)
                {
                    MediaDir newMediaDir = new MediaDir();

                    string Token = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(folder);
                    newMediaDir.AccessToken = Token;

                    await ApplicationData.Current.LocalFolder.CreateFolderAsync(folder.Name);
                    StorageFolder originalPosition = await ApplicationData.Current.LocalFolder.GetFolderAsync(folder.Name);

                    newMediaDir.UserDirFolder = folder.Path;
                    newMediaDir.LocalAssetFolder = originalPosition.Path;
                    newMediaDir.Name = folder.Name;
                    MediaDirs.Add(newMediaDir);
                    await XmlParser.AddMediaDirToXml(newMediaDir);

                    foreach (var item in await folder.GetFoldersAsync())
                    {
                        SpecificShow = null;
                        SpecificShow = new MediaInfo();
                        SpecificShow.EpisodeList = new ObservableCollection<EpisodeInfo>();

                        await originalPosition.CreateFolderAsync(item.DisplayName);
                        StorageFolder appSubFolder = await originalPosition.GetFolderAsync(item.DisplayName);

                        await RetrieveShowsFromMediaDir(item, appSubFolder);

                        // No media files were found. This sub directory must not actually be a show storage directory. Don't import anything.
                        if (SpecificShow.EpisodeList.Count != 0)
                        {
                            SpecificShow.LocalAssestMediaDirectory = originalPosition.Path;
                            SpecificShow.UserDirectory = item.Path;
                            SpecificShow.Title = item.Name;

                            // Check if a cover photo was found.
                            if (SpecificShow.CoverImage == null)
                            {
                                // Not found so set the image source to the placeholder image
                                SpecificShow.CoverImage = "/Assets/TempPic.png";
                            }

                            AllShows.Add(SpecificShow);
                            await XmlParser.AddShowToXml(SpecificShow, newMediaDir);
                        }
                        else
                        {
                            // Output a message to a text file. This should tell me when a show isn't added to the list. Probably missing some video types.
                            ErrorLog.Add(item.Path);
                            await appSubFolder.DeleteAsync();
                        }
                    }

                    if(ErrorLog.Count > 0)
                    {
                        ErrorPopup.IsOpen = true;
                        ErrorsListView.ItemsSource = ErrorLog;
                    }

                    UpdateMediaViews();
                }
            }
        }

        private async void DeleteMediaDirButton_Click(object sender, RoutedEventArgs e)
        {
            // Make this able to support multi selection eventually
            if(MediaDirListView.SelectedItem != null)
            {
                if (await ApplicationData.Current.LocalFolder.TryGetItemAsync(MediaDirs.ElementAt(MediaDirListView.SelectedIndex).Name) != null)
                {
                    StorageFolder appLocalMediaFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(MediaDirs.ElementAt(MediaDirListView.SelectedIndex).Name);

                    await appLocalMediaFolder.DeleteAsync();

                    for(int i = AllShows.Count -1; i>=0; i--)
                    {
                        if (AllShows.ElementAt(i).LocalAssestMediaDirectory == appLocalMediaFolder.Path)
                            AllShows.RemoveAt(i);
                    }

                    XmlParser.DeleteFromXML(MediaDirs.ElementAt(MediaDirListView.SelectedIndex));
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Remove(MediaDirs.ElementAt(MediaDirListView.SelectedIndex).AccessToken);

                    MediaDirs.RemoveAt(MediaDirListView.SelectedIndex);

                    UpdateMediaViews();
                }
            }
        }

        private async Task<StorageFolder> GrabDirectory()
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                // Application now has read/write access to all contents in the picked folder including subfolders
                return folder;
            }
            else
            {
                return folder;
            }
        }

        private async Task RetrieveShowsFromMediaDir(StorageFolder parent, StorageFolder appSubFolder)
        {
            foreach (var item in await parent.GetFilesAsync())
            {
                if (item.FileType == ".jpg"
                    || item.FileType == ".png"
                    || item.FileType == ".jpeg")
                {
                    if (item.DisplayName.ToLower() == "cover")
                    {
                        try
                        {
                            StorageFolder folder = await item.GetParentAsync();

                            await item.CopyAsync(appSubFolder);

                            SpecificShow.CoverImage = appSubFolder.Path + @"\Cover" + item.FileType;
                        }
                        catch (Exception e)
                        {

                        }
                    }
                }
                else if (item.FileType == ".mp4"
                            || item.FileType == ".mkv"
                            || item.FileType == ".m4v"
                            || item.FileType == ".avi")
                {

                    EpisodeInfo episode = new EpisodeInfo();

                    episode.Path = item.Path.ToString();
                    episode.EpisodeFile = item;
                    episode.FileName = item.Name;

                    SpecificShow.EpisodeList.Add(episode);
                }
            }

            foreach (var item in await parent.GetFoldersAsync())
            {
                await RetrieveShowsFromMediaDir(item, appSubFolder);
            }
        }


        private void MediaInfoListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var media = (MediaInfo)e.ClickedItem;
            SpecificShow = media;

            SpecificShow = media;

            ShowTagsListView.ItemsSource = SpecificShow.Tags;

            MediaInfoListView.IsItemClickEnabled = false;
            MediaInfoGrid.IsItemClickEnabled = false;

            ObservableCollection<string> testing = new ObservableCollection<string>();
            VideoList.Clear();
            foreach (EpisodeInfo episode in media.EpisodeList)
            {
                VideoList.Add(episode.FileName);
            }

            CreateListVideoGroup();

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

            // Reset the hyperlink connection
            LinkHyperLink.NavigateUri = null;
            LinkHyperLink.Content = "";
            LinkHyperLink.Visibility = Visibility.Collapsed;

            if (SpecificShow.ExternalLink != "")
            {
                Uri externalLink = new Uri(SpecificShow.ExternalLink);

                LinkHyperLink.Content = "Link";
                LinkHyperLink.NavigateUri = externalLink;
                LinkHyperLink.Visibility = Visibility.Visible;
                LinkTextBlock.Visibility = Visibility.Collapsed;
                LinkBox.Visibility = Visibility.Collapsed;
            }
            else
                LinkBox.Text = "";

            GetVideoFiles();

            CompactMediaPopup.IsOpen = true;
        }

        private async void GetVideoFiles()
        {
            string token = "";
            StorageFolder folder = null;

            MediaDir dir = MediaDirs.Where(x => x.LocalAssetFolder == SpecificShow.LocalAssestMediaDirectory).FirstOrDefault();

            if (dir != null)
            {
                token = dir.AccessToken;
                if(token == "")
                    token = await XmlParser.GetAccessToken(SpecificShow.LocalAssestMediaDirectory);
                folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
            }
            else
            {
                token = await XmlParser.GetAccessToken(SpecificShow.LocalAssestMediaDirectory);
                folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
            }

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

        private async void InfoEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (InfoEditButton.IsChecked == true)
            {
                // Save the information that is currently being displaye to test against when the editing is done
                showInfo.Rating = SpecificShow.Rating;
                showInfo.MalLink = SpecificShow.ExternalLink;
                showInfo.Title = SpecificShow.Title;

                TitleBox.Visibility = Visibility.Visible;
                TitleBox.Text = showInfo.Title;
                TitleTextBlock.Text = "";

                RatingBox.Visibility = Visibility.Visible;
                RatingTextBlock.Visibility = Visibility.Collapsed;

                if (showInfo.Rating == -1)
                {
                    RatingBox.Text = "";
                }

                LinkBox.Visibility = Visibility.Visible;
                LinkTextBlock.Visibility = Visibility.Collapsed;
                LinkHyperLink.Visibility = Visibility.Collapsed;

                ShowTagsListView.Visibility = Visibility.Collapsed;
                EditShowTagsButton.Visibility = Visibility.Visible;
            }
            else
            {
                if(showInfo.Title != TitleBox.Text)
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
                    try
                    {
                        Uri externalLink = new Uri(LinkBox.Text);

                        LinkHyperLink.Content = "Link";
                        LinkHyperLink.NavigateUri = externalLink;
                        LinkHyperLink.Visibility = Visibility.Visible;
                        LinkTextBlock.Visibility = Visibility.Collapsed;
                        LinkBox.Visibility = Visibility.Collapsed;

                        SpecificShow.ExternalLink = externalLink.ToString();
                        await XmlParser.SetMal(SpecificShow);
                    }
                    catch
                    {
                        if (LinkBox.Text == "")
                        {
                            // The user removed the link. Set the textblock back to say "Not set" and remove the hyperlink
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

                if(SpecificShow.ExternalLink != "")
                    LinkHyperLink.Visibility = Visibility.Visible;

                ShowTagsListView.Visibility = Visibility.Visible;
                EditShowTagsButton.Visibility = Visibility.Collapsed;

                // Need to set a check to only update if there was any changes made to the show info
                UpdateShowInView(SpecificShow);
            }

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

        private void ExitCompactMediaPageButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable any editing that was ongoing if editing was enabled
            if (InfoEditButton.IsChecked == true)
            {
                InfoEditButton.IsChecked = false;

                TitleBox.Visibility = Visibility.Collapsed;
                TitleTextBlock.Visibility = Visibility.Visible;

                RatingBox.Visibility = Visibility.Collapsed;
                RatingTextBlock.Visibility = Visibility.Visible;

                LinkBox.Visibility = Visibility.Collapsed;

                if (SpecificShow.ExternalLink != "")
                    LinkHyperLink.Visibility = Visibility.Visible;

                ShowTagsListView.Visibility = Visibility.Visible;
                EditShowTagsButton.Visibility = Visibility.Collapsed;
            }

                CompactMediaPopup.IsOpen = false;

            MediaInfoListView.IsItemClickEnabled = true;
            MediaInfoGrid.IsItemClickEnabled = true;
        }

        private void MediaTabButton_Click(object sender, RoutedEventArgs e)
        {
            MediaManagerPanel.Visibility = Visibility.Visible;
            ExclusionManagerPanel.Visibility = Visibility.Collapsed;
            TagsManagerPanel.Visibility = Visibility.Collapsed;
        }

        private void TagsTabButton_Click(object sender, RoutedEventArgs e)
        {
            MediaManagerPanel.Visibility = Visibility.Collapsed;
            ExclusionManagerPanel.Visibility = Visibility.Collapsed;
            TagsManagerPanel.Visibility = Visibility.Visible;
        }

        private void ExclusionTabButton_Click(object sender, RoutedEventArgs e)
        {
            MediaManagerPanel.Visibility = Visibility.Collapsed;
            ExclusionManagerPanel.Visibility = Visibility.Visible;
            TagsManagerPanel.Visibility = Visibility.Collapsed;
        }

        private void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            string value = TagsAddBox.Text.Trim();
            if (value != "")
            {
                // Check to see if the tag already exists
                bool exists = false;
                foreach(var tag in TagsList)
                {
                    if (tag.ToUpper() == value.ToUpper())
                    {
                        TagsAddBox.Text = "";
                        TagsAddBox.PlaceholderText = "Error: already exists";
                        exists = true;
                        break;
                    }
                }

                // If not then add the tag to the list
                if (!exists)
                {
                    TagsList.Add(value);
                    TagsList = new ObservableCollection<string>(TagsList.OrderBy(i => i));

                    // I have no clue how to set AdvancedCollectionView to receive updates from TagsList collection.. just resetting the 
                    // source and using it to sort / filter. The Orderby sort on the original list doesn't work for some reason.
                    acvTagsList = new AdvancedCollectionView(TagsList);
                    acvTagsList.SortDescriptions.Add(new SortDescription(SortDirection.Ascending));
                    TagsListView.ItemsSource = acvTagsList;

                    // Add the new tag to the XML file
                    XmlParser.SaveTagsToXml(TagsList);

                    TagsAddBox.Text = "";
                }
            }
        }

        // This has not been tested yet
        private async void DeleteTagButton_Click(object sender, RoutedEventArgs e)
        {
            int index = TagsListView.SelectedIndex;

            string value = TagsList[TagsListView.SelectedIndex];

            TagsList.RemoveAt(index);
            //acvTagsList.RemoveAt(index);

            // Remove tag from all shows
            XmlParser.RemoveTagFromAllShows(value);

            // Remove tag from tags xml
            XmlParser.SaveTagsToXml(TagsList);

            // Update AllShows to represent the tag changes
            AllShows = await XmlParser.LoadXmlMediaInfo();
        }

        private void ExitSettingsPopupButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = false;
            MediaInfoGrid.IsItemClickEnabled = true;
            MediaInfoListView.IsItemClickEnabled = true;
        }

        private void OnLayoutUpdated(object sender, object e)
        {
            if (CompactMediaPanel.ActualWidth == 0 && CompactMediaPanel.ActualHeight == 0)
            {
                return;
            }

            double ActualHorizontalOffset = this.CompactMediaPopup.HorizontalOffset;
            double ActualVerticalOffset = this.CompactMediaPopup.VerticalOffset;

            double NewHorizontalOffset = (Window.Current.Bounds.Width - CompactMediaPanel.ActualWidth) / 2;
            double NewVerticalOffset = (Window.Current.Bounds.Height - CompactMediaPanel.ActualHeight) / 2;

            if (ActualHorizontalOffset != NewHorizontalOffset || ActualVerticalOffset != NewVerticalOffset)
            {
                this.CompactMediaPopup.HorizontalOffset = NewHorizontalOffset;
                this.CompactMediaPopup.VerticalOffset = NewVerticalOffset;
            }
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

        private void OnNotificationLayoutUpdated(object sender, object e)
        {
            if (NotificationsPanel.ActualWidth == 0 && NotificationsPanel.ActualHeight == 0)
            {
                return;
            }

            double ActualHorizontalOffset = this.NotificationPopup.HorizontalOffset;
            double ActualVerticalOffset = this.NotificationPopup.VerticalOffset;

            double NewHorizontalOffset = (Window.Current.Bounds.Width - NotificationsPanel.ActualWidth) / 2;
            double NewVerticalOffset = (Window.Current.Bounds.Height - NotificationsPanel.ActualHeight) / 2;

            if (ActualHorizontalOffset != NewHorizontalOffset || ActualVerticalOffset != NewVerticalOffset)
            {
                this.NotificationPopup.HorizontalOffset = NewHorizontalOffset;
                this.NotificationPopup.VerticalOffset = NewVerticalOffset;
            }
        }

        private void OnSettingsLayoutUpdated(object sender, object e)
        {
            if (SettingsPanel.ActualWidth == 0 && SettingsPanel.ActualHeight == 0)
            {
                return;
            }

            double ActualHorizontalOffset = this.SettingsPopup.HorizontalOffset;
            double ActualVerticalOffset = this.SettingsPopup.VerticalOffset;

            double NewHorizontalOffset = (Window.Current.Bounds.Width - SettingsPanel.ActualWidth) / 2;
            double NewVerticalOffset = (Window.Current.Bounds.Height - SettingsPanel.ActualHeight) / 2;

            if (ActualHorizontalOffset != NewHorizontalOffset || ActualVerticalOffset != NewVerticalOffset)
            {
                this.SettingsPopup.HorizontalOffset = NewHorizontalOffset;
                this.SettingsPopup.VerticalOffset = NewVerticalOffset;
            }
        }

        private void OnErrorLayoutUpdated(object sender, object e)
        {
            if (ErrorsPanel.ActualWidth == 0 && ErrorsPanel.ActualHeight == 0)
            {
                return;
            }

            double ActualHorizontalOffset = this.ErrorPopup.HorizontalOffset;
            double ActualVerticalOffset = this.ErrorPopup.VerticalOffset;

            double NewHorizontalOffset = (Window.Current.Bounds.Width - ErrorsPanel.ActualWidth) / 2;
            double NewVerticalOffset = (Window.Current.Bounds.Height - ErrorsPanel.ActualHeight) / 2;

            if (ActualHorizontalOffset != NewHorizontalOffset || ActualVerticalOffset != NewVerticalOffset)
            {
                this.ErrorPopup.HorizontalOffset = NewHorizontalOffset;
                this.ErrorPopup.VerticalOffset = NewVerticalOffset;
            }
        }

        private void EditShowTagsButton_Click(object sender, RoutedEventArgs e)
        {
            CompactMediaPopup.Visibility = Visibility.Collapsed;
            EditTagsPopup.IsOpen = true;

            // Set the new source for the Tags page if needed
            CurrentTagsListView.ItemsSource = SpecificShow.Tags;
        }

        private void ExitTagsEditButton_Click(object sender, RoutedEventArgs e)
        {
            // Exit the popup
            EditTagsPopup.IsOpen = false;
            CompactMediaPopup.Visibility = Visibility.Visible;
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
                UpdateShowInView(SpecificShow);
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (AllTagsListView.SelectedIndex != -1)
            {
                // Check if the tag already exists in the show tags list
                bool exists = false;
                foreach(var tag in SpecificShow.Tags)
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
                    UpdateShowInView(SpecificShow);
                }
            }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                string text = sender.Text.Trim();
                if (sender.Text != "")
                {
                    FilteredShows = new List<MediaInfo>();
                    FilteredShows = AllShows.Where(x => x.Title.Contains(text)).ToList();

                    // Limit the search down to shows with the current tags selected
                    var lst = TagListView.SelectedItems;
                    foreach (var tag in lst)
                    {
                        FilteredShows = FilteredShows.Where(x => x.Tags.Contains(tag.ToString())).ToList();
                    }

                    ObservableCollection<string> searchTitles = new ObservableCollection<string>();

                    foreach (var show in FilteredShows)
                        searchTitles.Add(show.Title);


                    sender.ItemsSource = searchTitles;

                    // Set the listview / gridview item source to only display the items in searchResult
                    MediaInfoListView.ItemsSource = FilteredShows;
                    MediaInfoGrid.ItemsSource = FilteredShows;
                }
                else
                {
                    //sender.ItemsSource = new string[] { "No suggestions..." };

                    FilteredShows = new List<MediaInfo>();
                    FilteredShows = AllShows.ToList();

                    // Limit the search down to shows with the current tags selected
                    var lst = TagListView.SelectedItems;
                    foreach (var tag in lst)
                    {
                        FilteredShows = FilteredShows.Where(x => x.Tags.Contains(tag.ToString())).ToList();
                    }

                    MediaInfoListView.ItemsSource = FilteredShows;
                    MediaInfoGrid.ItemsSource = FilteredShows;
                }
            }
        }

        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            string text = args.SelectedItem.ToString();

            // Set the listview / gridview item source to only display the selected item
            FilteredShows = new List<MediaInfo>();
            FilteredShows = AllShows.Where(x => x.Title.Equals(text)).ToList();

            MediaInfoListView.ItemsSource = FilteredShows;
            MediaInfoGrid.ItemsSource = FilteredShows;
        }

        private void UpdateShowInView(MediaInfo show)
        {
            // Once a show information has been changed the show must be updated in the AllShows and FilteredShows lists
            var originalAllShow = AllShows.Where(x => x.UserDirectory == show.UserDirectory).FirstOrDefault();
            var index = AllShows.IndexOf(originalAllShow);

            if (index != -1)
                AllShows[index] = show;

            var originalFilteredShow = FilteredShows.Where(x => x.UserDirectory == show.UserDirectory).FirstOrDefault();
            index = FilteredShows.IndexOf(originalFilteredShow);

            if (index != -1)
                FilteredShows[index] = show;

            AllShows = new ObservableCollection<MediaInfo>(AllShows.OrderBy(i => i.Title));
            FilteredShows = new List<MediaInfo>(FilteredShows.OrderBy(i => i.Title));

            UpdateMediaViews();
        }

        private void UpdateMediaViews ()
        {
            // Need to update the grid/list view depdning on what is in the search bar and tags list
            string text = AutoSearchBox.Text.Trim();

            if (text != "")
            {
                //searchResult = new List<MediaInfo>();
                FilteredShows = AllShows.Where(x => x.Title.Contains(text)).ToList();

                // Limit the search down to shows with the current tags selected
                var lst = TagListView.SelectedItems;
                foreach (var tag in lst)
                {
                    FilteredShows = FilteredShows.Where(x => x.Tags.Contains(tag.ToString())).ToList();
                }

                // Set the listview / gridview item source to only display the items in searchResult
                MediaInfoListView.ItemsSource = FilteredShows;
                MediaInfoGrid.ItemsSource = FilteredShows;
            }
            else
            {
                //searchResult = new List<MediaInfo>();
                FilteredShows = AllShows.ToList();

                // Limit the search down to shows with the current tags selected
                var lst = TagListView.SelectedItems;
                foreach (var tag in lst)
                {
                    FilteredShows = FilteredShows.Where(x => x.Tags.Contains(tag.ToString())).ToList();
                }

                MediaInfoListView.ItemsSource = FilteredShows;
                MediaInfoGrid.ItemsSource = FilteredShows;
            }
        }

        private void CreateListVideoGroup()
        {
            // Clear everything from the VideoListPanel
            if(VideoListPanel.Children.Count > 0)
            {
                while(VideoListPanel.Children.Count != 0)
                    VideoListPanel.Children.RemoveAt(0);
            }

            // Break the episodelist into groups based on parent folder
            // Create a new list for each unique parent folder found
            VideoGroups = new List<List<string>>();
            List<string> names = new List<string>();
            foreach(var episode in SpecificShow.EpisodeList)
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
                var index = names.FindIndex(x=>x.ToString() == dirName);

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

                newSeasonList.Name = group;
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

        private async void FavoriteButton_Checked(object sender, RoutedEventArgs e)
        {
            SpecificShow.Favorite = true;
            await XmlParser.SetFavorite(SpecificShow);
            UpdateShowInView(SpecificShow);
        }

        private async void FavoriteButton_Unchecked(object sender, RoutedEventArgs e)
        {
            SpecificShow.Favorite = false;
            await XmlParser.SetFavorite(SpecificShow);
            UpdateShowInView(SpecificShow);
        }

        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            // Load all media that have favorite set to true
            FilteredShows = AllShows.Where(x => x.Favorite == true).ToList();
            MediaInfoGrid.ItemsSource = FilteredShows;
            MediaInfoListView.ItemsSource = FilteredShows;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset all filters/searches and show all media
            TagListView.SelectedItems.Clear();
            AutoSearchBox.Text = "";
            FilteredShows = AllShows.ToList();
            MediaInfoGrid.ItemsSource = FilteredShows;
            MediaInfoListView.ItemsSource = FilteredShows;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Make a popup that asks the user if they are sure they want to remove the show
            CompactMediaPopup.IsOpen = false;
            NotificationPopup.IsOpen = true;
            NotificationText.Text = "Are you sure you want to remove this title? The path will be added to the excluded media list in the settings page.";
        }

        private async void Confirmation_Click(object sender, RoutedEventArgs e)
        {
            // Add the show to the exluded shows in the associated MediaDir
            var directory = MediaDirs.Where(x => x.LocalAssetFolder == SpecificShow.LocalAssestMediaDirectory).FirstOrDefault();
            var pos = MediaDirs.IndexOf(directory);
            if (pos != -1)
            {
                MediaDirs.ElementAt(pos).ExcludedMedia.Add(SpecificShow.UserDirectory);
                await XmlParser.SetExcludedDirectories(MediaDirs.ElementAt(pos));
            }

            // Remove the show from the MediaInfo xml
            await XmlParser.DeleteSingleMediaFromXML(SpecificShow);

            // Remove the show from the AllShows
            var originalAllShow = AllShows.Where(x => x.UserDirectory == SpecificShow.UserDirectory).FirstOrDefault();
            var index = AllShows.IndexOf(originalAllShow);

            if (index != -1)
                AllShows.RemoveAt(index);

            // Update the list/grid views
            UpdateMediaViews();

            // Close the popups
            CompactMediaPopup.IsOpen = false;
            NotificationPopup.IsOpen = false;

            // Update the ExcludedMedia list source
            AllExcludedMedia.Add(SpecificShow.UserDirectory);
            AllExcludedMedia = new ObservableCollection<string>(AllExcludedMedia.OrderBy(i => i));
            ExcludedMediaListView.ItemsSource = AllExcludedMedia;

            // Re-enable the media views
            MediaInfoListView.IsItemClickEnabled = true;
            MediaInfoGrid.IsItemClickEnabled = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NotificationPopup.IsOpen = false;
            CompactMediaPopup.IsOpen = true;
        }

        private async void CheckForDirectoryUpdates()
        {
            AllExcludedMedia = new ObservableCollection<string>();

            // Loop through all media directories and consolidate all excluded media into a single list
            for(int i = 0; i<MediaDirs.Count; i++)
            {
                for(int j = 0; j<MediaDirs.ElementAt(i).ExcludedMedia.Count; j++)
                {
                    AllExcludedMedia.Add(MediaDirs.ElementAt(i).ExcludedMedia.ElementAt(j));
                }
            }

            // Traverse through the directories looking for shows not added to the XML and are not in the excluded list
            foreach (var mediaDir in MediaDirs)
            {
                StorageFolder folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(mediaDir.AccessToken);
                foreach (var item in await folder.GetFoldersAsync())
                {
                    // Check if the item path exists in the mediaDir excluded list
                    //int index = mediaDir.ExcludedMedia.FindIndex(x => x.ToString() == item.Path);
                    bool found = false;
                    foreach(var exclusion in mediaDir.ExcludedMedia)
                    {
                        if (exclusion == item.Path)
                        {
                            found = true;
                            break;
                        }
                    }

                    // If it does not exist then see if the show already exists in the AllShows object
                    if (!found)
                    {
                        found = false;
                        foreach (var show in AllShows)
                        {
                            if (show.UserDirectory == item.Path)
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    // If the media was not found in AllShows then it needs to be read and added to the AllShows object
                    if (!found)
                    {
                        StorageFolder originalPosition = await ApplicationData.Current.LocalFolder.GetFolderAsync(folder.Name);
                        await originalPosition.CreateFolderAsync(item.DisplayName);
                        StorageFolder appSubFolder = await originalPosition.GetFolderAsync(item.DisplayName);

                        SpecificShow = null;
                        SpecificShow = new MediaInfo();
                        SpecificShow.EpisodeList = new ObservableCollection<EpisodeInfo>();

                        await RetrieveShowsFromMediaDir(item, appSubFolder);

                        // No media files were found. This sub directory must not actually be a show storage directory. Don't import anything.
                        if (SpecificShow.EpisodeList.Count != 0)
                        {
                            SpecificShow.LocalAssestMediaDirectory = originalPosition.Path;
                            SpecificShow.UserDirectory = item.Path;
                            SpecificShow.Title = item.Name;

                            // Check if a cover photo was found.
                            if (SpecificShow.CoverImage == null)
                            {
                                // Not found so set the image source to the placeholder image
                                SpecificShow.CoverImage = "/Assets/TempPic.png";
                            }

                            AllShows.Add(SpecificShow);
                            await XmlParser.AddShowToXml(SpecificShow, mediaDir);
                        }
                    }
                }
            }

            // Update the list/grid view
            UpdateMediaViews();
        }

        private async void RemoveExclusionButton_Click(object sender, RoutedEventArgs e)
        {
            // Find and update the MediaDir that is associated with the removed exclusion
            foreach (var mediaDir in MediaDirs)
            {
                foreach (var exclusion in mediaDir.ExcludedMedia)
                {
                    if (exclusion == ExcludedMediaListView.SelectedItem.ToString())
                    {
                        mediaDir.ExcludedMedia.Remove(exclusion);
                        await XmlParser.SetExcludedDirectories(mediaDir);
                        break;
                    }
                }
            }

            // Remove the exclusion path from the listview source
            AllExcludedMedia.Remove(ExcludedMediaListView.SelectedItem.ToString());
            ExcludedMediaListView.ItemsSource = AllExcludedMedia;

            // Run the directory traversal to retreive the show information
            CheckForDirectoryUpdates();
        }

        private async void RescanMediaDirButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear the AllShows object
            AllShows = null;
            AllShows = new ObservableCollection<MediaInfo>();

            // Clear the MediaInfo xml
            await XmlParser.ClearMediaInfo();

            // Traverse through the MediaDirs and re-populate the xml
            foreach(var mediaDir in MediaDirs)
            {
                StorageFolder folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(mediaDir.AccessToken);
                StorageFolder originalLocalAssetDir = await ApplicationData.Current.LocalFolder.GetFolderAsync(folder.Name);
                await originalLocalAssetDir.DeleteAsync();

                await ApplicationData.Current.LocalFolder.CreateFolderAsync(folder.Name);
                StorageFolder originalPosition = await ApplicationData.Current.LocalFolder.GetFolderAsync(folder.Name);

                await XmlParser.AddMediaDirToXml(mediaDir);

                foreach (var item in await folder.GetFoldersAsync())
                {
                    // Make sure the item isn't an excluded media 
                    bool found = false;
                    foreach (var exclusion in mediaDir.ExcludedMedia)
                    {
                        if (exclusion == item.Path)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        await originalPosition.CreateFolderAsync(item.DisplayName);
                        StorageFolder appSubFolder = await originalPosition.GetFolderAsync(item.DisplayName);

                        SpecificShow = null;
                        SpecificShow = new MediaInfo();
                        SpecificShow.EpisodeList = new ObservableCollection<EpisodeInfo>();

                        await RetrieveShowsFromMediaDir(item, appSubFolder);

                        // No media files were found. This sub directory must not actually be a show storage directory. Don't import anything.
                        if (SpecificShow.EpisodeList.Count != 0)
                        {
                            SpecificShow.LocalAssestMediaDirectory = originalPosition.Path;
                            SpecificShow.UserDirectory = item.Path;
                            SpecificShow.Title = item.Name;

                            // Check if a cover photo was found.
                            if (SpecificShow.CoverImage == null)
                            {
                                // Not found so set the image source to the placeholder image
                                SpecificShow.CoverImage = "/Assets/TempPic.png";
                            }

                            AllShows.Add(SpecificShow);
                            await XmlParser.AddShowToXml(SpecificShow, mediaDir);
                        }
                    }
                }
            }

            // Get all media related information from the xml
            AllShows = await XmlParser.LoadXmlMediaInfo();
            FilteredShows = new List<MediaInfo>();
            FilteredShows = AllShows.ToList();

            // Update the list/grid view
            UpdateMediaViews();
        }

        private void Okay_Click(object sender, RoutedEventArgs e)
        {
            ErrorPopup.IsOpen = false;
        }
    }
}
