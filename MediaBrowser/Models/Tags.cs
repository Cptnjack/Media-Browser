using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Models
{
    public static class Tags
    {
        public static async Task<ObservableCollection<string>> LoadDefaultTags()
        {
            ObservableCollection<string>  TagsList = new ObservableCollection<string>();

            TagsList.Add("Action");
            TagsList.Add("Adventure");
            TagsList.Add("Cars");
            TagsList.Add("Comedy");
            TagsList.Add("Demons");
            TagsList.Add("Drama");
            TagsList.Add("Fantasy");
            TagsList.Add("Game");
            TagsList.Add("Harem");
            TagsList.Add("Historical");
            TagsList.Add("Horror");
            TagsList.Add("Josei");
            TagsList.Add("Magic");
            TagsList.Add("Movie");
            TagsList.Add("Mecha");
            TagsList.Add("Military");
            TagsList.Add("Music");
            TagsList.Add("Mystery");
            TagsList.Add("Parody");
            TagsList.Add("Psychological");
            TagsList.Add("Romance");
            TagsList.Add("Samurai");
            TagsList.Add("School");
            TagsList.Add("Sci-Fi");
            TagsList.Add("Seinen");
            TagsList.Add("Shoujo");
            TagsList.Add("Shounen");
            TagsList.Add("Slice of Life");
            TagsList.Add("Space");
            TagsList.Add("Sports");
            TagsList.Add("Super Power");
            TagsList.Add("Supernatural");
            TagsList.Add("Thriller");
            TagsList.Add("TV Show");

            return TagsList;
        }

        public static async Task<ObservableCollection<string>> LoadUserDefinedTags()
        {
            ObservableCollection<string> TagsList = new ObservableCollection<string>();

            TagsList = await XmlParser.LoadTags();

            return TagsList;
        }

        public static void SaveUserDefinedTags(ObservableCollection<string> TagsList)
        {
             XmlParser.SaveTagsToXml(TagsList);
        }
    }
}
