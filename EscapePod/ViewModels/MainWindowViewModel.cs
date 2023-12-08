using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Pp.Models;

namespace Pp.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private PodcastService podcastService = new PodcastService();

        protected ObservableCollection<Podcast> podcasts;
        private Podcast selectedPodcast;
        private Episode selectedEpisode;
        private Episode playingEpisode;

        private string searchString;
        private ObservableCollection<iTunesPodcastFinder.Models.Podcast> searchPodcasts;
        private iTunesPodcastFinder.Models.Podcast selectedSearchPodcast;

        Timer episodeIsPlayingTimer = new Timer(1000);
        IWavePlayer waveOutDevice = new WaveOutEvent();
        AudioFileReader audioFileReader;
        private float volume;

        public MainWindowViewModel()
        {
            var myLock = new object();

            podcasts = new ObservableCollection<Podcast>(podcastService.LoadFromDisk());
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(podcasts, myLock);
            selectedPodcast = SelectedEpisode?.Podcast;
            selectedEpisode = LastPlayed();

            searchString = string.Empty;

            searchPodcasts = new ObservableCollection<iTunesPodcastFinder.Models.Podcast>();
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(searchPodcasts, myLock);
            selectedSearchPodcast = null;

            episodeIsPlayingTimer.Elapsed += EpisodeIsPlayingTimer_Elapsed;
            Volume = 1.0f;
        }

        public string SearchPlaceholder
        {
            get
            {
                return "Search for podcasts";
            }
        }

        public bool IsPlaying
        {
            get
            {
                return this.waveOutDevice.PlaybackState == PlaybackState.Playing;
            }
        }

        public int SearchListBoxIndex
        {
            get
            {
                if (string.IsNullOrEmpty(SearchString) || SearchString == SearchPlaceholder)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
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

        public Episode PlayingEpisode
        {
            get
            {
                return playingEpisode;
            }

            set
            {
                playingEpisode = value;
                this.OnPropertyChanged(nameof(PlayingEpisode));
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
            }
        }

        public string SearchString
        {
            get
            {
                return searchString;
            }

            set
            {
                searchString = value;

                this.OnPropertyChanged(nameof(SearchString));
                this.OnPropertyChanged(nameof(SearchListBoxIndex));

                if (searchString == SearchPlaceholder)
                {
                    return;
                }

                PodcastSearch(searchString).ConfigureAwait(true);
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
                if (oldPodcast.Id == SelectedSearchPodcast.ItunesId)
                {
                    SelectedPodcast = oldPodcast;
                    return;
                }
            }

            var newFeedUri = new Uri(SelectedSearchPodcast.FeedUrl);
            var newPodcast = await podcastService.GetPodcastAsync(newFeedUri).ConfigureAwait(false);

            this.Podcasts.Add(newPodcast);
            podcastService.SaveToDisk(Podcasts);
            SelectedPodcast = newPodcast;

            //TODO hier noch bug fixen, dass bild erst gesetzt wird bevor es runtergeladen ist ... und dann crap binding exception kommt :C
            await podcastService.DownloadTitleCardAsync(newPodcast);

            await podcastService.DownloadEpisodeAsync(newPodcast.EpisodeList.First()).ConfigureAwait(false);
            await podcastService.DownloadEpisodeAsync(newPodcast.EpisodeList.Last()).ConfigureAwait(false);

            this.SearchString = string.Empty;
        }

        public void DeletePodcast()
        {
            Podcasts.Remove(SelectedPodcast);
            podcastService.SaveToDisk(Podcasts);
        }

        public async Task PlayOrPauseAsync()
        {
            if (IsPlaying)
            {
                PauseEpisode();
            }
            else
            {
                await PlayEpisodeAsync();
            }
        }

        public async Task PlayEpisodeAsync()
        {
            if (PlayingEpisode == SelectedEpisode 
                && waveOutDevice.PlaybackState == PlaybackState.Playing)
            {
                return;
            }

            await podcastService.DownloadEpisodeAsync(SelectedEpisode);

            if (waveOutDevice.PlaybackState == PlaybackState.Playing 
                || waveOutDevice.PlaybackState == PlaybackState.Paused)
            {
                waveOutDevice.Stop();
            }

            audioFileReader = new AudioFileReader(selectedEpisode.LocalPath);
            waveOutDevice.Init(audioFileReader);
            audioFileReader.CurrentTime = TimeSpan.FromSeconds(SelectedEpisode.Timestamp);
            EpisodeLength = audioFileReader.TotalTime.TotalSeconds;
            audioFileReader.Volume = Volume;
            waveOutDevice.Play();
            episodeIsPlayingTimer.Start();

            PlayingEpisode = SelectedEpisode;

            this.OnPropertyChanged(nameof(PlayOrPauseButtonContent));
        }

        private async void EpisodeIsPlayingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (PlayingEpisode == null)
            {
                return;
            }

            PlayingEpisode.Timestamp = audioFileReader.CurrentTime.TotalSeconds;

            if (audioFileReader.CurrentTime.TotalSeconds > (EpisodeLength * 0.8))
            {
                var nextIndex = SelectedPodcast.EpisodeList.IndexOf(PlayingEpisode) - 1;

                if (nextIndex >= 0)
                {
                    var nextEpisode = SelectedPodcast.EpisodeList.ElementAt(nextIndex);

                    if (!nextEpisode.IsDownloading)
                    {
                        await podcastService.DownloadEpisodeAsync(nextEpisode);
                        nextEpisode.IsDownloading = false;
                        nextEpisode.IsDownloaded = true;
                    }
                }
            }
            if (audioFileReader.CurrentTime.TotalSeconds > (EpisodeLength * 0.95))
            {
                SelectedEpisode.EpisodeFinished = true;
            }
        }

        private void PauseEpisode()
        {
            episodeIsPlayingTimer.Stop();
            waveOutDevice.Pause();
            PlayingEpisode.Timestamp = audioFileReader.CurrentTime.TotalSeconds;
            PlayingEpisode.LastPlayed = DateTime.Now;

            podcastService.SaveToDisk(Podcasts);

            this.OnPropertyChanged(nameof(PlayOrPauseButtonContent));
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

        public async Task UpdateAllPodcastAsync()
        {
            //alten zustand merken
            var oldSelectedPodcastUri = this.SelectedPodcast?.Uri;
            var oldSelectedEpisodeUri = this.SelectedEpisode?.EpisodeUri;

            //neuen stuff laden
            var updatedPodcasts = new List<Podcast>();
            for (int i = 0; i < Podcasts.Count; i++)
            {
                var updatedPodcast = await podcastService.GetPodcastAsync(Podcasts[i].Uri);
                updatedPodcasts.Add(updatedPodcast);
            }

            //das alte zerstoeren
            this.Podcasts.Clear();
            foreach (var updatedPodcast in updatedPodcasts)
            {
                this.Podcasts.Add(updatedPodcast);
            }

            //alten zustand wiederherstellen
            this.SelectedPodcast = oldSelectedPodcastUri == null
                ? null
                : this.Podcasts.FirstOrDefault(p => p.Uri == oldSelectedPodcastUri);

            this.SelectedEpisode = oldSelectedEpisodeUri == null
                ? null
                : this.SelectedPodcast.EpisodeList.FirstOrDefault(e => e.EpisodeUri == oldSelectedEpisodeUri);

            podcastService.SaveToDisk(Podcasts);
        }

        public void SelectFirstEpisode()
        {
            if (SelectedPodcast == null)
            {
                return;
            }

            SelectedEpisode = SelectedPodcast.EpisodeList.Last();
        }

        public void SelectLastEpisode()
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

        public async Task StreamEpisodeAsync(Episode current)
        {
            var memoryStream = new MemoryStream();

            var mp3Stream = await new HttpClient().GetStreamAsync(current.EpisodeUri);
            var mp3ChunkBuffer = new byte[4]; //hier kommen unsere 4 bytes immer rein :D

            using (var fileStream = new FileStream(current.LocalPath, FileMode.Create, FileAccess.Write)) //und der crap muss ja auch gespeichert werden
            {
                while (mp3Stream.Read(mp3ChunkBuffer, 0, 4) > 0) //4er bytes lesen bis nix mehr kommt (falls du skippen musst, vergiss nicht den geskipten part auch in die file zu schreiben)
                {
                    memoryStream.Write(mp3ChunkBuffer, 0, mp3ChunkBuffer.Length);
                    fileStream.Write(mp3ChunkBuffer, 0, mp3ChunkBuffer.Length); //schreib den chunck in die datei
                }
            }

            mp3Stream.Close();

            var readFullyStream = new ReadFullyStream(memoryStream);
            var frame = Mp3Frame.LoadFromStream(readFullyStream);
            IMp3FrameDecompressor decompressor = CreateFrameDecompressor(frame);
            BufferedWaveProvider bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat);
            bufferedWaveProvider.BufferDuration = TimeSpan.FromSeconds(20);

            var buffer = new byte[16384 * 4];
            int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
            bufferedWaveProvider.AddSamples(buffer, 0, decompressed);
        }

        /* private bool IsBufferNearlyFull
        {
            
            if (bufferedWaveProvider != null && (bufferedWaveProvider.BufferLength - bufferedWaveProvider.BufferedBytes<bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4))
	        {
                return true;
	        } 
            
        } */

        private static IMp3FrameDecompressor CreateFrameDecompressor(Mp3Frame frame)
        {
            WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                frame.FrameLength, frame.BitRate);
            return new AcmMp3FrameDecompressor(waveFormat);
        }

        public Episode LastPlayed()
        {
            Episode hi = null;
            foreach (Podcast p in podcasts)
            {
                foreach (Episode e in p.EpisodeList)
                {
                    if (e.LastPlayed != null)
                    {
                        if (hi == null)
                        {
                            hi = e;
                        }
                        else if (e.LastPlayed > hi.LastPlayed)
                        {
                            hi = e;
                        }
                    }
                }
            }
            return hi;
        }

        public async Task PodcastSearch(string query)
        {
            var results = await this.podcastService.SearchPodcastAsync(query);
            var orderedResults = results.OrderBy(x => x.Name);

            SearchPodcasts.Clear();

            foreach (var item in orderedResults)
            {
                SearchPodcasts.Add(item);
            }
        }
       
        public ObservableCollection<iTunesPodcastFinder.Models.Podcast> SearchPodcasts
        {
            get
            {
                return searchPodcasts;
            }

            set
            {
                searchPodcasts = value;
                this.OnPropertyChanged(nameof(SearchPodcasts));
            }
        }

        public iTunesPodcastFinder.Models.Podcast SelectedSearchPodcast
        {
            get
            {
                return selectedSearchPodcast;
            }

            set
            {
                selectedSearchPodcast = value;
                this.OnPropertyChanged(nameof(SelectedSearchPodcast));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
