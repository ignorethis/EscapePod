﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
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
            if (vm.SelectedSearchPodcast != null)
            {
                await vm.AddPodcastAsync();
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            vm.DeletePodcast();
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

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            vm.CloseApplication();
        }
    }
}
