using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Pp
{
    public class PodcastService
    {
        private string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "EscapePod", "savefile.json");
        private HttpClient httpClient = new HttpClient();

        public async Task<IEnumerable<iTunesPodcastFinder.Models.Podcast>> SearchPodcastAsync(string query)
        {
            var podcastFinder = new iTunesPodcastFinder.PodcastFinder();
            return await podcastFinder.SearchPodcastsAsync(query)
                .ConfigureAwait(false);
        }

        public async Task<Podcast> GetPodcastAsync(Uri podcastUrl)
        {
            var podcastFinder = new iTunesPodcastFinder.PodcastFinder();
            var getPodcastWithEpisodesResult = await podcastFinder.GetPodcastEpisodesAsync(podcastUrl.AbsoluteUri);
            return PodcastConversion(getPodcastWithEpisodesResult.Podcast, getPodcastWithEpisodesResult.Episodes);

            /*
            string podcastTitle = podcast.Name;

            string podcastTitleCard = (podcast.Image.Split('/').Last());
                
            Uri podcastTitleCardUri = new Uri(podcast.Image);

            var localPodcastPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "EscapePod", podcastTitle);

            var pod = new Podcast()
            {
                Url = new Uri(podcast.FeedUrl),
                Name = podcast.Name,
                TitleCard = new Uri(podcastTitleCard),
                LocalTitleCardFileFullName = GetFileFullName(localPodcastPath, podcastTitle, podcastTitleCard.Split('.').Last()),
                LocalPodcastPath = localPodcastPath
            };

                
            List<Episode> epilist = new List<Episode>();
            foreach (var item in podcast.Episodes)
            {
                string episodeTitle = item.Title;
                    
                StringWriter episodeTitleWriter = new StringWriter();
                StringWriter episodeDescriptionWriter = new StringWriter();

                Regex reg = new Regex(@"\d+");
                Match match = reg.Match(item.Title);
                if (!match.Success)
                {
                    continue;
                }

                int episodeNumber = int.Parse(match.Value);
                string episodeDescription = item.Summary;
                var episodeUrl = item.Link;
                                        
                DateTime? pubDateParam = null;
                DateTime pubDate = item.PubDate;

                System.Web.HttpUtility.HtmlDecode(episodeDescription,episodeDescriptionWriter);
                episodeDescription = episodeDescriptionWriter.ToString();
                System.Web.HttpUtility.HtmlDecode(episodeTitle,episodeTitleWriter);
                episodeTitle = episodeTitleWriter.ToString();
                episodeDescription = EpisodeDescriptionParser(episodeDescription);
                double episodeTimestamp = 0.0;
                Episode episode = new Episode(pod, episodeTitle, episodeNumber, new Uri(episodeUrl), episodeDescription, episodeTimestamp, pubDateParam);
                    
                episode.LocalPath = GetFileFullName(episode.Podcast.LocalPodcastPath, episode.EpisodeName, episode.EpisodeUri.AbsoluteUri.Split('.').Last());
                    
                epilist.Add(episode);
            }

            pod.EpisodeList = epilist;
            */

        }

        public async Task DownloadEpisodeAsync(Episode episode)
        {
            if (!File.Exists(episode.LocalPath) || !episode.IsDownloaded)
            {
                var fileFullName = await DownloadFileAsync(episode.EpisodeUri, episode.Podcast.LocalPodcastPath, episode.EpisodeName, "mp3").ConfigureAwait(false);
                episode.LocalPath = fileFullName;
                episode.IsDownloaded = true;
            }
        }

        public async Task DownloadTitleCardAsync(Podcast podcast)
        {
            if (!File.Exists(podcast.LocalTitleCardFileFullName))
            {
                var fileFullName = await DownloadFileAsync(podcast.TitleCard, podcast.LocalPodcastPath, podcast.Name, podcast.TitleCard.AbsolutePath.Split('.').Last()).ConfigureAwait(false);
                //TODO hack ... plz refactor me after you have moaaar skillz
                podcast.LocalTitleCardFileFullName = fileFullName;
            }
        }

        public async Task DownloadAllEpisodesAsync(Podcast podcast)
        {
            await Task.Run(() => {
                var episodesToDownload = podcast.EpisodeList.OrderByDescending(el => el.PublishDate);
                Parallel.ForEach(episodesToDownload, new ParallelOptions() { MaxDegreeOfParallelism = 10 }, async (episode) =>
                {
                    await DownloadEpisodeAsync(episode);
                });
            });
        }

        public async Task<string> DownloadFileAsync(Uri uri, string path, string name, string extension)
        {
            var response = await httpClient.GetAsync(uri).ConfigureAwait(false);
            var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var fileFullName = GetFileFullName(path, name, extension);

            using (var fileStream = new FileStream(fileFullName, FileMode.Create, FileAccess.Write))
            {
                await contentStream.CopyToAsync(fileStream).ConfigureAwait(false);
            }

            return fileFullName;
        }

        public string GetFileFullName(string localPath, string fileName, string extension)
        {
            var invalidPathChars = Path.GetInvalidPathChars();
            var validPath = string.Join("_", localPath.Split(invalidPathChars));

            var invalidFileNameChars = Path.GetInvalidFileNameChars();
            var validFileName = string.Join("_", fileName.Split(invalidFileNameChars));

            return Path.Combine(validPath, validFileName + "." + extension);
        }

        public void SaveToDisk(IEnumerable<Podcast> podcasts)
        {
            string json = JsonConvert.SerializeObject(podcasts);
            File.WriteAllText(filePath, json);
        }

        public List<Podcast> LoadFromDisk()
        {
            if (File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(Path.Combine(filePath));
                var podcasts = JsonConvert.DeserializeObject<List<Podcast>>(fileContent);

                foreach (var podcast in podcasts)
                {
                    foreach (var episode in podcast.EpisodeList)
                    {
                        episode.Podcast = podcast;
                    }
                }

                return podcasts;
            }
            else
            {
                return new List<Podcast>();
            }
        }

        public string EpisodeDescriptionParser(string descriptionText)
        {
            Regex link = new Regex(@"<a([A-Za-z0-9\-\._\~\:\/\?\#\[\]\@\!\$\&\'\(\)\*\+\,\;\=""\>\s]+)</a>");
            Regex urlFromLink = new Regex(@"""([a-zA-Z0-9.\/:]+)"">");
            Regex titleFromLink = new Regex(@">([a-zA-Z0-9.\/:""]+)<\/a>");
            Regex linebreaks = new Regex(@"\<br\>");
            Regex boldText = new Regex(@"\<b\>");
            Regex boldTextEnd = new Regex(@"\<\/b\>");
            Regex stupidityLinebreaks = new Regex(@"\<br.\/\>");

            while (true)
            {
                var match = link.Match(descriptionText);
                if (!match.Success)
                {
                    break;
                }

                var rege = link.Options;
                int lon = descriptionText.Length;
                descriptionText = descriptionText.Remove(match.Index, match.Length);

                var node = HtmlNode.CreateNode(match.Value);
                var url = node.GetAttributeValue("href", string.Empty);
                var content = node.InnerHtml;

                descriptionText = descriptionText.Insert(match.Index, content + ": " + url);
            }

            descriptionText = linebreaks.Replace(descriptionText,"\n");
            descriptionText = boldText.Replace(descriptionText,"");
            descriptionText = boldTextEnd.Replace(descriptionText,"");
            descriptionText = stupidityLinebreaks.Replace(descriptionText,"\n");
            return descriptionText;
        }

        public Podcast PodcastConversion(iTunesPodcastFinder.Models.Podcast outerPodcast, IEnumerable<iTunesPodcastFinder.Models.PodcastEpisode> podcastEpisodes)
        {
            var invalidChars = Path.GetInvalidPathChars();
            var validPathName = string.Join("_", outerPodcast.Name.Split(invalidChars));

            Podcast result = new Podcast()
            {
                Name = outerPodcast.Name,
                Url = new Uri(outerPodcast.FeedUrl),
                TitleCard = new Uri(outerPodcast.ArtWork),
                Author = outerPodcast.Editor,
                Subtitle = string.Empty, //TODO find replacement
                Description = outerPodcast.Summary,
                Website = new Uri(outerPodcast.ItunesLink),
                IsExplicid = false,
                Language = string.Empty, //TODO find replacement
                Copyright = string.Empty, //TODO find replacement
                EpisodeCount = outerPodcast.EpisodesCount,
                LastUpdate = DateTime.MinValue, //TODO find replacement
                Id = outerPodcast.ItunesId,
                LocalPodcastPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "EscapePod", validPathName),
            };

            foreach (var episode in podcastEpisodes)
            {
                var newEpisode = new Episode(result, episode.Title, episode.FileUrl, episode.Summary, 0.0, episode.PublishedDate, episode.Duration, episode.Title, episode.Editor, false, episode.Summary, string.Empty, string.Empty, string.Empty, false, string.Empty);
                result.EpisodeList.Add(newEpisode);
            }

            return result;
        }

    }
}
