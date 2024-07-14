using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EscapePod.Models;

public sealed class Podcast : ObservableObject
{
    public string Id { get; init; }
    public string Author { get; init; }
    public string Name { get; init; }
    public string Subtitle { get; init; }
    public string Description { get; init; }
    public bool IsExplicit { get; init; }
    public string Language { get; init; }
    public string Copyright { get; init; }
    public DateTime? LastUpdate { get; init; }

    public Uri? PodcastUri { get; init; }
    public Uri? ImageUri { get; init; }
    public Uri? WebsiteUri { get; init; }

    public string DisplayName 
    { 
        get { return Name + " | " + Author; } 
    }

    private string? _imageLocalPath;
    public string? ImageLocalPath
    {
        get => _imageLocalPath;
        set => SetProperty(ref _imageLocalPath, value);
    }

    private string _podcastLocalPath;
    public string PodcastLocalPath
    {
        get => _podcastLocalPath;
        set => SetProperty(ref _podcastLocalPath, value);
    }

    public List<Episode> Episodes { get; set; } = [];
}