using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using EscapePod.Models;
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
            return await new PodcastFinder()
                .SearchPodcastsAsync(searchValue)
                .ConfigureAwait(false);
        }

        public async Task<Podcast> GetPodcastAsync(Uri podcastUrl)
        {
            PodcastFinder.HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:77.0) Gecko/20100101 Firefox/77.0");

            var podcastWithEpisodes = await new PodcastFinder()
                .GetPodcastEpisodesAsync(podcastUrl.AbsoluteUri);

            return PodcastConversion(podcastWithEpisodes.Podcast, podcastWithEpisodes.Episodes);
        }

        public async Task<bool> DownloadEpisodeAsync(Episode episode)
        {
            if (File.Exists(episode.LocalPath) && episode.IsDownloaded)
            {
                return true;
            }

            var extension = GetExtensionFromMimeType(episode.AudioFileType);

            var fileFullName = await DownloadFileAsync(
                    episode.EpisodeUri,
                    episode.Podcast.LocalPodcastPath,
                    episode.EpisodeName,
                    extension)
                .ConfigureAwait(false);

            if (fileFullName is null)
            {
                return false;
            }

            episode.LocalPath = fileFullName;
            episode.IsDownloaded = true;

            return true;
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

        public async Task<string?> DownloadFileAsync(Uri uri, string path, string name, string extension)
        {
            var response = await _httpClient.GetAsync(uri).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                // TODO: wir brauchen eine möglichkeit um fehler anzuzeigen.
                return null;
            }

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
            var invalidPathChars = new List<char>(Path.GetInvalidPathChars());
            var validPath = string.Join("_", localPath.Split(invalidPathChars.ToArray()).Select(s => s.Trim()));

            var invalidFileNameChars = new List<char>(Path.GetInvalidFileNameChars());
            invalidFileNameChars.Add('.');
            var validFileName = string.Join("_", fileName.Split(invalidFileNameChars.ToArray()).Select(s => s.Trim()));

            return Path.Combine(validPath, validFileName + "." + extension);
        }

        public async Task SaveToDiskAsync(IEnumerable<Podcast> podcasts)
        {
            EnsureContentDirectoryExists();

            string json = JsonConvert.SerializeObject(podcasts);
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
                return new List<Podcast>();
            }

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
