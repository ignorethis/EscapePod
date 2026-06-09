using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EscapePod.Models;

public sealed class Episode : ObservableObject
{
    [JsonIgnore]
    public Podcast Podcast { get; set; }

    public string Author { get; set; }
    public string Description { get; set; }
    public bool IsExplicit { get; set; }
    public string MimeType { get; set; }
    public string Name { get; set; }
    public DateTime PublishDate { get; set; }
    public string Subtitle { get; set; }
    public string Summary { get; set; }

    public Uri EpisodeUri { get; init; }
    public Uri? ImageUri { get; set; }

    public string EpisodeLocalPath { get; set; }
    public string ImageLocalPath { get; set; }

    private DownloadState _downloadState;
    public DownloadState DownloadState
    {
        get => _downloadState;
        set => SetProperty(ref _downloadState, value);
    }

    private TimeSpan _length;
    public TimeSpan Length
    {
        get => _length;
        set
        {
            if (!SetProperty(ref _length, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ListenPercent));
        }
    }

    private DateTime? _listenLastAt;
    public DateTime? ListenLastAt
    {
        get => _listenLastAt;
        set
        {
            if (!SetProperty(ref _listenLastAt, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ListenPercent));
        }
    }

    private TimeSpan? _listenStoppedAt;
    public TimeSpan? ListenStoppedAt
    {
        get => _listenStoppedAt;
        set
        {
            if (!SetProperty(ref _listenStoppedAt, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ListenPercent));
        }
    }

    public double ListenPercent
    {
        get
        {
            if (ListenLastAt is null)
            {
                return 0;
            }

            if (Length == TimeSpan.Zero)
            {
                return 5; //ListenLastAt != null
            }

            return (ListenStoppedAt ?? TimeSpan.Zero) / Length * 100;
        }
    }

    public ListenState ListenState =>
        ListenPercent switch
        {
            0 => ListenState.NotStarted,
            > 0 and <= 95 => ListenState.Started,
            > 95 and <= 100 => ListenState.Finished,
            _ => throw new SwitchExpressionException(ListenPercent)
        };

    public void ApplyUpdate(Episode updatedEpisode)
    {
        // Podcast stays the same
        Author = updatedEpisode.Author;
        Description = updatedEpisode.Description;
        IsExplicit = updatedEpisode.IsExplicit;
        // Length stays the same
        MimeType = updatedEpisode.MimeType;
        Name = updatedEpisode.Name;
        PublishDate = updatedEpisode.PublishDate;
        Subtitle = updatedEpisode.Subtitle;
        Summary = updatedEpisode.Summary;
        // EpisodeUri stays the same
        ImageUri = updatedEpisode.ImageUri;
        // DownloadState stays the same
        // EpisodeLocalPath stays the same
        // ImageLocalPath stays the same
        // ListenLastAt  stays the same
        // ListenStoppedAt stays the same
    }
}
