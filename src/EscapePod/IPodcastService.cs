using EscapePod.Models;
using iTunesPodcastFinder.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EscapePod
{
    public interface IPodcastService
    {
        Task DownloadAllEpisodes(Models.Podcast podcast);
        Task<bool> DownloadEpisode(Episode episode);
        Task<string?> DownloadFile(Uri uri, string path, string name, string extension);
        Task<string?> DownloadImage(Models.Podcast podcast);
        string GetFileFullName(string localPath, string fileName, string extension);
        Task<Models.Podcast> GetPodcast(Uri podcastUrl);
        List<Models.Podcast> LoadFromDisk();
        Models.Podcast PodcastConversion(PodcastRequestResult podcastRequestResult);
        Task SaveToDisk(IEnumerable<Models.Podcast> podcasts);
        Task<List<Models.Podcast>> SearchPodcast(string searchValue);
    }
}