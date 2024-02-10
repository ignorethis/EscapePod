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
            get => _podcast;
            set
            {
                _podcast = value;
                OnPropertyChanged();
            }
        }

        public string EpisodeName => _episodeName;

        public Uri EpisodeUri => _episodeUri;

        public string Description => _description;

        [JsonIgnore]
        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                _isDownloading = value;
                OnPropertyChanged();
            }
        }

        public bool IsDownloaded
        {
            get => _isDownloaded;
            set
            {
                _isDownloaded = value;
                OnPropertyChanged();
            }
        }

        public double Timestamp
        {
            get => _timestamp;
            set
            {
                _timestamp = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EpisodeProgress));
            }
        }

        public DateTime? LastPlayed
        {
            get => _lastPlayed;
            set
            {
                _lastPlayed = value;
                OnPropertyChanged();
            }
        }

        public DateTime? PublishDate => _publishDate;

        public string LocalPath
        {
            get => _localPath;
            set
            {
                _localPath = value;
                OnPropertyChanged();
            }
        }

        public double EpisodeLength
        {
            get => _episodeLength;
            set
            {
                _episodeLength = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EpisodeProgress));
            }
        }

        public bool EpisodeFinished
        {
            get => _episodeFinished;
            set
            {
                _episodeFinished = value;
                OnPropertyChanged();
            }
        }

        public double EpisodeProgress => _timestamp / _episodeLength * 100;

        public string Subtitle => _subtitle;

        public string Author => _author;

        public bool IsExplicit => _isExplicit;

        public string Summary => _summary;

        public Uri ImageUri => _imageUri;

        public string AudioFileType => _audioFileType;

        public string AudioFileSize => _audioFileSize;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
