using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls;
using System.Xml;
using System.IO;
using System.Xml.Linq;

namespace MediaBrowser.Models
{
    public class MediaInfo
    {
        public string Title { get; set; }
        public string CoverImage { get; set; }
        public string MediaPageImage { get; set; }
        public double Rating { get; set; }
        public string DisplayRating { get; set; }
        public bool Favorite { get; set; }
        public string ExternalLink { get; set; }
        public string Overview { get; set; }
        public ObservableCollection<string> Tags { get; set; }
        public ObservableCollection<EpisodeInfo> EpisodeList;
        public string UserDirectory { get; set; }
        public string LocalAssestMediaDirectory { get; set; }

        public MediaInfo()
        {
            this.Title = "";
            this.CoverImage = "";
            this.MediaPageImage = "";
            this.Rating = -1;
            this.DisplayRating = "";
            this.Favorite = false;
            this.ExternalLink = "";
            this.Overview = "";
            this.Tags = new ObservableCollection<string>();
            this.EpisodeList = new ObservableCollection<EpisodeInfo>();
        }
    }

    public class MediaDir
    {
        public string Name { get; set; }
        public string UserDirFolder { get; set; }
        public string LocalAssetFolder { get; set; }
        public string AccessToken { get; set; }
        public List<string> ExcludedMedia { get; set; }

        public MediaDir()
        {
            Name = "";
            UserDirFolder = "";
            LocalAssetFolder = "";
            AccessToken = "";
            ExcludedMedia = new List<string>();
        }
    }

    public class EpisodeInfo
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string Path { get; set; }
        public BitmapImage Thumbnail;
        public StorageFile EpisodeFile { get; set; }

        public EpisodeInfo()
        {
            Id = 0;
            FileName = "";
            Path = "";
            EpisodeFile = null;
        }
    }
}
