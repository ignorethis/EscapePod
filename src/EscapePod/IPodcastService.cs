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

        /// <summary>
        ///     Downloads an Episode and manages Episode state.
        /// </summary>
        /// <returns><b>NULL</b> if successful, or an error if the operation was not successful.</returns>
        Task<string?> DownloadEpisode(Episode episode);

        Task<(string? fileFullName, string? error)> DownloadFile(Uri uri, string path, string name, string extension);
        Task<(string? fileFullName, string? error)> DownloadImage(Models.Podcast podcast);
        string GetFileFullName(string localPath, string fileName, string extension);
        Task<(Models.Podcast? podcast, string? error)> GetPodcast(Uri podcastUrl);
        List<Models.Podcast> LoadFromDisk();
        Models.Podcast PodcastConversion(PodcastRequestResult podcastRequestResult);
        Task SaveToDisk(IEnumerable<Models.Podcast> podcasts);
        Task<(List<Models.Podcast>? podcasts, string? error)> SearchPodcast(string searchValue);
    }
}
