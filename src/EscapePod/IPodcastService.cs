using EscapePod.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EscapePod
{
    public interface IPodcastService
    {
        Task<Result> DownloadEpisode(Episode episode);

        Task<Result<string>> DownloadImage(Podcast podcast);

        Task<Result<Podcast>> GetPodcast(Uri podcastUrl);

        List<Podcast> LoadFromDisk();

        Task SaveToDisk(IEnumerable<Podcast> podcasts);

        Task<Result<List<Podcast>>> SearchPodcast(string searchValue);
    }
}
