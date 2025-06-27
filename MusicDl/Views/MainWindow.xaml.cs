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

    private void FluentWindow_Activated(object sender, System.EventArgs e)
    {
        // 当窗口激活时，自动选中搜索框并全选文本
        FocusAndSelectSearchTextBox();
    }

    /// <summary>
    /// 聚焦搜索框并全选文本
    /// </summary>
    private void FocusAndSelectSearchTextBox()
    {
        try
        {
            // 使用 Dispatcher.BeginInvoke 确保在UI完全加载后执行
            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                if (SearchTextBox != null)
                {
                    // 聚焦到搜索框
                    SearchTextBox.Focus();

                    // 全选文本内容
                    SearchTextBox.SelectAll();
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }
        catch (System.Exception)
        {
            // 忽略任何可能的异常，避免影响程序运行
        }
    }

    /// <summary>
    /// 公共方法，允许外部调用来聚焦搜索框
    /// </summary>
    public void FocusSearchBox()
    {
        FocusAndSelectSearchTextBox();
    }
}