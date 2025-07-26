using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using EscapePod.Models;
using iTunesPodcastFinder;
using iTunesPodcastFinder.Models;
using Newtonsoft.Json;
using Podcast = EscapePod.Models.Podcast;

namespace EscapePod;

public sealed class PodcastService : IPodcastService
{
    private readonly string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:122.0) Gecko/20100101 Firefox/122.0";

    private readonly string _contentDirectoryPath;
    private readonly string _savefilePath;
    private readonly HttpClient _httpClient;

    public PodcastService()
    {
        _contentDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "EscapePod");
        _savefilePath = Path.Combine(_contentDirectoryPath, "savefile.json");
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", _userAgent);
    }

    public async Task<List<Podcast>> SearchPodcast(string searchValue)
    {
        var podcasts = await new PodcastFinder()
            .SearchPodcastsAsync(searchValue)
            .ConfigureAwait(false);

        return podcasts.Select(p => GetPodcastFrom(p)).ToList();
    }

    public async Task<Podcast> GetPodcast(Uri podcastUrl)
    {
        PodcastFinder.HttpClient.DefaultRequestHeaders.Add("User-Agent", _userAgent);

        var podcastWithEpisodes = await new PodcastFinder()
            .GetPodcastEpisodesAsync(podcastUrl.AbsoluteUri);

        return PodcastConversion(podcastWithEpisodes);
    }

    public async Task<string?> DownloadEpisode(Episode episode)
    {
        if (File.Exists(episode.EpisodeLocalPath) && episode.DownloadState == DownloadState.Downloaded)
        {
            return null;
        }

        var extension = MimeTypeHelper.GetFileExtensionFromMimeType(episode.MimeType);

        var result = await DownloadFile(
                episode.EpisodeUri,
                episode.Podcast.PodcastLocalPath,
                episode.Name,
                extension)
            .ConfigureAwait(false);

        if (result.error is not null)
        {
            return result.error;
        }

        episode.EpisodeLocalPath = result.fileFullName;
        episode.DownloadState = DownloadState.Downloaded;

        return null;
    }

    public async Task<(string? fileFullName, string? error)> DownloadImage(Podcast podcast)
    {
        if (!File.Exists(podcast.ImageLocalPath))
        {
            return await DownloadFile(podcast.ImageUri, podcast.PodcastLocalPath, podcast.Name, podcast.ImageUri.AbsolutePath.Split('.').Last())
                .ConfigureAwait(false);
        }

        return (podcast.ImageLocalPath,null);
    }

    public async Task DownloadAllEpisodes(Podcast podcast)
    {
        await Task.Run(() =>
        {
            var episodesToDownload = podcast.Episodes.OrderByDescending(el => el.PublishDate);
            Parallel.ForEachAsync(
                episodesToDownload,
                new ParallelOptions() { MaxDegreeOfParallelism = 10 },
                async (episode, _) =>
                {
                    await DownloadEpisode(episode);
                });
        });
    }

    public async Task<(string? fileFullName,string? error)> DownloadFile(Uri uri, string directoryPath, string name, string extension)
    {
        var response = await _httpClient.GetAsync(uri).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return (null, "Cannot download file!");
        }

        var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

        var fileFullName = GetFileFullName(directoryPath, name, extension);
        var directoryName = Path.GetDirectoryName(fileFullName);
        if (directoryName is not null && !Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        await using var fileStream = new FileStream(fileFullName, FileMode.Create, FileAccess.Write);
        await contentStream.CopyToAsync(fileStream).ConfigureAwait(false);

        return (fileFullName,null);
    }

    public string GetFileFullName(string localPath, string fileName, string extension)
    {
        var invalidPathChars = new List<char>(Path.GetInvalidPathChars());
        var validPath = string.Join("_", localPath.Split(invalidPathChars.ToArray()).Select(s => s.Trim()));

        var invalidFileNameChars = new List<char>(Path.GetInvalidFileNameChars());
        invalidFileNameChars.Add('.');
        var validFileName = string.Join("_", fileName.Split(invalidFileNameChars.ToArray()).Select(s => s.Trim()));

        return Path.Combine(validPath, validFileName + "." + extension);
    }

    public async Task SaveToDisk(IEnumerable<Podcast> podcasts)
    {
        EnsureContentDirectoryExists();

        string json = JsonConvert.SerializeObject(podcasts, new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
        await File.WriteAllTextAsync(_savefilePath, json);
    }

    private void EnsureContentDirectoryExists()
    {
        if (Directory.Exists(_contentDirectoryPath))
        {
            return;
        }

        Directory.CreateDirectory(_contentDirectoryPath);
    }

    public List<Podcast> LoadFromDisk()
    {
        if (!File.Exists(_savefilePath))
        {
            return [];
        }

        string fileContent = File.ReadAllText(Path.Combine(_savefilePath));

        var podcasts = JsonConvert.DeserializeObject<List<Podcast>>(fileContent) ?? [];
        foreach (Podcast podcast in podcasts)
        {
            foreach (Episode episode in podcast.Episodes)
            {
                episode.Podcast = podcast;
            }
        }
        return podcasts;
    }

    public Podcast PodcastConversion(PodcastRequestResult podcastRequestResult)
    {
        var podcast = GetPodcastFrom(podcastRequestResult.Podcast);

        foreach (var episode in podcastRequestResult.Episodes)
        {
            podcast.Episodes.Add(GetEpisodeFrom(podcast, episode));
        }

        return podcast;
    }

    private Podcast GetPodcastFrom(iTunesPodcastFinder.Models.Podcast outerPodcast)
    {
        var invalidChars = new List<char>(Path.GetInvalidPathChars());
        invalidChars.Add(':');

        var validPathName = string.Join("_", outerPodcast.Name.Split(invalidChars.ToArray())).Trim();

        var xml = XDocument.Parse("<podcast>" + outerPodcast.InnerXml + "</podcast>");
        var itunesNs = "http://www.itunes.com/dtds/podcast-1.0.dtd";

        var descriptionValue = xml.Descendants(XName.Get("subtitle", itunesNs)).FirstOrDefault()?.Value;
        var explicitValue = xml.Descendants(XName.Get("explicit", itunesNs)).FirstOrDefault()?.Value;
        var languageValue = xml.Descendants("language").FirstOrDefault()?.Value;
        var copyrightValue = xml.Descendants("copyright").FirstOrDefault()?.Value;
        var lastBuildDateValue = xml.Descendants("lastBuildDate").FirstOrDefault()?.Value;

        var validUri = Uri.TryCreate(outerPodcast.FeedUrl, UriKind.RelativeOrAbsolute, out Uri? uri);
        var validTitleCard = Uri.TryCreate(outerPodcast.ArtWork, UriKind.RelativeOrAbsolute, out Uri? titleCardUri);
        var validWebsite = Uri.TryCreate(outerPodcast.ItunesLink, UriKind.RelativeOrAbsolute, out Uri? websiteUri);

        return new Podcast()
        {
            Name = outerPodcast.Name,
            PodcastUri = validUri ? uri : null,
            ImageUri = validTitleCard ? titleCardUri : null,
            Author = outerPodcast.Editor,
            Subtitle = descriptionValue ?? string.Empty,
            Description = outerPodcast.Summary,
            WebsiteUri = validWebsite ? websiteUri : null,
            IsExplicit = explicitValue is "yes",
            Language = languageValue ?? string.Empty,
            Copyright = copyrightValue ?? string.Empty,
            LastUpdate = lastBuildDateValue is null ? null : DateTime.Parse(lastBuildDateValue),
            Id = outerPodcast.ItunesId,
            PodcastLocalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "EscapePod", validPathName),
        };
    }

    private Episode GetEpisodeFrom(Podcast podcast, iTunesPodcastFinder.Models.PodcastEpisode episode)
    {
        var xml = XDocument.Parse("<episode>" + episode.InnerXml + "</episode>");
        var itunesNs = "http://www.itunes.com/dtds/podcast-1.0.dtd";

        var explicitValue = xml.Descendants(XName.Get("explicit", itunesNs)).FirstOrDefault()?.Value;
        var imageValue = xml.Descendants(XName.Get("image", itunesNs)).FirstOrDefault()?.Attribute("href")?.Value;
        var enclosure = xml.Descendants("enclosure").FirstOrDefault();
        var audioFileTypeValue = enclosure?.Attribute("type")?.Value;

        return new Episode
        {
            Podcast = podcast,
            Author = episode.Editor,
            Description = episode.Summary,
            IsExplicit = explicitValue is "yes",
            Length = TimeSpan.Zero,
            MimeType = audioFileTypeValue ?? string.Empty,
            Name = episode.Title,
            PublishDate = episode.PublishedDate,
            Subtitle = episode.Title,
            Summary = episode.Summary,
            EpisodeUri = episode.FileUrl,
            ImageUri = string.IsNullOrEmpty(imageValue) ? null : new Uri(imageValue),
            DownloadState = DownloadState.NotStarted,
            EpisodeLocalPath = string.Empty,
            ImageLocalPath = string.Empty,
            ListenLastAt = null,
            ListenStoppedAt = null,
        };
    }
}