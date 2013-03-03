using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MFCChatClient
{
    public class MetricsPayload
    {
        public String fileno { get; set; }
    }

    public class User
    {
        [JsonProperty(PropertyName = "lv")]
        public MFCAccessLevel? AccessLevel { get; set; }
        [JsonProperty(PropertyName = "nm")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "sid")]
        public int? SessionId { get; set; }
        [JsonProperty(PropertyName = "uid")]
        public int? UserId { get; set; }
        [JsonProperty(PropertyName = "vs")]
        public MFCVideoState? VideoState { get; set; }
        [JsonProperty(PropertyName = "msg")]
        public string Message { get; set; } //used for chat messages
        [JsonProperty(PropertyName = "u")]
        public UserDetails UserDetails { get; set; }
        [JsonProperty(PropertyName = "m")]
        public ModelDetails ModelDetails { get; set; }

        //Update this user object with values from the passed user object
        public void Update(User u)
        {
            AccessLevel = u.AccessLevel ?? AccessLevel;
            Name = u.Name ?? Name;
            SessionId = u.SessionId ?? SessionId;
            VideoState = u.VideoState ?? VideoState;

            if (null != u.UserDetails)
            {
                if (null != UserDetails)
                {
                    UserDetails.Age = u.UserDetails.Age ?? UserDetails.Age;
                    UserDetails.Avatar = u.UserDetails.Avatar ?? UserDetails.Avatar;
                    UserDetails.Blurb = u.UserDetails.Blurb ?? UserDetails.Blurb;
                    UserDetails.Camserv = u.UserDetails.Camserv ?? UserDetails.Camserv;
                    UserDetails.ChatBackground = u.UserDetails.ChatBackground ?? UserDetails.ChatBackground;
                    UserDetails.ChatColor = u.UserDetails.ChatColor ?? UserDetails.ChatColor;
                    UserDetails.ChatOptions = u.UserDetails.ChatOptions ?? UserDetails.ChatOptions;
                    UserDetails.ChatFont = u.UserDetails.ChatFont ?? UserDetails.ChatFont;
                    UserDetails.City = u.UserDetails.City ?? UserDetails.City;
                    UserDetails.Country = u.UserDetails.Country ?? UserDetails.Country;
                    UserDetails.CreationDate = u.UserDetails.CreationDate ?? UserDetails.CreationDate;
                    UserDetails.Ethnic = u.UserDetails.Ethnic ?? UserDetails.Ethnic;
                    UserDetails.Photos = u.UserDetails.Photos ?? UserDetails.Photos;
                    UserDetails.Profile = u.UserDetails.Profile ?? UserDetails.Profile;
                }
                else
                    UserDetails = u.UserDetails;
            }

            if (null != u.ModelDetails)
            {
                if (null != ModelDetails)
                {
                    ModelDetails.Camscore = u.ModelDetails.Camscore ?? ModelDetails.Camscore;
                    ModelDetails.Continent = u.ModelDetails.Continent ?? ModelDetails.Continent;
                    ModelDetails.Flags = u.ModelDetails.Flags ?? ModelDetails.Flags;
                    ModelDetails.Kbit = u.ModelDetails.Kbit ?? ModelDetails.Kbit;
                    ModelDetails.LastNews = u.ModelDetails.LastNews ?? ModelDetails.LastNews;
                    ModelDetails.Mg = u.ModelDetails.Mg ?? ModelDetails.Mg;
                    ModelDetails.MissMFC = u.ModelDetails.MissMFC ?? ModelDetails.MissMFC;
                    ModelDetails.NewModel = u.ModelDetails.NewModel ?? ModelDetails.NewModel;
                    ModelDetails.Rank = u.ModelDetails.Rank ?? ModelDetails.Rank;
                    ModelDetails.Topic = u.ModelDetails.Topic ?? ModelDetails.Topic;
                }
                else
                    ModelDetails = u.ModelDetails;
            }
        }
    }
    public class UserDetails
    {
        [JsonProperty(PropertyName = "age")]
        public int? Age { get; set; }
        [JsonProperty(PropertyName = "avatar")]
        public int? Avatar { get; set; }
        [JsonProperty(PropertyName = "blurb")]
        public string Blurb { get; set; }
        [JsonProperty(PropertyName = "camserv")]
        public int? Camserv { get; set; }
        [JsonProperty(PropertyName = "chat_bg")]
        public int? ChatBackground { get; set; }
        [JsonProperty(PropertyName = "chat_color")]
        public string ChatColor { get; set; }
        [JsonProperty(PropertyName = "chat_opt")]
        public int? ChatOptions { get; set; }
        [JsonProperty(PropertyName = "chat_font")]
        public int? ChatFont { get; set; }
        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }
        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }
        [JsonProperty(PropertyName = "creation")]
        public int? CreationDate { get; set; } //TODO: convert this to a date
        [JsonProperty(PropertyName = "ethnic")]
        public string Ethnic { get; set; }
        [JsonProperty(PropertyName = "photos")]
        public int? Photos { get; set; } //I think this is a boolean
        [JsonProperty(PropertyName = "profile")]
        public int? Profile { get; set; } //this too
    }
    public class ModelDetails
    {
        [JsonProperty(PropertyName = "camscore")]
        public double? Camscore { get; set; }
        [JsonProperty(PropertyName = "continent")]
        public string Continent { get; set; }
        [JsonProperty(PropertyName = "flags")]
        public int? Flags { get; set; }
        [JsonProperty(PropertyName = "kbit")]
        public int? Kbit { get; set; }
        [JsonProperty(PropertyName = "lastnews")]
        public int? LastNews { get; set; } //date
        [JsonProperty(PropertyName = "mg")]
        public int? Mg { get; set; }
        [JsonProperty(PropertyName = "missmfc")]
        public int? MissMFC { get; set; }
        [JsonProperty(PropertyName = "new_model")]
        public int? NewModel { get; set; } //boolean
        [JsonProperty(PropertyName = "rank")]
        public int? Rank { get; set; }
        [JsonProperty(PropertyName = "topic")]
        public string Topic { get; set; }
    }
}
