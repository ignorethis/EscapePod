using EscapePod.Models;
using EscapePod.ViewModels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EscapePod.Tests
{
    public class MainWindowViewModelTests
    {
        [Fact]
        public void ConstructorTest_podcastServiceNull()
        {
            Action act = () => new MainWindowViewModel(null);

            act.Should().Throw<ArgumentNullException>().WithParameterName("podcastService");
        }

        [Fact]
        public void ConstructorTest1() 
        { 
            var podcastService = Substitute.For<IPodcastService>();
            podcastService.LoadFromDisk().Returns(new List<Models.Podcast>());

            MainWindowViewModel viewModel = new MainWindowViewModel(podcastService);

            viewModel.Should().NotBeNull();
            viewModel.SelectedSearchPodcast.Should().BeNull();
            viewModel.SelectedPodcast.Should().BeNull();
            viewModel.SelectedPodcastImage.Should().BeNull();
            viewModel.PlayingPodcastImage.Should().BeNull();
            viewModel.PlayingEpisode.Should().BeNull();
            viewModel.SearchValue.Should().BeEmpty();
            viewModel.Status.Should().BeEmpty();
            viewModel.StatusPanelVisible.Should().BeFalse();
            viewModel.Podcasts.Should().BeEmpty();
            viewModel.SearchPodcasts.Should().BeEmpty();
            viewModel.Volume.Should().Be(1.0f);
            viewModel.PlayingEpisodeListenProgress.Should().Be(0.0);
            viewModel.PlayingEpisodeListenProgressMax.Should().Be(TimeSpan.Zero);
            viewModel.IsPlaying.Should().BeFalse();
            viewModel.IsSearching.Should().BeFalse();
            viewModel.SearchListBoxIndex.Should().Be(0);
            viewModel.SelectedPodcastPanelVisible.Should().BeFalse();
            viewModel.EpisodeDescriptionHtmlStyled.Should().NotBeEmpty();

            podcastService.Received().LoadFromDisk();
        }

        [Fact]
        public void ConstructorTest2()
        {
            var podcasts = new List<Podcast>() { new Podcast() };

            var podcastService = Substitute.For<IPodcastService>();
            podcastService.LoadFromDisk().Returns(podcasts);

            MainWindowViewModel viewModel = new MainWindowViewModel(podcastService);

            viewModel.Should().NotBeNull();
            viewModel.SelectedSearchPodcast.Should().BeNull();
            viewModel.SelectedPodcast.Should().BeNull();
            viewModel.SelectedPodcastImage.Should().BeNull();
            viewModel.PlayingPodcastImage.Should().BeNull();
            viewModel.PlayingEpisode.Should().BeNull();
            viewModel.SearchValue.Should().BeEmpty();
            viewModel.Status.Should().BeEmpty();
            viewModel.StatusPanelVisible.Should().BeFalse();
            viewModel.Podcasts.Should().Equal(podcasts);
            viewModel.SearchPodcasts.Should().BeEmpty();
            viewModel.Volume.Should().Be(1.0f);
            viewModel.PlayingEpisodeListenProgress.Should().Be(0.0);
            viewModel.PlayingEpisodeListenProgressMax.Should().Be(TimeSpan.Zero);
            viewModel.IsPlaying.Should().BeFalse();
            viewModel.IsSearching.Should().BeFalse();
            viewModel.SearchListBoxIndex.Should().Be(0);
            viewModel.SelectedPodcastPanelVisible.Should().BeFalse();
            viewModel.EpisodeDescriptionHtmlStyled.Should().NotBeEmpty();

            podcastService.Received().LoadFromDisk();
        }
    }
}
