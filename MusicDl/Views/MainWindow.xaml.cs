using MusicDl.ViewModels;

namespace MusicDl.Views;

public partial class MainWindow
{
    private MainViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();

        DataContext = _vm;

        // Set up Snackbar with WPF-UI
        _vm.SetSnackbarService(MainSnackbar);
    }

    private void FluentWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _vm.SaveConfig();
    }
}
