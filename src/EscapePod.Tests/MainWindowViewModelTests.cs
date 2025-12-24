using EscapePod.Models;
using EscapePod.ViewModels;
using NSubstitute;

namespace EscapePod.Tests
{
    public class MainWindowViewModelTests
    {
        [Test]
        public async Task ConstructorTest_PodcastServiceNull()
        {
            Action act = () => new MainWindowViewModel(null);

            await Assert.That(act).Throws<ArgumentNullException>().WithParameterName("podcastService");
        }

        [Test]
        public async Task ConstructorTest1()
        {
            var podcastService = Substitute.For<IPodcastService>();
            podcastService.LoadFromDisk().Returns(new List<Podcast>());

            var viewModel = new MainWindowViewModel(podcastService);

            using (Assert.Multiple())
            {
                await Assert.That(viewModel).IsNotNull();
                await Assert.That(viewModel.SelectedSearchPodcast).IsNull();
                await Assert.That(viewModel.SelectedPodcast).IsNull();
                await Assert.That(viewModel.SelectedPodcastImage).IsNull();
                await Assert.That(viewModel.PlayingPodcastImage).IsNull();
                await Assert.That(viewModel.PlayingEpisode).IsNull();
                await Assert.That(viewModel.SearchValue).IsEmpty();
                await Assert.That(viewModel.Status).IsEmpty();
                await Assert.That(viewModel.StatusPanelVisible).IsFalse();
                await Assert.That(viewModel.Podcasts).IsEmpty();
                await Assert.That(viewModel.SearchPodcasts).IsEmpty();
                await Assert.That(viewModel.Volume).IsEqualTo(1.0f);
                await Assert.That(viewModel.PlayingEpisodeListenProgress).IsEqualTo(0.0);
                await Assert.That(viewModel.PlayingEpisodeListenProgressMax).IsEqualTo(TimeSpan.Zero);
                await Assert.That(viewModel.IsPlaying).IsFalse();
                await Assert.That(viewModel.IsSearching).IsFalse();
                await Assert.That(viewModel.SearchListBoxIndex).IsEqualTo(0);
                await Assert.That(viewModel.SelectedPodcastPanelVisible).IsFalse();
                await Assert.That(viewModel.EpisodeDescriptionHtmlStyled).IsNotEmpty();
            }

            podcastService.Received().LoadFromDisk();
        }

        [Test]
        public async Task ConstructorTest2()
        {
            var podcasts = new List<Podcast>() { new Podcast() };

            var podcastService = Substitute.For<IPodcastService>();
            podcastService.LoadFromDisk().Returns(podcasts);

            var viewModel = new MainWindowViewModel(podcastService);

            using (Assert.Multiple())
            {
                await Assert.That(viewModel).IsNotNull();
                await Assert.That(viewModel.SelectedSearchPodcast).IsNull();
                await Assert.That(viewModel.SelectedPodcast).IsNull();
                await Assert.That(viewModel.SelectedPodcastImage).IsNull();
                await Assert.That(viewModel.PlayingPodcastImage).IsNull();
                await Assert.That(viewModel.PlayingEpisode).IsNull();
                await Assert.That(viewModel.SearchValue).IsEmpty();
                await Assert.That(viewModel.Status).IsEmpty();
                await Assert.That(viewModel.StatusPanelVisible).IsFalse();
                await Assert.That(viewModel.Podcasts).IsEquivalentTo(podcasts);
                await Assert.That(viewModel.SearchPodcasts).IsEmpty();
                await Assert.That(viewModel.Volume).IsEqualTo(1.0f);
                await Assert.That(viewModel.PlayingEpisodeListenProgress).IsEqualTo(0.0);
                await Assert.That(viewModel.PlayingEpisodeListenProgressMax).IsEqualTo(TimeSpan.Zero);
                await Assert.That(viewModel.IsPlaying).IsFalse();
                await Assert.That(viewModel.IsSearching).IsFalse();
                await Assert.That(viewModel.SearchListBoxIndex).IsEqualTo(0);
                await Assert.That(viewModel.SelectedPodcastPanelVisible).IsFalse();
                await Assert.That(viewModel.EpisodeDescriptionHtmlStyled).IsNotEmpty();
            }

            podcastService.Received().LoadFromDisk();
        }
    }
}
