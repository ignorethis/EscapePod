using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Collections;
using Avalonia.Media.Imaging;
using Cairo;
using CommunityToolkit.Mvvm.ComponentModel;
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

    [ObservableProperty] private iTunesPodcastFinder.Models.Podcast? _selectedSearchPodcast;
    [ObservableProperty] private Podcast? _selectedPodcast;
    [ObservableProperty] private Bitmap? _selectedPodcastImage;
    [ObservableProperty] private Episode? _selectedEpisode;
    [ObservableProperty] private Episode? _playingEpisode;
    [ObservableProperty] private string _searchValue = string.Empty;
    [ObservableProperty] private string _status = string.Empty;

    public AvaloniaList<Podcast> Podcasts { get; init; } = [];
    public AvaloniaList<iTunesPodcastFinder.Models.Podcast> SearchPodcasts { get; init; } = [];

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

    public string EpisodeDescriptionHtmlStyled
    {
        get
        {
            if (_selectedEpisode is null)
            {
                return string.Empty;
            }

            return $$"""
                   <html>
                   <head>
                     <style>
                       body {
                           background-color: black;
                           color: white;
                       }
                       
                       a {
                            color: white;
                       }
                     </style>
                   </head>
                   <body>
                     {{_selectedEpisode.Description}}
                   </body>
                   </html>
                   """;
        }
    }

    public MainWindowViewModel()
    {
        var podcasts = _podcastService.LoadFromDisk();
        Podcasts.AddRange(podcasts);
        _episodeIsPlayingTimer.Elapsed += EpisodeIsPlayingTimer_Elapsed;
    }

    private async void EpisodeIsPlayingTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        if (_playingEpisode == null || _selectedPodcast == null || _selectedEpisode == null)
        {
            return;
        }

        _playingEpisode.ListenStoppedAt = _audioFileReader.CurrentTime;
        _playingEpisode.ListenLastAt = e.SignalTime;

        if (_audioFileReader.CurrentTime > _playingEpisode.Length * 0.8)
        {
            var nextIndex = _selectedPodcast.Episodes.IndexOf(_selectedEpisode) - 1;
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

        _audioFileReader = new AudioFileReader(episode.EpisodeLocalPath);
        _audioPlayer.Init(_audioFileReader);
        _audioFileReader.CurrentTime = episode.ListenStoppedAt ?? TimeSpan.Zero;
        _audioPlayer.Play();
        _episodeIsPlayingTimer.Start();

        episode.Length = _audioFileReader.TotalTime;

        PlayingEpisode = episode;

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

        if (_selectedEpisode is null)
        {
            SelectedEpisode = _selectedPodcast.Episodes.FirstOrDefault();
            return;
        }

        int nextIndex = _selectedPodcast.Episodes.IndexOf(_selectedEpisode) - 1;
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

        if (_selectedEpisode is null)
        {
            SelectedEpisode = _selectedPodcast.Episodes.LastOrDefault();
            return;
        }

        int previousIndex = _selectedPodcast.Episodes.IndexOf(_selectedEpisode) + 1;
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
        SelectedPodcast = oldSelectedPodcastUri == null
            ? null
            : Podcasts.FirstOrDefault(p => p.PodcastUri == oldSelectedPodcastUri);

        if (_selectedPodcast is not null)
        {
            SelectedEpisode = oldSelectedEpisodeUri == null
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

        //TODO hier noch bug fixen, dass bild erst gesetzt wird bevor es runtergeladen ist ... und dann crap binding exception kommt :C
        newPodcast.ImageLocalPath = await _podcastService.DownloadImage(newPodcast);

        SearchValue = string.Empty;

        await _podcastService.SaveToDisk(Podcasts);

        var firstSuccess =  await _podcastService.DownloadEpisode(newPodcast, newPodcast.Episodes.First()).ConfigureAwait(false);
        if (!firstSuccess)
        {
            Status = _couldNotDownloadEpisodeError;
        }
        var lastSuccess = await _podcastService.DownloadEpisode(newPodcast, newPodcast.Episodes.Last()).ConfigureAwait(false);
        if (!lastSuccess)
        {
            Status = _couldNotDownloadEpisodeError;
        }
    }

    [RelayCommand]
    public async Task DeletePodcast(Podcast? podcast)
    {
        if (podcast is null)
        {
            return;
        }

        Podcasts.Remove(podcast);

        await _podcastService.SaveToDisk(Podcasts);
    }

    partial void OnSelectedPodcastChanged(Podcast? podcast)
    {
        if (string.IsNullOrEmpty(podcast?.ImageLocalPath))
        {
            return;
        }

        if (!File.Exists(podcast.ImageLocalPath))
        {
            return;
        }

        SelectedPodcastImage = new Bitmap(podcast.ImageLocalPath);

        // Switch once updated and make async
        //SelectedPodcastImage = Bitmap.DecodeToHeight(File.OpenRead(podcast.ImageLocalPath), 200);
    }

    partial void OnSelectedEpisodeChanged(Episode? value)
    {
        OnPropertyChanged(nameof(EpisodeDescriptionHtmlStyled));
    }



    partial void OnSearchValueChanged(string value)
    {
        PodcastSearch(value).ConfigureAwait(true);
        OnPropertyChanged(nameof(SearchListBoxIndex));
    }

    partial void OnStatusChanged(string value)
    {
        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));

            Status = string.Empty;
        });
    }
}
