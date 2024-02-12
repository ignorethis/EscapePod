using System;
using EscapePod.Models;

namespace EscapePod.ViewModels
{
    public class MainWindowViewModelDesign : MainWindowViewModel
    {
        public MainWindowViewModelDesign()
        {
            Episode episode1 = new()
            {
                Author = "Episode1_Author",
                Description = "Episode1_Description",
                IsExplicit = false,
                Length = TimeSpan.FromSeconds(100),
                MimeType = "audio/mpeg",
                Name = "Episode1_Name",
                PublishDate = DateTime.Now.AddDays(-5),
                Subtitle = "Episode1_Subtitle",
                Summary = "Episode1_Summary",
                EpisodeUri = new Uri("http://www.test.de/Episode1_EpisodeUri/"),
                ImageUri = new Uri("https://upload.wikimedia.org/wikipedia/commons/1/11/Test-Logo.svg"),
                DownloadState = DownloadState.Downloaded,
                EpisodeLocalPath = string.Empty,
                ImageLocalPath = string.Empty,
                ListenLastAt = DateTime.Now.AddDays(-2),
                ListenStoppedAt = TimeSpan.FromSeconds(80),
            };

            Episode episode2 = new()
            {
                Author = "Episode2_Author",
                Description = "Episode2_Description",
                IsExplicit = false,
                Length = TimeSpan.FromSeconds(100),
                MimeType = "audio/mpeg",
                Name = "Episode2_Name",
                PublishDate = DateTime.Now.AddDays(-5),
                Subtitle = "Episode2_Subtitle",
                Summary = "Episode2_Summary",
                EpisodeUri = new Uri("http://www.test.de/Episode2_EpisodeUri/"),
                ImageUri = new Uri("https://upload.wikimedia.org/wikipedia/commons/1/11/Test-Logo.svg"),
                DownloadState = DownloadState.Downloaded,
                EpisodeLocalPath = string.Empty,
                ImageLocalPath = string.Empty,
                ListenLastAt = DateTime.Now.AddDays(-2),
                ListenStoppedAt = TimeSpan.FromSeconds(40),
            };

            Episode episode3 = new()
            {
                Author = "Episode3_Author",
                Description = "Episode3_Description",
                IsExplicit = false,
                Length = TimeSpan.FromSeconds(100),
                MimeType = "audio/mpeg",
                Name = "Episode3_Name",
                PublishDate = DateTime.Now.AddDays(-5),
                Subtitle = "Episode3_Subtitle",
                Summary = "Episode3_Summary",
                EpisodeUri = new Uri("http://www.test.de/Episode3_EpisodeUri/"),
                ImageUri = new Uri("https://upload.wikimedia.org/wikipedia/commons/1/11/Test-Logo.svg"),
                DownloadState = DownloadState.Downloaded,
                EpisodeLocalPath = string.Empty,
                ImageLocalPath = string.Empty,
                ListenLastAt = DateTime.Now.AddDays(-2),
                ListenStoppedAt = TimeSpan.FromSeconds(60),
            };

            Podcast podcast1 = new()
            {
                Author = "Podcast_Author",
                Copyright = "Podcast_Copyright",
                Description = "Podcast_Description",
                Episodes = [episode1, episode2, episode3],
                Id = "Podcast_Id",
                ImageUri = new Uri("https://upload.wikimedia.org/wikipedia/commons/1/11/Test-Logo.svg"),
                IsExplicit = true,
                Name = "Podcast_Name",
                Language = "Podcast_Language",
                LastUpdate = DateTime.Now.AddDays(-5),
                Subtitle = "Podcast_Subtitle"
            };

            Podcasts.Add(podcast1);
        }
    }
}
