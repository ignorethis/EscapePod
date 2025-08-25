using EscapePod.Models;
using iTunesPodcastFinder.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EscapePod
{
    public interface IPodcastService
    {
        Task<Result> DownloadAllEpisodes(Models.Podcast podcast);

        /// <summary>
        ///     Downloads an Episode and manages Episode state.
        /// </summary>
        /// <returns><b>NULL</b> if successful, or an error if the operation was not successful.</returns>
        Task<Result> DownloadEpisode(Episode episode);

        Task<Result<string>> DownloadFile(Uri uri, string path, string name, string extension);
        Task<Result<string>> DownloadImage(Models.Podcast podcast);
        string GetFileFullName(string localPath, string fileName, string extension);
        Task<Result<Models.Podcast>> GetPodcast(Uri podcastUrl);
        List<Models.Podcast> LoadFromDisk();
        Models.Podcast PodcastConversion(PodcastRequestResult podcastRequestResult);
        Task SaveToDisk(IEnumerable<Models.Podcast> podcasts);
        Task<Result<List<Models.Podcast>>> SearchPodcast(string searchValue);
    }
}
