using MusicDl.ViewModels;

namespace MusicDl.Views;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        var vm = new MainViewModel();

        DataContext = vm;

        // Set up Snackbar with WPF-UI
        vm.SetSnackbarService(MainSnackbar);
    }
}
