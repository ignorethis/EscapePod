using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EscapePod.Models
{
    public class Podcast : INotifyPropertyChanged
    {
        private Uri _uri;
        private string _name;
        private Uri _titleCard;
        private string _localTitleCardPath;
        private string _localPodcastPath;
        private string _author;
        private string _subtitle;
        private string _description;
        private Uri _website;
        private bool _isExplicit;
        private string _language;
        private string _copyright;
        private int _episodeCount;
        private DateTime? _lastUpdate;
        private string _id;
        private List<Episode> _episodeList = new List<Episode>();
        
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public Uri TitleCard
        {
            get => _titleCard;
            set
            {
                _titleCard = value;
                OnPropertyChanged();
            }
        }

        public List<Episode> EpisodeList
        {
            get => _episodeList;
            set
            {
                _episodeList = value;
                OnPropertyChanged();
            }
        }

        public Uri Uri
        {
            get => _uri;
            set
            {
                _uri = value;
                OnPropertyChanged();
            }
        }

        public string LocalTitleCardFileFullName
        {
            get => _localTitleCardPath;
            set
            {
                _localTitleCardPath = value;
                OnPropertyChanged();
            }
        }

        public string LocalPodcastPath
        {
            get => _localPodcastPath;
            set
            {
                _localPodcastPath = value;
                OnPropertyChanged();
            }
        }

        public string Author
        {
            get => _author;
            set
            {
                _author = value;
                OnPropertyChanged();
            }
        }

        public string Subtitle
        {
            get => _subtitle;
            set
            {
                _subtitle = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        public Uri Website
        {
            get => _website;
            set
            {
                _website = value;
                OnPropertyChanged();
            }
        }

        public bool IsExplicit
        {
            get => _isExplicit;
            set
            {
                _isExplicit = value;
                OnPropertyChanged();
            }
        }

        public string Language
        {
            get => _language;
            set
            {
                _language = value;
                OnPropertyChanged();
            }
        }

        public string Copyright
        {
            get => _copyright;
            set
            {
                _copyright = value;
                OnPropertyChanged();
            }
        }

        public int EpisodeCount
        {
            get => _episodeCount;
            set
            {
                _episodeCount = value;
                OnPropertyChanged();
            }
        }

        public DateTime? LastUpdate
        {
            get => _lastUpdate;
            set
            {
                _lastUpdate = value;
                OnPropertyChanged();
            }
        }

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
