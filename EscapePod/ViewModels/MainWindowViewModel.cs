using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using EscapePod.Models;
using NAudio.Wave;

namespace EscapePod.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private PodcastService _podcastService = new PodcastService();

        protected ObservableCollection<Podcast> _podcasts;
        private Podcast _selectedPodcast;
        private Episode _selectedEpisode;
        private Episode _playingEpisode;

        private string _searchString;
        private ObservableCollection<iTunesPodcastFinder.Models.Podcast> _searchPodcasts;
        private iTunesPodcastFinder.Models.Podcast _selectedSearchPodcast;

        Timer _episodeIsPlayingTimer = new Timer(1000);
        IWavePlayer _waveOutDevice = new WaveOutEvent();
        AudioFileReader _audioFileReader;
        private float _volume;

        public MainWindowViewModel()
        {
            var myLock = new object();

            _podcasts = new ObservableCollection<Podcast>(_podcastService.LoadFromDisk());
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(_podcasts, myLock);
            _selectedPodcast = SelectedEpisode?.Podcast;
            _selectedEpisode = LastPlayed();

            _searchString = string.Empty;

            _searchPodcasts = new ObservableCollection<iTunesPodcastFinder.Models.Podcast>();
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(_searchPodcasts, myLock);
            _selectedSearchPodcast = null;

            _episodeIsPlayingTimer.Elapsed += EpisodeIsPlayingTimer_Elapsed;
            Volume = 1.0f;
        }

        public string SearchPlaceholder => "Search for podcasts";

        public bool IsPlaying => _waveOutDevice.PlaybackState == PlaybackState.Playing;

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
                if (_podcasts == null)
                {
                    _podcasts = new ObservableCollection<Podcast>();
                }

                return _podcasts;
            }
        }

        public Podcast SelectedPodcast
        {
            get => _selectedPodcast;
            set
            {
                _selectedPodcast = value;
                OnPropertyChanged();
            }
        }

        public Episode PlayingEpisode
        {
            get => _playingEpisode;
            set
            {
                _playingEpisode = value;
                OnPropertyChanged();
            }
        }

        public double EpisodeLength
        {
            get
            {
                if (_selectedEpisode == null || _selectedEpisode.EpisodeLength == 0)
                {
                    return 1;
                }
                return _selectedEpisode.EpisodeLength;
            }
            set
            {
                _selectedEpisode.EpisodeLength = value;
                OnPropertyChanged();
            }
        }

        public Episode SelectedEpisode
        {
            get => _selectedEpisode;
            set
            {
                _selectedEpisode = value;
                OnPropertyChanged();
            }
        }

        public string SearchString
        {
            get => _searchString;
            set
            {
                _searchString = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(SearchListBoxIndex));

                if (_searchString == SearchPlaceholder)
                {
                    return;
                }

                PodcastSearch(_searchString).ConfigureAwait(true);
            }
        }

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                OnPropertyChanged();

                if (_audioFileReader != null)
                {
                    _audioFileReader.Volume = _volume;
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
            var newPodcast = await _podcastService.GetPodcastAsync(newFeedUri).ConfigureAwait(false);

            Podcasts.Add(newPodcast);
            _podcastService.SaveToDisk(Podcasts);
            SelectedPodcast = newPodcast;

            //TODO hier noch bug fixen, dass bild erst gesetzt wird bevor es runtergeladen ist ... und dann crap binding exception kommt :C
            await _podcastService.DownloadTitleCardAsync(newPodcast);

            await _podcastService.DownloadEpisodeAsync(newPodcast.EpisodeList.First()).ConfigureAwait(false);
            await _podcastService.DownloadEpisodeAsync(newPodcast.EpisodeList.Last()).ConfigureAwait(false);

            SearchString = string.Empty;
        }

        public void DeletePodcast()
        {
            Podcasts.Remove(SelectedPodcast);
            _podcastService.SaveToDisk(Podcasts);
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
                && _waveOutDevice.PlaybackState == PlaybackState.Playing)
            {
                return;
            }

            await _podcastService.DownloadEpisodeAsync(SelectedEpisode);

            if (_waveOutDevice.PlaybackState == PlaybackState.Playing
                || _waveOutDevice.PlaybackState == PlaybackState.Paused)
            {
                _waveOutDevice.Stop();
            }

            _audioFileReader = new AudioFileReader(_selectedEpisode.LocalPath);
            _waveOutDevice.Init(_audioFileReader);
            _audioFileReader.CurrentTime = TimeSpan.FromSeconds(SelectedEpisode.Timestamp);
            EpisodeLength = _audioFileReader.TotalTime.TotalSeconds;
            _audioFileReader.Volume = Volume;
            _waveOutDevice.Play();
            _episodeIsPlayingTimer.Start();

            PlayingEpisode = SelectedEpisode;

            OnPropertyChanged(nameof(PlayOrPauseButtonContent));
        }

        private async void EpisodeIsPlayingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (PlayingEpisode == null)
            {
                return;
            }

            PlayingEpisode.Timestamp = _audioFileReader.CurrentTime.TotalSeconds;

            if (_audioFileReader.CurrentTime.TotalSeconds > (EpisodeLength * 0.8))
            {
                var nextIndex = SelectedPodcast.EpisodeList.IndexOf(PlayingEpisode) - 1;

                if (nextIndex >= 0)
                {
                    var nextEpisode = SelectedPodcast.EpisodeList.ElementAt(nextIndex);

                    if (!nextEpisode.IsDownloading)
                    {
                        await _podcastService.DownloadEpisodeAsync(nextEpisode);
                        nextEpisode.IsDownloading = false;
                        nextEpisode.IsDownloaded = true;
                    }
                }
            }
            if (_audioFileReader.CurrentTime.TotalSeconds > (EpisodeLength * 0.95))
            {
                SelectedEpisode.EpisodeFinished = true;
            }
        }

        private void PauseEpisode()
        {
            _episodeIsPlayingTimer.Stop();
            _waveOutDevice.Pause();
            PlayingEpisode.Timestamp = _audioFileReader.CurrentTime.TotalSeconds;
            PlayingEpisode.LastPlayed = DateTime.Now;

            _podcastService.SaveToDisk(Podcasts);

            OnPropertyChanged(nameof(PlayOrPauseButtonContent));
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
            var oldSelectedPodcastUri = SelectedPodcast?.Uri;
            var oldSelectedEpisodeUri = SelectedEpisode?.EpisodeUri;

            //neuen stuff laden
            var updatedPodcasts = new List<Podcast>();
            for (int i = 0; i < Podcasts.Count; i++)
            {
                var updatedPodcast = await _podcastService.GetPodcastAsync(Podcasts[i].Uri);
                updatedPodcasts.Add(updatedPodcast);
            }

            //das alte zerstoeren
            Podcasts.Clear();
            foreach (var updatedPodcast in updatedPodcasts)
            {
                Podcasts.Add(updatedPodcast);
            }

            //alten zustand wiederherstellen
            SelectedPodcast = oldSelectedPodcastUri == null
                ? null
                : Podcasts.FirstOrDefault(p => p.Uri == oldSelectedPodcastUri);

            SelectedEpisode = oldSelectedEpisodeUri == null
                ? null
                : SelectedPodcast.EpisodeList.FirstOrDefault(e => e.EpisodeUri == oldSelectedEpisodeUri);

            _podcastService.SaveToDisk(Podcasts);
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

            if (_audioFileReader == null)
            {
                return;
            }

            SelectedEpisode.Timestamp = timestamp;
            _audioFileReader.CurrentTime = TimeSpan.FromSeconds(timestamp);
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
            foreach (Podcast p in _podcasts)
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
            var results = await _podcastService.SearchPodcastAsync(query);
            var orderedResults = results.OrderBy(x => x.Name);

            SearchPodcasts.Clear();

            foreach (var item in orderedResults)
            {
                SearchPodcasts.Add(item);
            }
        }
       
        public ObservableCollection<iTunesPodcastFinder.Models.Podcast> SearchPodcasts
        {
            get => _searchPodcasts;
            set
            {
                _searchPodcasts = value;
                OnPropertyChanged();
            }
        }

        public iTunesPodcastFinder.Models.Podcast SelectedSearchPodcast
        {
            get => _selectedSearchPodcast;
            set
            {
                _selectedSearchPodcast = value;
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
