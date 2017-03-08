using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pp
{
    public class Podcast : INotifyPropertyChanged
    {
        public Podcast()
        {
        }

        private Uri url;
        private string name;
        private Uri titleCard;
        private string localTitleCardPath;
        private List<Episode> episodeList;
        private string localPodcastPath;
        
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

        public Uri Url
        {
            get
            {
                return url;
            }

            set
            {
                url = value;
                this.OnPropertyChanged(nameof(Url));
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
