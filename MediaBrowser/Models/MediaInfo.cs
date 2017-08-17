using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Models
{
    public class MediaInfo
    {
        public int MediaId { get; set; }
        public string Title { get; set; }
        public string CoverImage { get; set; }
    }

    public class MediaManager
    {
        public static List<MediaInfo> GetMedia()
        {
            var media = new List<MediaInfo>();

            media.Add(new MediaInfo { MediaId = 11, Title = "C The Money of Soul and Possibility Control", CoverImage = "Assets/11.jpg" });
            media.Add(new MediaInfo { MediaId = 1, Title = "Attack on Titan", CoverImage="Assets/1.jpg" });
            media.Add(new MediaInfo { MediaId = 2, Title = "Barakamon", CoverImage = "Assets/2.jpg" });
            media.Add(new MediaInfo { MediaId = 3, Title = "Beautiful Bones", CoverImage = "Assets/3.jpg" });
            media.Add(new MediaInfo { MediaId = 4, Title = "Ben-To", CoverImage = "Assets/4.jpg" });
            media.Add(new MediaInfo { MediaId = 5, Title = "Blood Blockade Battlefront", CoverImage = "Assets/5.jpg" });
            media.Add(new MediaInfo { MediaId = 6, Title = "Blue Exorcist", CoverImage = "Assets/6.jpg" });
            media.Add(new MediaInfo { MediaId = 7, Title = "Blue Spring Ride", CoverImage = "Assets/7.jpg" });
            media.Add(new MediaInfo { MediaId = 8, Title = "Bokura Ga Ita", CoverImage = "Assets/8.jpg" });
            media.Add(new MediaInfo { MediaId = 9, Title = "BTOOOM!", CoverImage = "Assets/9.jpg" });
            media.Add(new MediaInfo { MediaId = 10, Title = "Bunny Drop", CoverImage = "Assets/10.jpg" });

            return media;
        }
    }
}
