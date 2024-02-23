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

    private readonly PodcastService _podcastService = new();
    private readonly WaveOutEvent _audioPlayer = new();
    private readonly Timer _episodeIsPlayingTimer = new(TimeSpan.FromSeconds(1));

    private AudioFileReader? _audioFileReader;

    private iTunesPodcastFinder.Models.Podcast? _selectedSearchPodcast;
    private Podcast? _selectedPodcast;
    private Bitmap? _selectedPodcastImage;
    private Episode? _selectedEpisode;
    private Episode? _playingEpisode;
    private string _searchValue = string.Empty;
    private string _status = string.Empty;

    public MainWindowViewModel()
    {
        var podcasts = _podcastService.LoadFromDisk();
        Podcasts.AddRange(podcasts);
        _episodeIsPlayingTimer.Elapsed += EpisodeIsPlayingTimer_Elapsed;
    }

    public iTunesPodcastFinder.Models.Podcast? SelectedSearchPodcast
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

            if (!string.IsNullOrEmpty(value?.ImageLocalPath) && File.Exists(value.ImageLocalPath))
            {
                SelectedPodcastImage = new Bitmap(value.ImageLocalPath);
                // Switch once updated and make async
                //SelectedPodcastImage = Bitmap.DecodeToHeight(File.OpenRead(podcast.ImageLocalPath), 200);
            }
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

            OnPropertyChanged(nameof(EpisodeDescriptionHtmlStyled));
        }
    }

    public Episode? PlayingEpisode
    {
        get => _playingEpisode;
        set => SetProperty(ref _playingEpisode, value);
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

            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                SetProperty(ref _status, string.Empty);
            });
        }
    }

    public AvaloniaList<Podcast> Podcasts { get; init; } = [];
    public AvaloniaList<iTunesPodcastFinder.Models.Podcast> SearchPodcasts { get; } = [];

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
    public string PlayOrPauseButtonContent => IsPlaying ? "Pause" : "Play";
    public bool IsSearching => !string.IsNullOrEmpty(SearchValue);
    public int SearchListBoxIndex => IsSearching ? 1 : 0;

    private readonly string _episodeDescriptionHtmlStyledTemplate = """
        <html>
        <head>
            <style>
                body {{
                    background-color: black;
                    color: white;
                }}
        
                a {{
                    color: white;
                }}
            </style>
        </head>
        <body>
            {0}
        </body>
        </html>
        """;

    public string EpisodeDescriptionHtmlStyled
    {
        get
        {
            if (SelectedEpisode is null)
            {
                return string.Format(_episodeDescriptionHtmlStyledTemplate, string.Empty);
            }

            return string.Format(_episodeDescriptionHtmlStyledTemplate, SelectedEpisode.Description);
        }
    }

    private async void EpisodeIsPlayingTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (_playingEpisode is null || _selectedPodcast is null || _selectedEpisode is null || _audioFileReader is null)
        {
            return;
        }

        _playingEpisode.ListenStoppedAt = _audioFileReader.CurrentTime;
        _playingEpisode.ListenLastAt = e.SignalTime;
        OnPropertyChanged(nameof(PlayingEpisodeListenProgress));

        if (_audioFileReader.CurrentTime > _playingEpisode.Length * 0.8)
        {
            var nextIndex = _selectedPodcast.Episodes.IndexOf(SelectedEpisode) - 1;
            if (nextIndex >= 0)
            {
                var nextEpisode = _selectedPodcast.Episodes.ElementAt(nextIndex);
                if (nextEpisode.DownloadState != DownloadState.IsDownloading)
                {
                    Status = _downloadingEpisodeMessage;
                    var success = await _podcastService.DownloadEpisode(_selectedPodcast, nextEpisode);
                    if (success)
                    {
                        nextEpisode.DownloadState = DownloadState.Downloaded;
                    }
                    else
                    {
                        Status = _couldNotDownloadEpisodeError;
                    }
                }
            }
        }
    }

    public async Task PodcastSearch(string query)
    {
        var results = await _podcastService.SearchPodcast(query);
        SearchPodcasts.Clear();
        SearchPodcasts.AddRange(results.OrderBy(x => x.Name));
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
        if (_playingEpisode is null)
        {
            return;
        }

        _episodeIsPlayingTimer.Stop();
        _audioPlayer.Pause();
        _playingEpisode.ListenStoppedAt = TimeSpan.FromSeconds(_audioFileReader.CurrentTime.TotalSeconds);
        _playingEpisode.ListenLastAt = DateTime.Now;

        OnPropertyChanged(nameof(PlayOrPauseButtonContent));

        await _podcastService.SaveToDisk(Podcasts);
    }

    [RelayCommand]
    public async Task PlayEpisode(Episode episode)
    {
        if (episode == PlayingEpisode
            && _audioPlayer.PlaybackState == PlaybackState.Playing)
        {
            return;
        }

        if (!File.Exists(episode.EpisodeLocalPath))
        {
            Status = _downloadingEpisodeMessage;
            var success = await _podcastService.DownloadEpisode(_selectedPodcast, episode);
            if (!success)
            {
                Status = _couldNotDownloadEpisodeError;
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

        OnPropertyChanged(nameof(PlayOrPauseButtonContent));
        OnPropertyChanged(nameof(PlayingEpisodeListenProgress));
        OnPropertyChanged(nameof(PlayingEpisodeListenProgressMax));
    }

    [RelayCommand]
    public void NextEpisode()
    {
        if (_selectedPodcast is null)
        {
            return;
        }

        if (SelectedEpisode is null)
        {
            SelectedEpisode = _selectedPodcast.Episodes.FirstOrDefault();
            return;
        }

        int nextIndex = _selectedPodcast.Episodes.IndexOf(SelectedEpisode) - 1;
        if (nextIndex >= 0)
        {
            SelectedEpisode = _selectedPodcast.Episodes.ElementAt(nextIndex);
        }
    }

    [RelayCommand]
    public void PreviousEpisode()
    {
        if (_selectedPodcast is null)
        {
            return;
        }

        if (SelectedEpisode is null)
        {
            SelectedEpisode = _selectedPodcast.Episodes.LastOrDefault();
            return;
        }

        int previousIndex = _selectedPodcast.Episodes.IndexOf(SelectedEpisode) + 1;
        if (previousIndex < _selectedPodcast.Episodes.Count)
        {
            SelectedEpisode = _selectedPodcast.Episodes.ElementAt(previousIndex);
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
            Podcast updatedPodcast = await _podcastService.GetPodcast(podcast.PodcastUri);
            updatedPodcasts.Add(updatedPodcast);
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
        if (_selectedPodcast is null)
        {
            return;
        }

        SelectedEpisode = _selectedPodcast.Episodes.LastOrDefault();
    }

    [RelayCommand]
    public void SelectLastEpisode()
    {
        if (_selectedPodcast is null)
        {
            return;
        }

        SelectedEpisode = _selectedPodcast.Episodes.FirstOrDefault();
    }

    [RelayCommand]
    public async Task AddPodcast(iTunesPodcastFinder.Models.Podcast podcast)
    {
        var newFeedUri = new Uri(podcast.FeedUrl);
        var newPodcast = await _podcastService.GetPodcast(newFeedUri).ConfigureAwait(false);

        Podcasts.Add(newPodcast);

        newPodcast.ImageLocalPath = await _podcastService.DownloadImage(newPodcast);

        SearchValue = string.Empty;

        await _podcastService.SaveToDisk(Podcasts);
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
