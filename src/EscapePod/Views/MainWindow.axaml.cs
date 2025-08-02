using Avalonia.Controls;
using EscapePod.ViewModels;

namespace EscapePod.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
