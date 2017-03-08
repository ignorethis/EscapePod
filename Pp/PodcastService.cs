using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Pp
{
    public class PodcastService
    {
        private string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "EscapePod", "Permanace.json");
        HttpClient httpClient = new HttpClient();

        public async Task<Podcast> GetPodcastAsync(Uri url)
        {
            return await Task.Run(() => {
                var content = httpClient.GetStringAsync(url).Result;
                var document = new HtmlDocument();
                document.LoadHtml(content);
                
                //parse podcast
                //foreach episode in podcast
                    //parse episode



                var channel = document.DocumentNode.SelectSingleNode("//channel");
                string podcastTitle = channel.SelectSingleNode("child::title").InnerText;

                var podcastTitleCard = channel.SelectSingleNode("image") == null ? channel.SelectSingleNode("//*[name()='itunes:image']").Attributes["href"].Value : channel.SelectSingleNode("image").SelectSingleNode("url").InnerText;
                Uri podcastTitleCardUri = new Uri(podcastTitleCard);

                var localPodcastPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "EscapePod", podcastTitle);

                var pod = new Podcast()
                {
                    Url = url,
                    Name = podcastTitle,
                    TitleCard = new Uri(podcastTitleCard),
                    LocalTitleCardFileFullName = GetFileFullName(localPodcastPath, podcastTitle, podcastTitleCard.Split('.').Last()),
                    LocalPodcastPath = localPodcastPath
                };

                var items = document.DocumentNode.SelectNodes("//item");
                List<Episode> epilist = new List<Episode>();
                foreach (var item in items)
                {
                    string episodeTitle = item.SelectSingleNode("title").InnerText;
                    var protoEpisodeNumber = item.SelectNodes("title").First().InnerText;
                    StringWriter episodeTitleWriter = new StringWriter();
                    StringWriter episodeDescriptionWriter = new StringWriter();

                    Regex reg = new Regex(@"\d+");
                    Match matchu = reg.Match(protoEpisodeNumber);
                    if (!matchu.Success)
                    {
                        continue;
                    }

                    int episodeNumber = int.Parse(matchu.Value);
                    string episodeDescription = item.SelectSingleNode("description").InnerText;
                    var episodeEnclosure = item.SelectSingleNode("child::enclosure");
                    var episodeUrl = episodeEnclosure.Attributes["url"].Value;

                    string pubDateString = item.SelectSingleNode("pubdate").InnerText;
                    DateTime? pubDateParam = null;
                    DateTime pubDate;
                    var validPubDate = DateTime.TryParse(pubDateString, out pubDate);

                    if (validPubDate)
                    {
                        pubDateParam = pubDate;
                    }
                    HttpUtility.HtmlDecode(episodeDescription,episodeDescriptionWriter);
                    episodeDescription = episodeDescriptionWriter.ToString();
                    HttpUtility.HtmlDecode(episodeTitle,episodeTitleWriter);
                    episodeTitle = episodeTitleWriter.ToString();
                    double episodeTimestamp = 0.0;
                    Episode episode = new Episode(pod, episodeTitle, episodeNumber, new Uri(episodeUrl), episodeDescription, episodeTimestamp, pubDateParam);
                    
                    episode.LocalPath = GetFileFullName(episode.Podcast.LocalPodcastPath, episode.EpisodeName, episode.EpisodeUri.AbsoluteUri.Split('.').Last());
                    
                    epilist.Add(episode);
                }

                pod.EpisodeList = epilist;
                
                return pod;
            });
        }

        public async Task DownloadEpisodeAsync(Episode episode)
        {
            if (!File.Exists(episode.LocalPath))
            {
                if (!episode.IsDownloaded)
                {
                    await DownloadFileAsync(episode.EpisodeUri, episode.Podcast.LocalPodcastPath, episode.EpisodeName).ConfigureAwait(false);
                    episode.IsDownloaded = true;
                }
            }

            await DownloadFileAsync(episode.EpisodeUri, episode.Podcast.LocalPodcastPath, episode.EpisodeName).ConfigureAwait(false);
        }

        public async Task DownloadTitleCardAsync(Podcast podcast)
        {
            if (!File.Exists(podcast.LocalTitleCardFileFullName))
            {
                await DownloadFileAsync(podcast.TitleCard, podcast.LocalPodcastPath, podcast.Name).ConfigureAwait(false);
                //TODO hack ... plz refactor me after you have moaaar skillz
                podcast.LocalTitleCardFileFullName = podcast.LocalTitleCardFileFullName;
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

        public async Task DownloadFileAsync(Uri url, string localPath, string fileName)
        {
            var response = await httpClient.GetAsync(url).ConfigureAwait(false);
            var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var extension = url.AbsoluteUri.Split('.').Last();
            var fileFullName = GetFileFullName(localPath, fileName, extension);

            using (var fileStream = new FileStream(fileFullName, FileMode.Create, FileAccess.Write))
            {
                await contentStream.CopyToAsync(fileStream).ConfigureAwait(false);
            }
        }

        public string GetFileFullName(string localPath, string fileName, string extension)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var validFileName = string.Join("_", fileName.Split(invalidChars));
            return Path.Combine(localPath, validFileName + "." + extension);
        }

        public void SaveToDisk(List<Podcast> podcasts)
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
    }
}
