using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
        private bool isExplicit;
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
                this.OnPropertyChanged();
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
                this.OnPropertyChanged();
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
                this.OnPropertyChanged();
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
                this.OnPropertyChanged();
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
                this.OnPropertyChanged();
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
                this.OnPropertyChanged();
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
                this.OnPropertyChanged();
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
                this.OnPropertyChanged();
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
                this.OnPropertyChanged();
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
                this.OnPropertyChanged();
            }
        }

        public bool IsExplicit
        {
            get
            {
                return isExplicit;
            }

            set
            {
                isExplicit = value;
                this.OnPropertyChanged();
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
                this.OnPropertyChanged();
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
                this.OnPropertyChanged();
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
                this.OnPropertyChanged();
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
                this.OnPropertyChanged();
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
                this.OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
