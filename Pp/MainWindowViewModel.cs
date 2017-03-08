using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using NAudio;
using NAudio.Wave;
using System.Windows.Threading;
using System.Windows;
using System.Net;
using System.Threading;
using System.Net.Http;

namespace Pp
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private PodcastService podcastService = new PodcastService();
        private Podcast selectedPodcast;
        private Episode selectedEpisode;
        protected ObservableCollection<Podcast> podcasts;
        private string url;
        System.Timers.Timer episodeIsPlayingTimer = new System.Timers.Timer(1000);
        IWavePlayer waveOutDevice = new WaveOut();
        AudioFileReader audioFileReader;
        private float volume;

        public MainWindowViewModel()
        {
            podcasts = new ObservableCollection<Podcast>(podcastService.LoadFromDisk());
            episodeIsPlayingTimer.Elapsed += EpisodeIsPlayingTimer_Elapsed;
        }


        public bool IsPlaying
        {
            get
            {
                return this.waveOutDevice.PlaybackState == PlaybackState.Playing;
            }
        }

        public string PlayOrPauseButtonContent
        {
            get
            {
                if (IsPlaying)
                {
                    return "Pause";
                }
                else
                {
                    return "Play";
                }
            }
        }

        public ObservableCollection<Podcast> Podcasts
        {
            get
            {
                if (podcasts == null)
                {
                    podcasts = new ObservableCollection<Podcast>();
                }

                return podcasts;
            }
        }

        public Podcast SelectedPodcast
        {
            get
            {
                return selectedPodcast;
            }

            set
            {
                selectedPodcast = value;
                this.OnPropertyChanged(nameof(SelectedPodcast));
            }
        }

        public double EpisodeLength
        {
            get
            {
                if (selectedEpisode == null || selectedEpisode.EpisodeLength == 0)
                {
                    return 1;
                }
                return selectedEpisode.EpisodeLength;
            }
            set
            {
                selectedEpisode.EpisodeLength = value;
                this.OnPropertyChanged(nameof(EpisodeLength));
            }
        }

        public Episode SelectedEpisode
        {
            get
            {
                return selectedEpisode;
            }

            set
            {
                selectedEpisode = value;
                this.OnPropertyChanged(nameof(SelectedEpisode));

                if (value != null && File.Exists(selectedEpisode.LocalPath))
                {
                    audioFileReader = new AudioFileReader(selectedEpisode.LocalPath);
                }
            }
        }

        public string Url
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

        public float Volume
        {
            get
            {
                return volume;
            }

            set
            {
                volume = value;
                this.OnPropertyChanged(nameof(Volume));

                if (audioFileReader != null)
                {
                    audioFileReader.Volume = this.volume;
                }
            }
        }

        public async Task AddPodcastAsync()
        {
            foreach (var oldPodcast in Podcasts)
            {
                if (oldPodcast.Url.AbsoluteUri == Url)
                {
                    SelectedPodcast = oldPodcast;
                    return;
                }
            }

            var newPodcast = await podcastService.GetPodcastAsync(new Uri(Url)).ConfigureAwait(false);

            if (!Directory.Exists(newPodcast.LocalPodcastPath))
            {
                Directory.CreateDirectory(newPodcast.LocalPodcastPath);
            }

            await Application.Current.Dispatcher.InvokeAsync(() => this.Podcasts.Add(newPodcast));
            podcastService.SaveToDisk(Podcasts.ToList());
            SelectedPodcast = newPodcast;

            //TODO hier noch bug fixen, dass bild erst gesetzt wird bevor es runtergeladen ist ... und dann crap binding exception kommt :C
            await podcastService.DownloadTitleCardAsync(newPodcast);

            await Task.Run(() =>
            {
                podcastService.DownloadEpisodeAsync(newPodcast.EpisodeList.First()).ConfigureAwait(false);
                podcastService.DownloadEpisodeAsync(newPodcast.EpisodeList.Last()).ConfigureAwait(false);
            });
        }

        public void DeletePodcast()
        {
            Podcasts.Remove(SelectedPodcast);
            podcastService.SaveToDisk(Podcasts.ToList());
        }

        public void PlayOrPauseAsync()
        {
            if (IsPlaying)
            {
                PauseEpisode();
            }
            else
            {
                PlayEpisode();
            }
        }

        public async void PlayEpisode()
        {
            if (IsPlaying)
            {
                return;
            }

            if (SelectedEpisode.IsDownloaded == false && SelectedEpisode.LocalPath == null)
            {
                await podcastService.DownloadEpisodeAsync(SelectedEpisode);
            }
            
            waveOutDevice.Init(audioFileReader);
            audioFileReader.CurrentTime = TimeSpan.FromSeconds(SelectedEpisode.Timestamp);
            EpisodeLength = audioFileReader.TotalTime.TotalSeconds;
            audioFileReader.Volume = Volume;
            waveOutDevice.Play();
            episodeIsPlayingTimer.Start();
            
            
            this.OnPropertyChanged(nameof(PlayOrPauseButtonContent));
        }

        private void EpisodeIsPlayingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SelectedEpisode.Timestamp = audioFileReader.CurrentTime.TotalSeconds;

            if (audioFileReader.CurrentTime.TotalSeconds > audioFileReader.CurrentTime.TotalSeconds * 0.8)
            {
                var nextIndex = SelectedPodcast.EpisodeList.IndexOf(SelectedEpisode) - 1;

                if (nextIndex >= 0)
                {
                    var nextEpisode = SelectedPodcast.EpisodeList.ElementAt(nextIndex);

                    if (!nextEpisode.IsDownloading)
                    {
                        Task.Run(async () =>
                        {
                            await podcastService.DownloadEpisodeAsync(nextEpisode);
                            nextEpisode.IsDownloading = false;
                            nextEpisode.IsDownloaded = true;
                        }).ConfigureAwait(false);
                    }
                }
            }
        }

        public void PauseEpisode()
        {
            episodeIsPlayingTimer.Stop();
            waveOutDevice.Pause();
            SelectedEpisode.Timestamp = audioFileReader.CurrentTime.TotalSeconds;
            SelectedEpisode.LastPlayed = DateTime.Now;
            podcastService.SaveToDisk(Podcasts.ToList());
        }

        public void NextEpisode()
        {
            var nextIndex = SelectedPodcast.EpisodeList.IndexOf(SelectedEpisode) - 1;
            if (nextIndex >= 0)
            {
                SelectedEpisode = SelectedPodcast.EpisodeList.ElementAt(nextIndex);
            }
        }

        public void PreviousEpisode()
        {
            var previousIndex = SelectedPodcast.EpisodeList.IndexOf(SelectedEpisode) + 1;
            if (previousIndex < SelectedPodcast.EpisodeList.Count)
            {
                SelectedEpisode = SelectedPodcast.EpisodeList.ElementAt(previousIndex);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public async Task UpdateAllPodcastAsync()
        {
            var oldSelectedPodcastUrl = this.SelectedPodcast.Url;
            Uri oldSelectedEpisodeUri = null;
            if (selectedEpisode != null)
            {
                oldSelectedEpisodeUri = this.SelectedEpisode.EpisodeUri;
            }

            var updatedPodcasts = new List<Podcast>();
            for (int i = 0; i < Podcasts.Count; i++)
            {
                var updatedPodcast = await podcastService.GetPodcastAsync(Podcasts[i].Url);
                updatedPodcasts.Add(updatedPodcast);
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>this.Podcasts.Clear());
            foreach (var updatedPodcast in updatedPodcasts)
            {
                await Application.Current.Dispatcher.InvokeAsync(() => this.Podcasts.Add(updatedPodcast));
            }

            this.SelectedPodcast = this.Podcasts.FirstOrDefault(p => p.Url == oldSelectedPodcastUrl);

            if (oldSelectedEpisodeUri != null)
            {
                this.SelectedEpisode = this.SelectedPodcast.EpisodeList.FirstOrDefault(e => e.EpisodeUri == oldSelectedEpisodeUri);
            }

            podcastService.SaveToDisk(Podcasts.ToList());
        }

        public void FirstEpisode()
        {
            if (SelectedPodcast == null)
            {
                return;
            }

            SelectedEpisode = SelectedPodcast.EpisodeList.Last();
        }

        public void LastEpisode()
        {
            if (SelectedPodcast == null)
            {
                return;
            }

            SelectedEpisode = SelectedPodcast.EpisodeList.First();
        }

        public void Seek(double timestamp)
        {
            if (SelectedEpisode == null)
            {
                return;
            }

            if (audioFileReader == null)
            {
                return;
            }

            SelectedEpisode.Timestamp = timestamp;
            audioFileReader.CurrentTime = TimeSpan.FromSeconds(timestamp);
        }
    }
}
