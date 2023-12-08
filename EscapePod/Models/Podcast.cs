using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EscapePod.Models
{
    public class Podcast : INotifyPropertyChanged
    {
        private Uri uri;
        private string name;
        private Uri titleCard;
        private string localTitleCardPath;
        private string localPodcastPath;
        private string author;
        private string subtitle;
        private string description;
        private Uri website;
        private bool isExplicid;
        private string language;
        private string copyright;
        private int episodeCount;
        private DateTime? lastUpdate;
        private string id;
        private List<Episode> episodeList = new List<Episode>();
        
        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
                this.OnPropertyChanged(nameof(Name));
            }
        }

        public Uri TitleCard
        {
            get
            {
                return titleCard;
            }

            set
            {
                titleCard = value;
                this.OnPropertyChanged(nameof(TitleCard));
            }
        }

        public List<Episode> EpisodeList
        {
            get
            {
                return episodeList;
            }

            set
            {
                episodeList = value;
                this.OnPropertyChanged(nameof(EpisodeList));
            }
        }

        public Uri Uri
        {
            get
            {
                return uri;
            }

            set
            {
                uri = value;
                this.OnPropertyChanged(nameof(Uri));
            }
        }

        public string LocalTitleCardFileFullName
        {
            get
            {
                return localTitleCardPath;
            }

            set
            {
                localTitleCardPath = value;
                this.OnPropertyChanged(nameof(LocalTitleCardFileFullName));
            }
        }

        public string LocalPodcastPath
        {
            get
            {
                return localPodcastPath;
            }

            set
            {
                localPodcastPath = value;
                this.OnPropertyChanged(nameof(LocalPodcastPath));
            }
        }

        public string Author
        {
            get
            {
                return author;
            }

            set
            {
                author = value;
                this.OnPropertyChanged(nameof(Author));
            }
        }

        public string Subtitle
        {
            get
            {
                return subtitle;
            }

            set
            {
                subtitle = value;
                this.OnPropertyChanged(nameof(Subtitle));
            }
        }

        public string Description
        {
            get
            {
                return description;
            }

            set
            {
                description = value;
                this.OnPropertyChanged(nameof(Description));
            }
        }

        public Uri Website
        {
            get
            {
                return website;
            }

            set
            {
                website = value;
                this.OnPropertyChanged(nameof(Website));
            }
        }

        public bool IsExplicid
        {
            get
            {
                return isExplicid;
            }

            set
            {
                isExplicid = value;
                this.OnPropertyChanged(nameof(IsExplicid));
            }
        }

        public string Language
        {
            get
            {
                return language;
            }

            set
            {
                language = value;
                this.OnPropertyChanged(nameof(Language));
            }
        }

        public string Copyright
        {
            get
            {
                return copyright;
            }

            set
            {
                copyright = value;
                this.OnPropertyChanged(nameof(Copyright));
            }
        }

        public int EpisodeCount
        {
            get
            {
                return episodeCount;
            }

            set
            {
                episodeCount = value;
                this.OnPropertyChanged(nameof(EpisodeCount));
            }
        }

        public DateTime? LastUpdate
        {
            get
            {
                return lastUpdate;
            }

            set
            {
                lastUpdate = value;
                this.OnPropertyChanged(nameof(LastUpdate));
            }
        }

        public string Id
        {
            get
            {
                return id;
            }

            set
            {
                id = value;
                this.OnPropertyChanged(nameof(Id));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
