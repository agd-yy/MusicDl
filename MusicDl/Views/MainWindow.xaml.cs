using MusicDl.ViewModels;
using System.Windows;

namespace MusicDl.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainViewModel();
    }
}