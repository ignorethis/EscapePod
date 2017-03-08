using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Environment;

namespace Pp
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
            this.DataContext = vm;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => vm.AddPodcastAsync());
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            vm.DeletePodcast();
        }

        private void PlayOrPause_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => vm.PlayOrPauseAsync());
        }

        private void EpisodeList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            Task.Run(() => vm.PlayEpisode());
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            vm.NextEpisode();
        }
        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            vm.PreviousEpisode();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => vm.UpdateAllPodcastAsync());
        }

        private void Last_Click(object sender, RoutedEventArgs e)
        {
            vm.LastEpisode();
        }

        private void First_Click(object sender, RoutedEventArgs e)
        {
            vm.FirstEpisode();
        }

        private void ProgressBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var positionX = e.GetPosition(progressBar).X;
            double ratio = positionX / progressBar.ActualWidth;
            var newValue = ratio * progressBar.Maximum;

            vm.Seek(newValue);
        }

    }
}
