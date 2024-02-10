using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EscapePod.Models;
using EscapePod.ViewModels;

namespace EscapePod
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel vm = new MainWindowViewModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = vm;
            Closing += OnClosing;
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            if (vm.SelectedSearchPodcast == null)
            {
                return;
            }

            await vm.AddPodcastAsync();
        }

        private async void SearchListBox_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (vm.SelectedSearchPodcast == null)
            {
                return;
            }

            await vm.AddPodcastAsync();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var podcast = (Podcast)((Button)sender).Tag;
            vm.DeletePodcastAsync(podcast);
        }

        private async void PlayOrPause_Click(object sender, RoutedEventArgs e)
        {
            await vm.PlayOrPauseAsync();
        }

        private async void EpisodeList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            await vm.PlayEpisodeAsync();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            vm.NextEpisode();
        }
        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            vm.PreviousEpisode();
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            await vm.UpdateAllPodcastAsync();
        }

        private void Last_Click(object sender, RoutedEventArgs e)
        {
            vm.SelectLastEpisode();
        }

        private void First_Click(object sender, RoutedEventArgs e)
        {
            vm.SelectFirstEpisode();
        }

        private void ProgressBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var positionX = e.GetPosition(progressBar).X;
            double ratio = positionX / progressBar.ActualWidth;
            var newValue = ratio * progressBar.Maximum;

            vm.Seek(newValue);
        }

        private async void OnClosing(object? sender, CancelEventArgs e)
        {
            await vm.CloseApplicationAsync();
        }
    }
}
