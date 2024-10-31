using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EscapePod.Models;

public sealed class Episode : ObservableObject
{
    [JsonIgnore]
    public Podcast Podcast { get; set; }

    public string Author { get; init; }
    public string Description { get; init; }
    public bool IsExplicit { get; init; }
    public string MimeType { get; init; }
    public string Name { get; init; }
    public DateTime PublishDate { get; init; }
    public string Subtitle { get; init; }
    public string Summary { get; init; }

    public Uri EpisodeUri { get; init; }
    public Uri? ImageUri { get; init; }

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
}