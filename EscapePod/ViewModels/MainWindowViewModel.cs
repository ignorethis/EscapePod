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
using System.Windows.Data;
using EscapePod.Models;
using NAudio.Wave;

namespace EscapePod.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private PodcastService _podcastService = new PodcastService();

        protected ObservableCollection<Podcast> _podcasts;
        private Podcast? _selectedPodcast;
        private Episode? _selectedEpisode;
        private Episode? _playingEpisode;

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
            BindingOperations.EnableCollectionSynchronization(_podcasts, myLock);
            (Podcast? lastPlayedPodcast, Episode? lastPlayedEpisode) = GetLastPlayedPodcastAndEpisode(_podcasts);
            SelectedPodcast = lastPlayedPodcast;
            SelectedEpisode = lastPlayedEpisode;
            _playingEpisode = null;

            _searchString = string.Empty;
            _searchPodcasts = new ObservableCollection<iTunesPodcastFinder.Models.Podcast>();
            BindingOperations.EnableCollectionSynchronization(_searchPodcasts, myLock);
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

                return 1;
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

                return "Play";
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

        public Podcast? SelectedPodcast
        {
            get => _selectedPodcast;
            set
            {
                _selectedPodcast = value;
                OnPropertyChanged();
            }
        }

        public Episode? PlayingEpisode
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

        public Episode? SelectedEpisode
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
            SelectedPodcast = newPodcast;

            //TODO hier noch bug fixen, dass bild erst gesetzt wird bevor es runtergeladen ist ... und dann crap binding exception kommt :C
            await _podcastService.DownloadTitleCardAsync(newPodcast);

            var firstSuccess =  await _podcastService.DownloadEpisodeAsync(newPodcast.EpisodeList.First()).ConfigureAwait(false);
            var lastSuccess = await _podcastService.DownloadEpisodeAsync(newPodcast.EpisodeList.Last()).ConfigureAwait(false);
            if (!firstSuccess || !lastSuccess)
            {
                // TODO inform user
            }

            SearchString = string.Empty;

            await _podcastService.SaveToDiskAsync(_podcasts);
        }

        public async Task DeletePodcastAsync(Podcast? podcast)
        {
            if (podcast == null)
            {
                return;
            }

            Podcasts.Remove(podcast);
            await _podcastService.SaveToDiskAsync(_podcasts);
        }

        public async Task PlayOrPauseAsync()
        {
            if (IsPlaying)
            {
                await PauseEpisodeAsync();
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

            var success = await _podcastService.DownloadEpisodeAsync(SelectedEpisode);
            if (!success)
            {
                return; // TODO: inform user
            }

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
            if (_playingEpisode == null || _selectedPodcast == null || _selectedEpisode == null)
            {
                return;
            }

            _playingEpisode.Timestamp = _audioFileReader.CurrentTime.TotalSeconds;
            _playingEpisode.LastPlayed = e.SignalTime;

            if (_audioFileReader.CurrentTime.TotalSeconds > (EpisodeLength * 0.8))
            {
                var nextIndex = _selectedPodcast.EpisodeList.IndexOf(_selectedEpisode) - 1;

                if (nextIndex >= 0)
                {
                    var nextEpisode = _selectedPodcast.EpisodeList.ElementAt(nextIndex);

                    if (!nextEpisode.IsDownloading)
                    {
                        var success = await _podcastService.DownloadEpisodeAsync(nextEpisode);
                        if (success)
                        {
                            nextEpisode.IsDownloading = false;
                            nextEpisode.IsDownloaded = true;
                        }
                        else
                        {
                            // TODO inform user
                        }
                    }
                }
            }
            if (_audioFileReader.CurrentTime.TotalSeconds > (EpisodeLength * 0.95))
            {
                _selectedEpisode.EpisodeFinished = true;
            }
        }

        private async Task PauseEpisodeAsync()
        {
            if (_playingEpisode == null)
            {
                return;
            }

            _episodeIsPlayingTimer.Stop();
            _waveOutDevice.Pause();
            _playingEpisode.Timestamp = _audioFileReader.CurrentTime.TotalSeconds;
            _playingEpisode.LastPlayed = DateTime.Now;

            OnPropertyChanged(nameof(PlayOrPauseButtonContent));

            await _podcastService.SaveToDiskAsync(_podcasts);
        }

        public void NextEpisode()
        {
            if (_selectedPodcast == null)
            {
                return;
            }

            if (_selectedEpisode == null)
            {
                SelectedEpisode = _selectedPodcast.EpisodeList.FirstOrDefault();
                return;
            }

            var nextIndex = _selectedPodcast.EpisodeList.IndexOf(_selectedEpisode) - 1;
            if (nextIndex >= 0)
            {
                SelectedEpisode = _selectedPodcast.EpisodeList.ElementAt(nextIndex);
            }
        }

        public void PreviousEpisode()
        {
            if (_selectedPodcast == null)
            {
                return;
            }

            if (_selectedEpisode == null)
            {
                SelectedEpisode = _selectedPodcast.EpisodeList.LastOrDefault();
                return;
            }

            var previousIndex = _selectedPodcast.EpisodeList.IndexOf(_selectedEpisode) + 1;
            if (previousIndex < _selectedPodcast.EpisodeList.Count)
            {
                SelectedEpisode = _selectedPodcast.EpisodeList.ElementAt(previousIndex);
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

            if (_selectedPodcast != null)
            {
                SelectedEpisode = oldSelectedEpisodeUri == null
                    ? null
                    : _selectedPodcast.EpisodeList.FirstOrDefault(e => e.EpisodeUri == oldSelectedEpisodeUri);
            }

            await _podcastService.SaveToDiskAsync(_podcasts);
        }

        public void SelectFirstEpisode()
        {
            if (_selectedPodcast == null)
            {
                return;
            }

            SelectedEpisode = _selectedPodcast.EpisodeList.LastOrDefault();
        }

        public void SelectLastEpisode()
        {
            if (_selectedPodcast == null)
            {
                return;
            }

            SelectedEpisode = _selectedPodcast.EpisodeList.FirstOrDefault();
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

        public (Podcast? lastPlayedPodcast, Episode? lastPlayedEpisode) GetLastPlayedPodcastAndEpisode(IEnumerable<Podcast> podcasts)
        {
            Podcast? lastPlayedPodcast = _podcasts.FirstOrDefault();
            Episode? lastPlayedEpisode = lastPlayedPodcast?.EpisodeList.FirstOrDefault();

            foreach (Podcast p in _podcasts)
            {
                foreach (Episode e in p.EpisodeList)
                {
                    if (e.LastPlayed == null)
                    {
                        continue;
                    }

                    if (e.LastPlayed.GetValueOrDefault() > lastPlayedEpisode?.LastPlayed.GetValueOrDefault())
                    {
                        lastPlayedPodcast = p;
                        lastPlayedEpisode = e;
                    }
                }
            }

            return (lastPlayedPodcast, lastPlayedEpisode);
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

        public async Task CloseApplicationAsync()
        {
            await _podcastService.SaveToDiskAsync(_podcasts);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
