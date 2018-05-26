using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Pp
{
    public class DesignTimeViewModel : MainWindowViewModel
    {
        public DesignTimeViewModel()
        {
            var testPodcast = new Podcast();
            testPodcast.Name = "test";
            
            var episode1 = new Episode(testPodcast, "test1_episodename1", "http://www.lolol.com", "test1_epi1_desc", 1.2, DateTime.Now.AddDays(-5), "5:45", "subtitle", "author", false, "summary", "http://www.test.de", "54:45", "mp3", true, string.Empty);
            episode1.IsDownloading = true;
            var episode2 = new Episode(testPodcast, "test1_episodename1", "http://www.lolol.com", "test1_epi1_desc", 1.2, DateTime.Now.AddDays(-5), "45:54", "subtitle", "author", false, "summary", "http://www.test.de", "54:45", "mp3", false, string.Empty);
            var episode3 = new Episode(testPodcast, "test1_episodename1", "http://www.lolol.com", "test1_epi1_desc", 1.2, DateTime.Now.AddDays(-5), "34:41", "subtitle", "author", false, "summary", "http://www.test.de", "54:45", "mp3", false, string.Empty);
            episode3.IsDownloading = true;

            testPodcast.EpisodeList = new List<Episode>() { episode1, episode2, episode3 };

            podcasts = new ObservableCollection<Podcast>() { testPodcast };

            SelectedPodcast = testPodcast;
            SelectedEpisode = episode1;
        }
    }
}
