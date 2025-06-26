using MaterialDesignThemes.Wpf;
using MusicDl.ViewModels;
using System.Windows;

namespace MusicDl.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var vm = new MainViewModel();
        
        DataContext = vm;

        MainSnackbar.MessageQueue = vm.SnackbarMessageQueue as SnackbarMessageQueue;
    }
}