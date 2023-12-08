using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using EscapePod.Models;
using HtmlAgilityPack;
using iTunesPodcastFinder;
using Newtonsoft.Json;

namespace EscapePod
{
    public class PodcastService
    {
        private readonly string _contentDirectoryPath;
        private readonly string _savefilePath;
        private readonly HttpClient _httpClient;

        public PodcastService()
        {
            _contentDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "EscapePod");
            _savefilePath = Path.Combine(_contentDirectoryPath, "savefile.json");
            _httpClient = new HttpClient();
        }

        public async Task<IEnumerable<iTunesPodcastFinder.Models.Podcast>> SearchPodcastAsync(string searchValue)
        {
            var podcastFinder = new PodcastFinder();
            return await podcastFinder.SearchPodcastsAsync(searchValue)
                .ConfigureAwait(false);
        }

        public async Task<Podcast> GetPodcastAsync(Uri podcastUrl)
        {
            var podcastFinder = new PodcastFinder();
            PodcastFinder.HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:77.0) Gecko/20100101 Firefox/77.0");
            var getPodcastWithEpisodesResult = await podcastFinder.GetPodcastEpisodesAsync(podcastUrl.AbsoluteUri);
            var podcast = PodcastConversion(getPodcastWithEpisodesResult.Podcast, getPodcastWithEpisodesResult.Episodes);
            return podcast;

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
                var extension = GetExtensionFromMimeType(episode.AudioFileType);

                var fileFullName = await DownloadFileAsync(episode.EpisodeUri, episode.Podcast.LocalPodcastPath, episode.EpisodeName, extension)
                    .ConfigureAwait(false);
                episode.LocalPath = fileFullName;
                episode.IsDownloaded = true;
            }
        }

        //doc http://streaming.osu.edu/podcast/Example/Podcast.xml Podcast Supported Enclosures
        private string GetExtensionFromMimeType(string mimeType)
        {
            return mimeType switch
            {
                "audio/mpeg" => "mp3",
                "audio/x-m4a" => "m4a",
                "audio/x-wav" => "wav",
                "audio/x-aiff" => "aif",
                "audio/x-pn-realaudio" => "ra",
                "audio/x-ms-wma" => "wma",
                "audio/midi" => "mid",
                _ => "mp3"
            };
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
            var response = await _httpClient.GetAsync(uri).ConfigureAwait(false);
            var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var fileFullName = GetFileFullName(path, name, extension);
            var directoryName = Path.GetDirectoryName(fileFullName);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

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
            EnsureContentDirectoryExists();

            string json = JsonConvert.SerializeObject(podcasts);
            File.WriteAllText(_savefilePath, json);
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
            if (File.Exists(_savefilePath))
            {
                string fileContent = File.ReadAllText(Path.Combine(_savefilePath));
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

            return new List<Podcast>();
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
            var result = GetPodcastFrom(outerPodcast);

            foreach (var episode in podcastEpisodes)
            {
                result.EpisodeList.Add(GetEpisodeFrom(result, episode));
            }

            return result;
        }

        private Podcast GetPodcastFrom(iTunesPodcastFinder.Models.Podcast outerPodcast)
        {
            var invalidChars = Path.GetInvalidPathChars();
            var validPathName = string.Join("_", outerPodcast.Name.Split(invalidChars));

            var xml = XDocument.Parse("<podcast>" + outerPodcast.InnerXml + "</podcast>");
            var itunesNs = "http://www.itunes.com/dtds/podcast-1.0.dtd";

            var descriptionValue = xml.Descendants(XName.Get("subtitle", itunesNs)).FirstOrDefault()?.Value;
            var explicitValue = xml.Descendants(XName.Get("explicit", itunesNs)).FirstOrDefault()?.Value;
            var languageValue = xml.Descendants("language").FirstOrDefault()?.Value;
            var copyrightValue = xml.Descendants("copyright").FirstOrDefault()?.Value;
            var lastBuildDateValue = xml.Descendants("lastBuildDate").FirstOrDefault()?.Value;

            var validUri = Uri.TryCreate(outerPodcast.FeedUrl, UriKind.RelativeOrAbsolute, out Uri uri);
            var validTitleCard = Uri.TryCreate(outerPodcast.ArtWork, UriKind.RelativeOrAbsolute, out Uri titleCardUri);
            var validWebsite = Uri.TryCreate(outerPodcast.ItunesLink, UriKind.RelativeOrAbsolute, out Uri websiteUri);

            return new Podcast()
            {
                Name = outerPodcast.Name,
                Uri = validUri ? uri : null,
                TitleCard = validTitleCard ? titleCardUri : null,
                Author = outerPodcast.Editor,
                Subtitle = descriptionValue,
                Description = outerPodcast.Summary,
                Website = validWebsite ? websiteUri : null,
                IsExplicit = explicitValue != null && explicitValue == "yes" ? true : false,
                Language = languageValue,
                Copyright = copyrightValue,
                EpisodeCount = outerPodcast.EpisodesCount,
                LastUpdate = lastBuildDateValue == null ? (DateTime?)null : DateTime.Parse(lastBuildDateValue),
                Id = outerPodcast.ItunesId,
                LocalPodcastPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "EscapePod", validPathName),
            };
        }

        private Episode GetEpisodeFrom(Podcast podcast, iTunesPodcastFinder.Models.PodcastEpisode episode)
        {
            var xml = XDocument.Parse("<episode>" + episode.InnerXml + "</episode>");
            var itunesNs = "http://www.itunes.com/dtds/podcast-1.0.dtd";
            
            var explicitValue = xml.Descendants(XName.Get("explicit", itunesNs)).FirstOrDefault()?.Value;
            var imageValue = xml.Descendants(XName.Get("image", itunesNs)).FirstOrDefault()?.Attribute("href")?.Value;
            var enclosure = xml.Descendants("enclosure").FirstOrDefault();
            var byteLengthValue = enclosure?.Attribute("length")?.Value;
            var audioFileTypeValue = enclosure?.Attribute("type")?.Value;

            return new Episode(
                podcast, 
                episode.Title, 
                episode.FileUrl, 
                episode.Summary, 
                0.0, 
                episode.PublishedDate, 
                episode.Duration, 
                episode.Title, 
                episode.Editor,
                explicitValue != null && explicitValue == "yes" ? true : false,
                episode.Summary, 
                imageValue,
                byteLengthValue,
                audioFileTypeValue, 
                false,
                string.Empty
            );
        }
    }
}
