using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace EscapePod.Models
{
    public class Episode : INotifyPropertyChanged
    {
        private string episodeName;
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
        private bool episodeFinished;
        private string subtitle;
        private string author;
        private bool isExplicid;
        private string summary;
        private Uri imageUri;
        private string audioFileType;
        private string audioFileSize;

        public Episode(Podcast podcast, string episodeName, Uri episodeUri, string description, double timestamp, DateTime? publishDate, TimeSpan duration, string subtitle, string author, bool isExplicid, string summary, string imageUri, string audioFileSize, string audioFileType, bool isDownloaded, string localPath)
        {
            this.podcast = podcast;
            this.episodeName = episodeName;
            this.episodeUri = episodeUri;
            this.description = description;
            this.timestamp = timestamp;
            this.isDownloading = false;
            this.isDownloaded = false;
            this.publishDate = publishDate;
            this.episodeFinished = false;
            this.episodeLength = duration.TotalSeconds;

            this.subtitle = subtitle;
            this.author = author;
            this.isExplicid = isExplicid;
            this.summary = summary;
            this.imageUri = string.IsNullOrEmpty(imageUri) ? podcast.TitleCard : new Uri(imageUri);
            this.audioFileSize = audioFileSize;
            this.audioFileType = audioFileType;
            this.isDownloaded = isDownloaded;
            this.localPath = localPath;
        }

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
        }

        public Uri EpisodeUri
        {
            get
            {
                return episodeUri;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        [JsonIgnore]
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

        public bool EpisodeFinished
        {
            get
            {
                return episodeFinished;
            }
            set
            {
                episodeFinished = value;
                this.OnPropertyChanged(nameof(EpisodeFinished));
            }
        }

        public string Subtitle
        {
            get
            {
                return subtitle;
            }
        }

        public string Author
        {
            get
            {
                return author;
            }
        }

        public bool IsExplicid
        {
            get
            {
                return isExplicid;
            }
        }

        public string Summary
        {
            get
            {
                return summary;
            }
        }

        public Uri ImageUri
        {
            get
            {
                return imageUri;
            }
        }

        public string AudioFileType
        {
            get
            {
                return audioFileType;
            }
        }

        public string AudioFileSize
        {
            get
            {
                return audioFileSize;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
