using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;

namespace MediaBrowser.Models
{
    class XmlParser
    {
        public static async Task<ObservableCollection<MediaInfo>> LoadXmlMediaInfo()
        {
            StorageFile newFile;
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            ObservableCollection<MediaInfo> mediaList = new ObservableCollection<MediaInfo>();

            if (await ApplicationData.Current.LocalFolder.TryGetItemAsync("MediaInfo.xml") == null)
            {
                await ApplicationData.Current.LocalFolder.CreateFileAsync("MediaInfo.xml");

                newFile = await folder.GetFileAsync("MediaInfo.xml");

                //XNamespace empNM = "urn:lst-emp:emp";

                XDocument xDoc = new XDocument(
                            new XDeclaration("1.0", "UTF-16", null),
                            new XElement("Media"));

                Stream mystream = await newFile.OpenStreamForWriteAsync();
                using (mystream)
                    xDoc.Save(mystream);
            }

            var file = await ApplicationData.Current.LocalFolder.GetFileAsync("MediaInfo.xml");

            XElement xelement = XElement.Load(file.Path);
            IEnumerable<XElement> mediaDirs = xelement.Elements();

            // Read the entire XML
            foreach (var mediaDir in mediaDirs)
            {
                IEnumerable<XElement> shows = mediaDir.Elements("Show");
                foreach (var show in shows)
                {
                    string UserDirectory = show.Element("UserDirectory").Value;
                    string LocalAssestMediaDirectory = show.Element("LocalAssestMediaDirectory").Value;
                    string Title = show.Element("Title").Value;
                    string CoverImage = show.Element("CoverImage").Value;
                    string Rating = show.Element("Rating").Value;
                    string Favorite = show.Element("Favorite").Value;
                    string ExternalLink = show.Element("ExternalLink").Value;
                    string Overview = show.Element("Overview").Value;
                    string Tags = show.Element("Tags").Value;

                    ObservableCollection<EpisodeInfo> episodeList = new ObservableCollection<EpisodeInfo>();

                    XElement EpisodesList = show.Element("Episodes");

                    IEnumerable<XElement> episodes = EpisodesList.Elements("Episode");
                    foreach (var episode in episodes)
                    {
                        EpisodeInfo singleEpisode = new EpisodeInfo();

                        string FileName = episode.Element("FileName").Value;
                        string Id = episode.Element("Id").Value;
                        string Path = episode.Element("Path").Value;

                        int value = Convert.ToInt32(Id);
                        singleEpisode.Id = value;
                        singleEpisode.Path = Path;
                        singleEpisode.FileName = FileName;

                        episodeList.Add(singleEpisode);
                    }

                    ObservableCollection<string> tagList = new ObservableCollection<string>();

                    XElement TagsList = show.Element("Tags");

                    IEnumerable<XElement> tags = TagsList.Elements("Tag");
                    foreach (var tag in tags)
                    {
                        string tagName = tag.Element("TagName").Value;
                        tagList.Add(tagName);
                    }

                    // Create a MediaInfo object and add it to the list
                    MediaInfo singleShow = new MediaInfo();

                    singleShow.UserDirectory = UserDirectory;
                    singleShow.LocalAssestMediaDirectory = LocalAssestMediaDirectory;
                    singleShow.Title = Title;
                    singleShow.CoverImage = CoverImage;
                    singleShow.Rating = Convert.ToDouble(Rating);

                    if (singleShow.Rating == -1)
                        singleShow.DisplayRating = "";
                    else
                    {
                        singleShow.DisplayRating = singleShow.Rating.ToString();
                    }

                    if (Favorite == "True")
                    {
                        singleShow.Favorite = true;
                    }
                    else
                        singleShow.Favorite = false;

                    singleShow.ExternalLink = ExternalLink;
                    singleShow.Overview = Overview;

                    singleShow.Tags = tagList;

                    singleShow.EpisodeList = episodeList;

                    mediaList.Add(singleShow);

                }
            }

            return mediaList;
        }

        public static async Task ClearMediaInfo()
        {
            StorageFile newFile;
            StorageFolder folder = ApplicationData.Current.LocalFolder;

            if (await ApplicationData.Current.LocalFolder.TryGetItemAsync("MediaInfo.xml") != null)
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync("MediaInfo.xml");
                await file.DeleteAsync();

                await ApplicationData.Current.LocalFolder.CreateFileAsync("MediaInfo.xml");

                newFile = await folder.GetFileAsync("MediaInfo.xml");

                //XNamespace empNM = "urn:lst-emp:emp";

                XDocument xDoc = new XDocument(
                            new XDeclaration("1.0", "UTF-16", null),
                            new XElement("Media"));

                Stream mystream = await newFile.OpenStreamForWriteAsync();
                using (mystream)
                    xDoc.Save(mystream);
            }
        }

        public static async Task<ObservableCollection<MediaDir>> LoadXmlMediaDirs()
        {
            ObservableCollection<MediaDir> mediaDirList = new ObservableCollection<MediaDir>();

            var file = await ApplicationData.Current.LocalFolder.GetFileAsync("MediaInfo.xml");

            XElement xelement = XElement.Load(file.Path);
            IEnumerable<XElement> mediaDirs = xelement.Elements();

            // Read the entire XML
            foreach (var mediaDir in mediaDirs)
            {
                string UserDirFolder = mediaDir.Element("UserPath").Value;
                string LocalAssetFolder = mediaDir.Element("AssetPath").Value;
                string Name = mediaDir.Element("Name").Value;
                string AccessToken = mediaDir.Element("AccessToken").Value;

                List<string> exclusionedPaths = new List<string>();
                XElement ExlusionsList = mediaDir.Element("ExcludedDirectories");
                IEnumerable<XElement> exclusions = ExlusionsList.Elements("Exlusion");
                foreach (var exclusion in exclusions)
                {
                    string exclusionsName = exclusion.Element("ExlusionName").Value;
                    exclusionedPaths.Add(exclusionsName);
                }


                MediaDir singleMediaDir = new MediaDir();
                singleMediaDir.UserDirFolder = UserDirFolder;
                singleMediaDir.LocalAssetFolder = LocalAssetFolder;
                singleMediaDir.Name = Name;
                singleMediaDir.AccessToken = AccessToken;
                singleMediaDir.ExcludedMedia = exclusionedPaths;
                mediaDirList.Add(singleMediaDir);
            }

                return mediaDirList;
        }

        public static async Task AddMediaDirToXml(MediaDir dirInfo)
        {
            var xmlDoc = await ApplicationData.Current.LocalFolder.GetFileAsync("MediaInfo.xml");

            XElement xEle = XElement.Load(xmlDoc.Path);
            xEle.Add(new XElement("MediaDirectory", new XAttribute("Id", dirInfo.Name),
                new XElement("UserPath", dirInfo.UserDirFolder),
                new XElement("AssetPath", dirInfo.LocalAssetFolder),
                new XElement("Name", dirInfo.Name),
                new XElement("AccessToken", dirInfo.AccessToken),
                new XElement("ExcludedDirectories"
                )));

            Stream mystream = await xmlDoc.OpenStreamForWriteAsync();
            using (mystream)
                xEle.Save(mystream);


        }

        public static async Task SetExcludedDirectories(MediaDir dirInfo)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("MediaInfo.xml");
            Stream stream = await file.OpenStreamForWriteAsync();

            using (stream)
            {
                XDocument doc = XDocument.Load(stream);

                doc.Descendants("MediaDirectory").Where(x => x.Element("Name").Value == dirInfo.Name).Select(x => x.Element("ExcludedDirectories")).SingleOrDefault().RemoveAll();

                foreach (var exclusion in dirInfo.ExcludedMedia)
                {
                    doc.Descendants("MediaDirectory").Where(x => x.Element("Name").Value == dirInfo.Name).Select(x => x.Element("ExcludedDirectories")).FirstOrDefault()
                        .Add(new XElement("Exlusion",
                            new XElement("ExlusionName", exclusion)));
                }

                stream.SetLength(0);
                doc.Save(stream);
            }
        }

        public static async Task AddShowToXml(MediaInfo mediaInfo, MediaDir dirInfo)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("MediaInfo.xml");
            Stream stream = await file.OpenStreamForWriteAsync();

            using (stream)
            {
                XDocument doc = XDocument.Load(stream);

                doc.Descendants("MediaDirectory").Where(x => x.Element("Name").Value == dirInfo.Name).Select(x => x.Element("Name").Parent).FirstOrDefault()
                        .Add(
                        new XElement("Show",
                        new XElement("UserDirectory", mediaInfo.UserDirectory),
                        new XElement("LocalAssestMediaDirectory", mediaInfo.LocalAssestMediaDirectory),
                        new XElement("Title", mediaInfo.Title),
                        new XElement("CoverImage", mediaInfo.CoverImage),
                        new XElement("Rating", mediaInfo.Rating),
                        new XElement("Favorite", mediaInfo.Favorite),
                        new XElement("ExternalLink", mediaInfo.ExternalLink),
                        new XElement("Overview", mediaInfo.Overview),
                        new XElement("Tags", mediaInfo.Tags),
                        new XElement("Episodes"
                        )));

                stream.SetLength(0);
                doc.Save(stream);
            }

            AddEpisodesXml(mediaInfo);
        }

        public static async void AddEpisodesXml(MediaInfo mediaInfo)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("MediaInfo.xml");
            Stream stream = await file.OpenStreamForWriteAsync();

            using (stream)
            {
                XDocument doc = XDocument.Load(stream);

                int id = 1;
                foreach (var episode in mediaInfo.EpisodeList)
                {
                    doc.Descendants("Show").Where(x => x.Element("UserDirectory").Value == mediaInfo.UserDirectory).Select(x => x.Element("Episodes")).FirstOrDefault()
                        .Add(new XElement("Episode",
                            new XElement("FileName", episode.FileName),
                            new XElement("Id", id),
                            new XElement("Path", episode.Path)));

                    id++;
                }

                stream.SetLength(0);
                doc.Save(stream);
            }
        }

        // Overloaded function - Deletes all shows in a given media directory
        public static async void DeleteFromXML(MediaDir dirInfo)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("MediaInfo.xml");
            Stream stream = await file.OpenStreamForWriteAsync();

            using (stream)
            {
                XDocument doc = XDocument.Load(stream);

                doc.Descendants("MediaDirectory").Where(x => x.Element("Name").Value == dirInfo.Name).SingleOrDefault().RemoveNodes();
                doc.Descendants("MediaDirectory").Where(x => x.Attribute("Id").Value == dirInfo.Name).SingleOrDefault().Remove();

                stream.SetLength(0);
                doc.Save(stream);
            }
        }

        // Overloaded function - Deletes a single show from a media directory
        public static async Task DeleteSingleMediaFromXML(MediaInfo mediaInfo)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("MediaInfo.xml");
            Stream stream = await file.OpenStreamForWriteAsync();

            using (stream)
            {
                XDocument doc = XDocument.Load(stream);

                var node = doc.Descendants("Show").Where(x => x.Element("UserDirectory").Value == mediaInfo.UserDirectory).SingleOrDefault();
                var parentNode = node.Parent;

                node.RemoveNodes();
                node.Remove();

                stream.SetLength(0);
                doc.Save(stream);
            }
        }

        public static async Task<string> GetAccessToken(string dirPath)
        {
            string token = "";

            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("MediaInfo.xml");
            Stream stream = await file.OpenStreamForWriteAsync();

            using (stream)
            {
                XDocument doc = XDocument.Load(stream);

                var node = doc.Descendants("MediaDirectory").Where(x => x.Element("AssetPath").Value == dirPath).Select(x => x.Element("AccessToken")).SingleOrDefault();

                if (node != null)
                {
                    token = node.Value;
                }
            }

            return token;
        }

        public static async Task SetTitle(MediaInfo mediaInfo)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("MediaInfo.xml");
            Stream stream = await file.OpenStreamForWriteAsync();

            using (stream)
            {
                XDocument doc = XDocument.Load(stream);

                var node = doc.Descendants("Show").Where(x => x.Element("UserDirectory").Value == mediaInfo.UserDirectory).Select(x => x.Element("Title")).FirstOrDefault();

                if (node != null)
                {
                    node.SetValue(mediaInfo.Title);

                    stream.SetLength(0);
                    doc.Save(stream);
                }
            }
        }

        public static async Task SetRating(MediaInfo mediaInfo)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("MediaInfo.xml");
            Stream stream = await file.OpenStreamForWriteAsync();

            using (stream)
            {
                XDocument doc = XDocument.Load(stream);

                var node = doc.Descendants("Show").Where(x => x.Element("UserDirectory").Value == mediaInfo.UserDirectory).Select(x => x.Element("Rating")).FirstOrDefault();

                if (node != null)
                {
                    node.SetValue(mediaInfo.Rating);

                    stream.SetLength(0);
                    doc.Save(stream);
                }
            }
        }

        public static async Task SetMal(MediaInfo mediaInfo)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("MediaInfo.xml");
            Stream stream = await file.OpenStreamForWriteAsync();

            using (stream)
            {
                XDocument doc = XDocument.Load(stream);

                var node = doc.Descendants("Show").Where(x => x.Element("UserDirectory").Value == mediaInfo.UserDirectory).Select(x => x.Element("ExternalLink")).FirstOrDefault();

                if (node != null)
                {
                    node.SetValue(mediaInfo.ExternalLink);

                    stream.SetLength(0);
                    doc.Save(stream);
                }
            }
        }

        public static async Task SetTags(MediaInfo mediaInfo)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("MediaInfo.xml");
            Stream stream = await file.OpenStreamForWriteAsync();

            using (stream)
            {
                XDocument doc = XDocument.Load(stream);

                var node = doc.Descendants("Show").Where(x => x.Element("UserDirectory").Value == mediaInfo.UserDirectory).Select(x => x.Element("Tags")).FirstOrDefault();

                if (node != null)
                {
                    // Clear the tags and re-enter them so they are in alphabetical order
                    node.RemoveAll();

                    foreach (var tag in mediaInfo.Tags)
                    {
                        doc.Descendants("Show").Where(x => x.Element("UserDirectory").Value == mediaInfo.UserDirectory).Select(x => x.Element("Tags")).FirstOrDefault()
                            .Add(new XElement("Tag",
                                new XElement("TagName", tag)));
                    }

                    stream.SetLength(0);
                    doc.Save(stream);
                }
            }
        }

        public static async Task SetFavorite(MediaInfo mediaInfo)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("MediaInfo.xml");
            Stream stream = await file.OpenStreamForWriteAsync();

            using (stream)
            {
                XDocument doc = XDocument.Load(stream);

                var node = doc.Descendants("Show").Where(x => x.Element("UserDirectory").Value == mediaInfo.UserDirectory).Select(x => x.Element("Favorite")).FirstOrDefault();

                if (node != null)
                {
                    if (mediaInfo.Favorite == true)
                        node.SetValue("True");
                    else
                        node.SetValue("False");

                    stream.SetLength(0);
                    doc.Save(stream);
                }

            }
        }

        public static async Task<UserSettings> LoadSettings()
        {

            StorageFile newFile;
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            UserSettings appSettings = new UserSettings();

            if (await ApplicationData.Current.LocalFolder.TryGetItemAsync("AppSettings.xml") == null)
            {
                await ApplicationData.Current.LocalFolder.CreateFileAsync("AppSettings.xml");

                newFile = await folder.GetFileAsync("AppSettings.xml");

                XDocument xDoc = new XDocument(
                            new XDeclaration("1.0", "UTF-16", null),
                            new XElement("Settings"));

                Stream mystream = await newFile.OpenStreamForWriteAsync();
                using (mystream)
                    xDoc.Save(mystream);

                AddSettingsToXml(appSettings);

                return appSettings;
            }

            var file = await ApplicationData.Current.LocalFolder.GetFileAsync("AppSettings.xml");

            XElement xelement = XElement.Load(file.Path);

            string ViewStyle = xelement.Element("ViewStyle").Value;
            string SortStyle = xelement.Element("SortStyle").Value;
            string ColorTheme = xelement.Element("ColorTheme").Value;
            string IndividualPagesEnabled = xelement.Element("IndividualPagesEnabled").Value;


            appSettings.ViewStyle = ViewStyle;
            appSettings.SortStyle = SortStyle;
            appSettings.ColorTheme = ColorTheme;

            if (IndividualPagesEnabled == "True")
            {
                appSettings.IndividualPagesEnabled = true;
            }
            else
                appSettings.IndividualPagesEnabled = false;


            return appSettings;
        }

        public static async void AddSettingsToXml(UserSettings settings)
        {
            var xmlDoc = await ApplicationData.Current.LocalFolder.GetFileAsync("AppSettings.xml");

            XElement xEle = XElement.Load(xmlDoc.Path);
            xEle.Add(
                new XElement("ViewStyle", settings.ViewStyle),
                new XElement("SortStyle", settings.SortStyle),
                new XElement("ColorTheme", settings.ColorTheme),
                new XElement("IndividualPagesEnabled", settings.IndividualPagesEnabled.ToString()
                ));

            Stream mystream = await xmlDoc.OpenStreamForWriteAsync();
            using (mystream)
                xEle.Save(mystream);
        }

        public static async Task SetViewStyle(UserSettings settings)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("AppSettings.xml");
            Stream stream = await file.OpenStreamForWriteAsync();

            using (stream)
            {
                XDocument doc = XDocument.Load(stream);

                var node = doc.Descendants("ViewStyle").FirstOrDefault();

                if (node != null)
                {
                    node.SetValue(settings.ViewStyle);

                    stream.SetLength(0);
                    doc.Save(stream);
                }
            }
        }

        public static async Task SetSortStyle(UserSettings settings)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("AppSettings.xml");
            Stream stream = await file.OpenStreamForWriteAsync();

            using (stream)
            {
                XDocument doc = XDocument.Load(stream);

                var node = doc.Descendants("SortStyle").FirstOrDefault();

                if (node != null)
                {
                    node.SetValue(settings.SortStyle);

                    stream.SetLength(0);
                    doc.Save(stream);
                }
            }
        }

        public static async Task SetColorTheme(UserSettings settings)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("AppSettings.xml");
            Stream stream = await file.OpenStreamForWriteAsync();

            using (stream)
            {
                XDocument doc = XDocument.Load(stream);

                var node = doc.Descendants("ColorTheme").FirstOrDefault();

                if (node != null)
                {
                    node.SetValue(settings.ColorTheme);

                    stream.SetLength(0);
                    doc.Save(stream);
                }
            }
        }

        public static async Task SetIndividualPagesEnabled(UserSettings settings)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("AppSettings.xml");
            Stream stream = await file.OpenStreamForWriteAsync();

            using (stream)
            {
                XDocument doc = XDocument.Load(stream);

                var node = doc.Descendants("IndividualPagesEnabled").FirstOrDefault();

                if (node != null)
                {
                    if(settings.IndividualPagesEnabled == true)
                        node.SetValue("True");
                    else
                        node.SetValue("False");

                    stream.SetLength(0);
                    doc.Save(stream);
                }
            }
        }

        // This reads the tags held in the Tags.xml file
        public static async Task<ObservableCollection<string>> LoadTags()
        {

            StorageFile newFile;
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            ObservableCollection<string> tagsList = new ObservableCollection<string>();

            if (await ApplicationData.Current.LocalFolder.TryGetItemAsync("Tags.xml") == null)
            {
                await ApplicationData.Current.LocalFolder.CreateFileAsync("Tags.xml");

                newFile = await folder.GetFileAsync("Tags.xml");

                XDocument xDoc = new XDocument(
                            new XDeclaration("1.0", "UTF-16", null),
                            new XElement("TagsList"));

                Stream mystream = await newFile.OpenStreamForWriteAsync();
                using (mystream)
                    xDoc.Save(mystream);

                return tagsList;
            }

            var file = await ApplicationData.Current.LocalFolder.GetFileAsync("Tags.xml");

            XElement xelement = XElement.Load(file.Path);
            IEnumerable<XElement> tags = xelement.Elements();

            // Read the entire XML
            foreach (var tag in tags)
            {
                string tagName = tag.Element("Name").Value;

                tagsList.Add(tagName);
            }

            return tagsList;
        }

        // This saves all tags supported to the Tags.xml
        public static async void SaveTagsToXml(ObservableCollection<string> tagsList)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("Tags.xml");
            Stream stream = await file.OpenStreamForWriteAsync();

            using (stream)
            {
                XDocument doc = XDocument.Load(stream);

                var node = doc.Descendants("TagsList").FirstOrDefault();

                if (node != null)
                {
                    node.RemoveAll();

                    foreach (var tag in tagsList)
                    {
                        doc.Descendants("TagsList").FirstOrDefault()
                            .Add(new XElement("Tag",
                                new XElement("Name", tag)));
                    }

                    stream.SetLength(0);
                    doc.Save(stream);
                }
            }
        }

        public static async void RemoveTagFromAllShows(string removedTag)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("MediaInfo.xml");
            Stream stream = await file.OpenStreamForWriteAsync();

            using (stream)
            {
                XDocument doc = XDocument.Load(stream);

                doc.Descendants("Tags").Elements("Tag").Where(x => x.Element("TagName").Value == removedTag).Remove();

              

                stream.SetLength(0);
                doc.Save(stream);
            }
        }

        public static bool IsFileReady(String sFilename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(sFilename, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    if (inputStream.Length > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
