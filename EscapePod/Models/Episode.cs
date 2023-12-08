using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace EscapePod.Models
{
    public class Episode : INotifyPropertyChanged
    {
        private string _episodeName;
        private Uri _episodeUri;
        private string _localPath;
        private string _description;
        private bool _isDownloading;
        private bool _isDownloaded;
        private double _timestamp;
        private DateTime? _lastPlayed;
        private DateTime? _publishDate;
        private Podcast _podcast;
        private double _episodeLength;
        private bool _episodeFinished;
        private string _subtitle;
        private string _author;
        private bool _isExplicit;
        private string _summary;
        private Uri _imageUri;
        private string _audioFileType;
        private string _audioFileSize;

        public Episode(Podcast podcast, string episodeName, Uri episodeUri, string description, double timestamp, DateTime? publishDate, TimeSpan duration, string subtitle, string author, bool isExplicit, string summary, string imageUri, string audioFileSize, string audioFileType, bool isDownloaded, string localPath)
        {
            _podcast = podcast;
            _episodeName = episodeName;
            _episodeUri = episodeUri;
            _description = description;
            _timestamp = timestamp;
            _isDownloading = false;
            _isDownloaded = false;
            _publishDate = publishDate;
            _episodeFinished = false;
            _episodeLength = duration.TotalSeconds;

            _subtitle = subtitle;
            _author = author;
            _isExplicit = isExplicit;
            _summary = summary;
            _imageUri = string.IsNullOrEmpty(imageUri) ? podcast.TitleCard : new Uri(imageUri);
            _audioFileSize = audioFileSize;
            _audioFileType = audioFileType;
            _isDownloaded = isDownloaded;
            _localPath = localPath;
        }

        [JsonIgnore]
        public Podcast Podcast
        {
            get
            {
                return _podcast;
            }
            set
            {
                _podcast = value;
                OnPropertyChanged();
            }
        }

        public string EpisodeName
        {
            get
            {
                return _episodeName;
            }
        }

        public Uri EpisodeUri
        {
            get
            {
                return _episodeUri;
            }
        }

        public string Description
        {
            get
            {
                return _description;
            }
        }

        [JsonIgnore]
        public bool IsDownloading
        {
            get
            {
                return _isDownloading;
            }
            set
            {
                _isDownloading = value;
                OnPropertyChanged();
            }
        }

        public bool IsDownloaded
        {
            get
            {
                return _isDownloaded;
            }
            set
            {
                _isDownloaded = value;
                OnPropertyChanged();
            }
        }

        public double Timestamp
        {
            get
            {
                return _timestamp;
            }
            set
            {
                _timestamp = value;
                OnPropertyChanged();
            }
        }

        public DateTime? LastPlayed
        {
            get
            {
                return _lastPlayed;
            }
            set
            {
                _lastPlayed = value;
                OnPropertyChanged();
            }
        }

        public DateTime? PublishDate
        {
            get
            {
                return _publishDate;
            }
        }

        public string LocalPath
        {
            get
            {
                return _localPath;
            }
            set
            {
                _localPath = value;
                OnPropertyChanged();
            }
        }

        public double EpisodeLength
        {
            get
            {
                return _episodeLength;
            }
            set
            {
                _episodeLength = value;
                OnPropertyChanged();
            }
        }

        public bool EpisodeFinished
        {
            get
            {
                return _episodeFinished;
            }
            set
            {
                _episodeFinished = value;
                OnPropertyChanged();
            }
        }

        public string Subtitle
        {
            get
            {
                return _subtitle;
            }
        }

        public string Author
        {
            get
            {
                return _author;
            }
        }

        public bool IsExplicit
        {
            get
            {
                return _isExplicit;
            }
        }

        public string Summary
        {
            get
            {
                return _summary;
            }
        }

        public Uri ImageUri
        {
            get
            {
                return _imageUri;
            }
        }

        public string AudioFileType
        {
            get
            {
                return _audioFileType;
            }
        }

        public string AudioFileSize
        {
            get
            {
                return _audioFileSize;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
