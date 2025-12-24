using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Collections;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using EscapePod.Models;
using NAudio.Wave;

namespace EscapePod.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly string _downloadingEpisodeMessage = "Downloading Episode ...";
    private readonly string _couldNotDownloadEpisodeError = "Could not download the episode.";
    private readonly string _htmlTemplate = "<!DOCTYPE html><html><body>{0}</body></html>";

    private readonly IPodcastService _podcastService;
    private readonly WaveOutEvent _audioPlayer = new();
    private readonly Timer _episodeIsPlayingTimer = new(TimeSpan.FromSeconds(1));

    private AudioFileReader? _audioFileReader;

    private Podcast? _selectedSearchPodcast;
    private Podcast? _selectedPodcast;
    private Bitmap? _selectedPodcastImage;
    private Episode? _selectedEpisode;
    private Bitmap? _playingPodcastImage;
    private Episode? _playingEpisode;
    private string _searchValue = string.Empty;
    private string _status = string.Empty;

    public MainWindowViewModel(IPodcastService podcastService)
    {
        _podcastService = podcastService ?? throw new ArgumentNullException(nameof(podcastService));
     
        var podcasts = _podcastService.LoadFromDisk();
        Podcasts.AddRange(podcasts);
        _episodeIsPlayingTimer.Elapsed += EpisodeIsPlayingTimer_Elapsed;
    }

    public Podcast? SelectedSearchPodcast
    {
        get => _selectedSearchPodcast;
        set => SetProperty(ref _selectedSearchPodcast, value);
    }

    public Podcast? SelectedPodcast
    {
        get => _selectedPodcast;
        set
        {
            if (!SetProperty(ref _selectedPodcast, value))
            {
                return;
            }

            // Switch once updated and make async
            //SelectedPodcastImage = Bitmap.DecodeToHeight(File.OpenRead(podcast.ImageLocalPath), 200);
            if (value is null)
            {
                SelectedPodcastImage = null;
            }
            else
            {
                if (string.IsNullOrEmpty(value.ImageLocalPath))
                {
                    if (value.ImageUri is null)
                    {
                        SelectedPodcastImage = null;
                    }
                    else
                    {
                        Task.Run(async () =>
                        {
                            var result = await _podcastService.DownloadImage(value);
                            if (result.IsOk)
                            {
                                value.ImageLocalPath = result.Value;
                                await _podcastService.SaveToDisk(Podcasts);
                                SelectedPodcastImage = new Bitmap(value.ImageLocalPath);
                            }
                            else
                            {
                                Status = result.Error;
                                SelectedPodcastImage = null;
                            }

                            OnPropertyChanged(nameof(SelectedPodcastPanelVisible));
                        });
                    }
                }
                else
                {
                    SelectedPodcastImage = new Bitmap(value.ImageLocalPath);
                }
            }

            OnPropertyChanged(nameof(SelectedPodcastPanelVisible));
            OnPropertyChanged(nameof(DescriptionHtml));
        }
    }

    public Bitmap? SelectedPodcastImage
    {
        get => _selectedPodcastImage;
        set => SetProperty(ref _selectedPodcastImage, value);
    }

    public Episode? SelectedEpisode
    {
        get => _selectedEpisode;
        set
        {
            if (!SetProperty(ref _selectedEpisode, value))
            {
                return;
            }

            OnPropertyChanged(nameof(DescriptionHtml));
        }
    }

    public Bitmap? PlayingPodcastImage
    {
        get => _playingPodcastImage;
        set => SetProperty(ref _playingPodcastImage, value);
    }

    public Episode? PlayingEpisode
    {
        get => _playingEpisode;
        set
        {
            if (!SetProperty(ref _playingEpisode, value))
            {
                return;
            }

            if (!string.IsNullOrEmpty(_playingEpisode?.Podcast?.ImageLocalPath) && File.Exists(_playingEpisode.Podcast.ImageLocalPath))
            {
                PlayingPodcastImage = new Bitmap(_playingEpisode.Podcast.ImageLocalPath);
            }
        }
    }

    public string SearchValue
    {
        get => _searchValue;
        set
        {
            if (!SetProperty(ref _searchValue, value))
            {
                return;
            }

            PodcastSearch(value).ConfigureAwait(true);
            OnPropertyChanged(nameof(SearchListBoxIndex));
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            if (!SetProperty(ref _status, value))
            {
                return;
            }

            OnPropertyChanged(nameof(StatusPanelVisible));

            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                SetProperty(ref _status, string.Empty);
                OnPropertyChanged(nameof(StatusPanelVisible));
            });
        }
    }
    public bool StatusPanelVisible => !string.IsNullOrEmpty(_status);

    public AvaloniaList<Podcast> Podcasts { get; init; } = [];
    public AvaloniaList<Podcast> SearchPodcasts { get; } = [];

    public float Volume
    {
        get => _audioPlayer.Volume;
        set => _audioPlayer.Volume = value;
    }

    public double PlayingEpisodeListenProgress
    {
        get => _audioFileReader?.CurrentTime.TotalSeconds ?? 0;
        set
        {
            if (_audioFileReader is null)
            {
                return;
            }

            TimeSpan seconds = TimeSpan.FromSeconds(value);
            if (PlayingEpisode is not null)
            {
                PlayingEpisode.ListenStoppedAt = seconds;
            }

            _audioFileReader.CurrentTime = seconds;
        }
    }

    public TimeSpan PlayingEpisodeListenProgressMax
    {
        get
        {
            if (_audioFileReader is null)
            {
                return TimeSpan.Zero;
            }

            return _audioFileReader.TotalTime;
        }
    }

    public bool IsPlaying => _audioPlayer.PlaybackState == PlaybackState.Playing;
    public bool IsSearching => !string.IsNullOrEmpty(SearchValue);
    public int SearchListBoxIndex => IsSearching ? 1 : 0;
    public bool SelectedPodcastPanelVisible => _selectedPodcast is not null;

    public string DescriptionHtmlStylesheet { get; } = """
        * {
            color: white;
        }

        body {
            color: white;
            margin: 0;
            padding: 0;
            border: 0;
        }

        p {
            margin: 0 0 16px 0;
        }

        a {
            color: white;
        }
    """;

    public string DescriptionHtml
    {
        get
        {
            if (_selectedEpisode is not null)
            {
                return string.Format(_htmlTemplate, _selectedEpisode.Description);
            }

            if (_selectedPodcast is not null)
            {
                return string.Format(_htmlTemplate, $"<p>{_selectedPodcast.Description}</p>");
            }

            return string.Empty;
        }
    }

    private async void EpisodeIsPlayingTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (_playingEpisode is null || _audioFileReader is null)
        {
            return;
        }

        _playingEpisode.ListenStoppedAt = _audioFileReader.CurrentTime;
        _playingEpisode.ListenLastAt = e.SignalTime;
        OnPropertyChanged(nameof(PlayingEpisodeListenProgress));

        if (_audioFileReader.CurrentTime > _playingEpisode.Length * 0.8)
        {
            var nextIndex = _playingEpisode.Podcast.Episodes.IndexOf(_playingEpisode) - 1;
            if (nextIndex >= 0)
            {
                var nextEpisode = _playingEpisode.Podcast.Episodes.ElementAt(nextIndex);
                if (!File.Exists(nextEpisode.EpisodeLocalPath) || nextEpisode.DownloadState is not DownloadState.Downloaded)
                {
                    if (nextEpisode == _playingEpisode)
                    {
                        _status = "Next Episode is same as current Episode";
                        return;
                    }

                    else
                    {
                        Status = _downloadingEpisodeMessage;
                        var result = await _podcastService.DownloadEpisode(nextEpisode);
                        if (result.IsFailure)
                        {
                            Status = result.Error;
                        }
                    }
                }
            }
        }
    }

    public async Task PodcastSearch(string query)
    {
        var result = await _podcastService.SearchPodcast(query);
        
        if (result.IsOk)
        {
            SearchPodcasts.Clear();
            SearchPodcasts.AddRange(result.Value.OrderBy(x => x.Name));
        }
        else
        {
            Status = result.Error;
        }
    }

    [RelayCommand]
    public async Task PlayOrPauseEpisode(Episode episode)
    {
        SelectedEpisode = episode;

        if (episode == PlayingEpisode && IsPlaying)
        {
            await PauseEpisode();
        }
        else
        {
            await PlayEpisode(episode);
        }
    }

    [RelayCommand]
    public async Task PauseEpisode()
    {
        if (_playingEpisode is null || _audioFileReader is null)
        {
            return;
        }

        _episodeIsPlayingTimer.Stop();
        _audioPlayer.Pause();
        _playingEpisode.ListenStoppedAt = TimeSpan.FromSeconds(_audioFileReader.CurrentTime.TotalSeconds);
        _playingEpisode.ListenLastAt = DateTime.Now;

        OnPropertyChanged(nameof(IsPlaying));

        await _podcastService.SaveToDisk(Podcasts);
    }

    [RelayCommand]
    public async Task PlayEpisode(Episode episode)
    {
        if (episode is null)
        {
            return;
        }

        if (episode == _playingEpisode && _audioPlayer.PlaybackState == PlaybackState.Playing)
        {
            return;
        }

        if (!File.Exists(episode.EpisodeLocalPath) || episode.DownloadState is not DownloadState.Downloaded)
        {
            Status = _downloadingEpisodeMessage;
            var result = await _podcastService.DownloadEpisode(episode);
            if (result.IsFailure)
            {
                Status = result.Error;
                return;
            }
        }

        if (_audioPlayer.PlaybackState == PlaybackState.Playing
            || _audioPlayer.PlaybackState == PlaybackState.Paused)
        {
            _audioPlayer.Stop();
        }

        PlayingEpisode = episode;

        _audioFileReader = new AudioFileReader(episode.EpisodeLocalPath);
        _audioPlayer.Init(_audioFileReader);
        _audioFileReader.CurrentTime = episode.ListenStoppedAt ?? TimeSpan.Zero;
        _audioPlayer.Play();
        _episodeIsPlayingTimer.Start();

        episode.Length = _audioFileReader.TotalTime;

        OnPropertyChanged(nameof(IsPlaying));
        OnPropertyChanged(nameof(PlayingEpisodeListenProgress));
        OnPropertyChanged(nameof(PlayingEpisodeListenProgressMax));
    }

    [RelayCommand]
    public void NextEpisode()
    {
        if (_playingEpisode is null)
        {
            return;
        }

        if (SelectedEpisode is null)
        {
            SelectedEpisode = _playingEpisode.Podcast.Episodes.FirstOrDefault();
            return;
        }

        int nextIndex = _playingEpisode.Podcast.Episodes.IndexOf(SelectedEpisode) - 1;
        if (nextIndex >= 0)
        {
            SelectedEpisode = _playingEpisode.Podcast.Episodes.ElementAt(nextIndex);
        }
    }

    [RelayCommand]
    public void PreviousEpisode()
    {
        if (_playingEpisode is null)
        {
            return;
        }

        if (SelectedEpisode is null)
        {
            SelectedEpisode = _playingEpisode.Podcast.Episodes.LastOrDefault();
            return;
        }

        int previousIndex = _playingEpisode.Podcast.Episodes.IndexOf(SelectedEpisode) + 1;
        if (previousIndex < _playingEpisode.Podcast.Episodes.Count)
        {
            SelectedEpisode = _playingEpisode.Podcast.Episodes.ElementAt(previousIndex);
        }
    }

    [RelayCommand]
    public async Task UpdateAllPodcasts()
    {
        //alten zustand merken
        var oldSelectedPodcastUri = SelectedPodcast?.PodcastUri;
        var oldSelectedEpisodeUri = SelectedEpisode?.EpisodeUri;

        //neuen stuff laden
        List<Podcast> updatedPodcasts = [];
        foreach (Podcast podcast in Podcasts)
        {
            if (podcast.PodcastUri is null)
            {
                continue;
            }

            var result = await _podcastService.GetPodcast(podcast.PodcastUri);
            if (result.IsOk)
            {
                updatedPodcasts.Add(result.Value);
            }
            else
            {
                Status = result.Error;
            }
        }

        //das alte zerstoeren
        Podcasts.Clear();
        foreach (Podcast updatedPodcast in updatedPodcasts)
        {
            Podcasts.Add(updatedPodcast);
        }

        //alten zustand wiederherstellen
        SelectedPodcast = oldSelectedPodcastUri is null
            ? null
            : Podcasts.FirstOrDefault(p => p.PodcastUri == oldSelectedPodcastUri);

        if (_selectedPodcast is not null)
        {
            SelectedEpisode = oldSelectedEpisodeUri is null
                ? null
                : _selectedPodcast.Episodes.FirstOrDefault(e => e.EpisodeUri == oldSelectedEpisodeUri);
        }

        await _podcastService.SaveToDisk(Podcasts);
    }

    [RelayCommand]
    public void SelectFirstEpisode()
    {
        if (_playingEpisode is null)
        {
            return;
        }

        SelectedEpisode = _playingEpisode.Podcast.Episodes.LastOrDefault();
    }

    [RelayCommand]
    public void SelectLastEpisode()
    {
        if (_playingEpisode is null)
        {
            return;
        }

        SelectedEpisode = _playingEpisode.Podcast.Episodes.FirstOrDefault();
    }

    [RelayCommand]
    public async Task AddPodcast(Podcast podcast)
    {
        var newFeedUri = podcast.PodcastUri;

        if (newFeedUri is null)
        {
            return;
        }

        var podcastResult = await _podcastService.GetPodcast(newFeedUri).ConfigureAwait(false);

        if (podcastResult.IsOk)
        {
            var newPodcast = podcastResult.Value;
            Podcasts.Add(newPodcast);
            var imageResult = await _podcastService.DownloadImage(newPodcast);

            if (imageResult.IsOk)
            {
                newPodcast.ImageLocalPath = imageResult.Value;
            }
            else
            {
                newPodcast.ImageLocalPath = null;
            }

            SearchValue = string.Empty;

            await _podcastService.SaveToDisk(Podcasts);
        }
        else
        {
            Status = podcastResult.Error;
        }        
    }

    [RelayCommand]
    public async Task DeletePodcast(Podcast? podcast)
    {
        if (podcast is null)
        {
            return;
        }

        if (podcast == _selectedPodcast) 
        {
            SelectedPodcastImage = null;
            
            SelectedPodcast = null;
            SelectedEpisode = null;
        }
        Podcasts.Remove(podcast);

        await _podcastService.SaveToDisk(Podcasts);
    }
}
