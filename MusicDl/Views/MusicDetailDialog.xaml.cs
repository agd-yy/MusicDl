using MusicDl.Models;
using MusicDl.ViewModels;

namespace MusicDl.Views;

public partial class MusicDetailDialog
{
    public MusicDetailDialog()
    {
        InitializeComponent();
    }

    private void DownloadButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        // 获取当前音乐详情
        if (DataContext is MusicDetail music)
        {
            // 获取主窗口的 ViewModel
            if (System.Windows.Application.Current.MainWindow.DataContext is MainViewModel mainViewModel)
            {
                // 执行下载命令
                mainViewModel.DownloadCommand.Execute(music);

                // 关闭当前窗口
                Close();
            }
        }
    }
}
