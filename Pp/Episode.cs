using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pp
{
    public class Episode : INotifyPropertyChanged
    {
        public Episode(Podcast podcast, string episodeName, int episodeNumber, Uri episodeUri, string description, double timestamp, DateTime? publishDate)
        {
            this.podcast = podcast;
            this.episodeName = episodeName;
            this.episodeNumber = episodeNumber;
            this.episodeUri = episodeUri;
            this.description = description;
            this.timestamp = timestamp;
            this.isDownloading = false;
            this.isDownloaded = false;
            this.publishDate = publishDate;
        }

        private string episodeName;
        private int episodeNumber;
        private Uri episodeUri;
        private string localPath;
        private string description;
        private bool isDownloading;
        private bool isDownloaded;
        private double timestamp;
        private DateTime? lastPlayed;
        private DateTime? publishDate;
        private Podcast podcast;
        private double episodeLength;

        [JsonIgnore]
        public Podcast Podcast
        {
            get
            {
                return podcast;
            }

            set
            {
                podcast = value;
                this.OnPropertyChanged(nameof(Podcast));
            }
        }

        public string EpisodeName
        {
            get
            {
                return episodeName;
            }

            set
            {
                episodeName = value;
                this.OnPropertyChanged(nameof(EpisodeName));
            }
        }

        public int EpisodeNumber
        {
            get
            {
                return episodeNumber;
            }

            set
            {
                episodeNumber = value;
                this.OnPropertyChanged(nameof(EpisodeNumber));
            }
        }

        public Uri EpisodeUri
        {
            get
            {
                return episodeUri;
            }

            set
            {
                episodeUri = value;
                this.OnPropertyChanged(nameof(EpisodeUri));
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

        public bool IsDownloading
        {
            get
            {
                return isDownloading;
            }

            set
            {
                isDownloading = value;
                this.OnPropertyChanged(nameof(IsDownloading));
            }
        }

        public bool IsDownloaded
        {
            get
            {
                return isDownloaded;
            }

            set
            {
                isDownloaded = value;
                this.OnPropertyChanged(nameof(IsDownloaded));
            }
        }

        public double Timestamp
        {
            get
            {
                return timestamp;
            }

            set
            {
                timestamp = value;
                this.OnPropertyChanged(nameof(Timestamp));
            }
        }

        public DateTime? LastPlayed
        {
            get
            {
                return lastPlayed;
            }

            set
            {
                lastPlayed = value;
                this.OnPropertyChanged(nameof(LastPlayed));
            }
        }

        public DateTime? PublishDate
        {
            get
            {
                return publishDate;
            }

            set
            {
                publishDate = value;
                this.OnPropertyChanged(nameof(PublishDate));
            }
        }

        public string LocalPath
        {
            get
            {
                return localPath;
            }

            set
            {
                localPath = value;
                this.OnPropertyChanged(nameof(LocalPath));
            }
        }

        public double EpisodeLength
        {
            get
            {
                return episodeLength;
            }

            set
            {
                episodeLength = value;
                this.OnPropertyChanged(nameof(EpisodeLength));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
