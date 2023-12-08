using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EscapePod.Models;

namespace EscapePod.ViewModels
{
    public class DesignTimeViewModel : MainWindowViewModel
    {
        public DesignTimeViewModel()
        {
            var testPodcast = new Podcast();
            testPodcast.Name = "test";
            
            var episode1 = new Episode(testPodcast, "test1_episodename1", new Uri("http://www.lolol.com"), "test1_epi1_desc", 1.2, DateTime.Now.AddDays(-5), TimeSpan.FromSeconds(405), "subtitle", "author", false, "summary", "http://www.test.de", "54:45", "mp3", true, string.Empty);
            episode1.IsDownloading = true;
            var episode2 = new Episode(testPodcast, "test1_episodename1", new Uri("http://www.lolol.com"), "test1_epi1_desc", 1.2, DateTime.Now.AddDays(-5), TimeSpan.FromSeconds(2754), "subtitle", "author", false, "summary", "http://www.test.de", "54:45", "mp3", false, string.Empty);
            var episode3 = new Episode(testPodcast, "test1_episodename1", new Uri("http://www.lolol.com"), "test1_epi1_desc", 1.2, DateTime.Now.AddDays(-5), TimeSpan.FromSeconds(2081), "subtitle", "author", false, "summary", "http://www.test.de", "54:45", "mp3", false, string.Empty);
            episode3.IsDownloading = true;

            testPodcast.EpisodeList = new List<Episode>() { episode1, episode2, episode3 };

            _podcasts = new ObservableCollection<Podcast>() { testPodcast };

            SelectedPodcast = testPodcast;
            SelectedEpisode = episode1;
        }
    }
}
