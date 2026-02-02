using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EscapePod.Models;

public sealed class Podcast : ObservableObject
{
    public string Id { get; set; }
    public string Author { get; set; }
    public string Name { get; set; }
    public string Subtitle { get; set; }
    public string Description { get; set; }
    public bool IsExplicit { get; set; }
    public string Language { get; set; }
    public string Copyright { get; set; }
    public DateTime? LastUpdate { get; set; }

    public required Uri PodcastUri { get; init; }
    public Uri? ImageUri { get; set; }
    public Uri? WebsiteUri { get; set; }

    public string DisplayName => Name + " | " + Author;

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

    public void ApplyUpdate(Podcast updatedPodcast)
    {
        Name = updatedPodcast.Name;
        // PodcastUri must stay same
        ImageUri = updatedPodcast.ImageUri;
        Author = updatedPodcast.Author;
        Subtitle = updatedPodcast.Subtitle;
        Description = updatedPodcast.Description;
        WebsiteUri = updatedPodcast.WebsiteUri;
        IsExplicit = updatedPodcast.IsExplicit;
        Language = updatedPodcast.Language;
        Copyright = updatedPodcast.Copyright;
        LastUpdate = updatedPodcast.LastUpdate;
        Id = updatedPodcast.Id;
        PodcastLocalPath = updatedPodcast.PodcastLocalPath;
    }
}
