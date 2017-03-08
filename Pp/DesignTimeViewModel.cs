using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pp
{
    public class DesignTimeViewModel : MainWindowViewModel
    {
        public DesignTimeViewModel()
        {
            var testPodcast = new Podcast();
            testPodcast.Name = "test";
            
            var episode1 = new Episode(testPodcast, "test1_episodename1", 1, new Uri("http://www.lolol.com"), "test1_epi1_desc", 1.2, DateTime.Now.AddDays(-5));
            episode1.IsDownloading = true;
            var episode2 = new Episode(testPodcast, "test1_episodename1", 1, new Uri("http://www.lolol.com"), "test1_epi1_desc", 1.2, DateTime.Now.AddDays(-5));
            var episode3 = new Episode(testPodcast, "test1_episodename1", 1, new Uri("http://www.lolol.com"), "test1_epi1_desc", 1.2, DateTime.Now.AddDays(-5));
            episode3.IsDownloading = true;

            testPodcast.EpisodeList = new List<Episode>() { episode1, episode2, episode3 };

            podcasts = new ObservableCollection<Podcast>() { testPodcast };

            SelectedPodcast = testPodcast;
            SelectedEpisode = episode1;
        }
    }
}
