using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MusicDl.Models;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Web;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace MusicDl.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private ISnackbarService _snackbarService = new SnackbarService();
    private readonly string _configFilePath;

    public MainViewModel()
    {
        // 配置文件路径
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _configFilePath = Path.Combine(appDirectory, "config.json");

        // 加载配置
        LoadConfig();
    }

    public void SetSnackbarService(SnackbarPresenter snackbarPresenter)
    {
        _snackbarService.SetSnackbarPresenter(snackbarPresenter);
    }

    private void ShowMessage(string message, string title = "通知", ControlAppearance appearance = ControlAppearance.Primary)
    {
        _snackbarService?.Show(title, message, appearance, null, TimeSpan.FromSeconds(3));
    }

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isSearching = false;

    [ObservableProperty]
    private int _selectedLimit = 10;

    [ObservableProperty]
    private List<int> _limitOptions = [1, 10, 15, 20, 30, 50, 100];

    [ObservableProperty]
    private AudioQuality _selectedAudioQuality = AudioQuality.Lossless;

    [ObservableProperty]
    private string _saveDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

    [ObservableProperty]
    private ObservableCollection<MusicDetail> _musicDetails = [];

    private bool _isDownload = false;

    #region 配置管理

    /// <summary>
    /// 加载配置
    /// </summary>
    private void LoadConfig()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var config = JsonConvert.DeserializeObject<AppConfig>(json);

                if (config != null)
                {
                    SelectedLimit = config.SelectedLimit;
                    SelectedAudioQuality = config.SelectedAudioQuality;
                    SaveDirectoryPath = config.SaveDirectoryPath;
                }
            }
        }
        catch (Exception)
        {
            // Ignore any errors during loading
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    public void SaveConfig()
    {
        try
        {
            var config = new AppConfig
            {
                SelectedLimit = SelectedLimit,
                SelectedAudioQuality = SelectedAudioQuality,
                SaveDirectoryPath = SaveDirectoryPath
            };

            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(_configFilePath, json);
        }
        catch (Exception ex)
        {
            ShowMessage($"保存配置失败: {ex.Message}", "错误", ControlAppearance.Danger);
        }
    }

    #endregion

    [RelayCommand]
    private void SelectSaveDirectory()
    {
        try
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select folder to save music files",
                InitialDirectory = SaveDirectoryPath,
                Multiselect = false
            };

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                SaveDirectoryPath = dialog.FolderName;
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"无法打开文件夹对话框: {ex.Message}", "错误", ControlAppearance.Danger);
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            ShowMessage("请输入搜索关键词", "错误", ControlAppearance.Danger);
            return;
        }

        try
        {
            IsSearching = true;
            MusicDetails.Clear();

            // Build the API URL with the search text and selected limit
            string encodedSearchText = HttpUtility.UrlEncode(SearchText);
            string apiUrl = $"https://api.kxzjoker.cn/api/163_search?name={encodedSearchText}&limit={SelectedLimit}";

            using var _httpClient = new HttpClient();
            // Make the API request
            var response = await _httpClient.GetFromJsonAsync<KxzSearchResp>(apiUrl);

            if (response == null || response.Data == null)
            {
                ShowMessage("未找到结果", "错误", ControlAppearance.Danger);
                return;
            }

            // Convert and add each song to MusicDetails
            foreach (var songData in response.Data)
            {
                MusicDetails.Add(ConvertToMusicDetail(songData));
            }
        }
        catch (HttpRequestException ex)
        {
            ShowMessage($"网络错误: {ex.Message}", "错误", ControlAppearance.Danger);
        }
        catch (JsonException ex)
        {
            ShowMessage($"无效响应格式: {ex.Message}", "错误", ControlAppearance.Danger);
        }
        catch (Exception ex)
        {
            ShowMessage($"发生了错误: {ex.Message}", "错误", ControlAppearance.Danger);
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task ParseAsync(MusicDetail music)
    {
        if (music == null || string.IsNullOrEmpty(music.Id))
        {
            ShowMessage("无效的音乐选择", "错误", ControlAppearance.Danger);
            return;
        }

        if (!string.IsNullOrEmpty(music.Url))
        {
            if (!_isDownload)
                ShowMusicDetails(music);
            return;
        }

        try
        {
            // Indicate that we're working
            IsSearching = true;

            // Convert AudioQuality enum to lowercase string for API parameter
            string qualityLevel = music.AudioQuality.ToString().ToLower();

            // Build the API URL with the music ID and quality level
            string apiUrl = $"https://api.kxzjoker.cn/api/163_music?ids={music.Id}&level={qualityLevel}&type=json";

            using var _httpClient = new HttpClient();
            // Make the API request
            var response = await _httpClient.GetFromJsonAsync<KxzMusicResp>(apiUrl);

            if (response == null || response.Status != 200)
            {
                ShowMessage("未成功获取音乐详情", "错误", ControlAppearance.Danger);
                return;
            }

            // Update the music details with the response data
            //music.Name = response.Name;
            //music.CoverUrl = response.Pic;
            music.Url = response.Url;

            // Parse the size (remove "MB" and convert to integer bytes)
            if (!string.IsNullOrEmpty(response.Size) && response.Size.EndsWith("MB"))
            {
                if (double.TryParse(response.Size.Replace("MB", "").Trim(), out double sizeInMb))
                {
                    // Convert MB to bytes (approximate)
                    music.Size = (int)(sizeInMb * 1024 * 1024);
                }
            }

            // Get the lyric and translated lyric
            music.Lyric = response.Lyric;

            // The API response already includes the full URL for streaming
            // No need to further process the URL

            // Store the quality level from the response
            // Note: We keep the original AudioQuality enum value as it was used to make the request
        }
        catch (HttpRequestException ex)
        {
            ShowMessage($"网络错误: {ex.Message}", "错误", ControlAppearance.Danger);
        }
        catch (JsonException ex)
        {
            ShowMessage($"无效响应格式: {ex.Message}", "错误", ControlAppearance.Danger);
        }
        catch (Exception ex)
        {
            ShowMessage($"出现了错误: {ex.Message}", "错误", ControlAppearance.Danger);
        }
        finally
        {
            IsSearching = false;
        }

        if (!_isDownload)
            ShowMusicDetails(music);
    }

    [RelayCommand]
    private async Task DownloadAsync(MusicDetail music)
    {
        if (music == null)
        {
            ShowMessage("无效的音乐选择", "错误", ControlAppearance.Danger);
            return;
        }

        // 检查FileDirectory是否非法
        if (string.IsNullOrWhiteSpace(SaveDirectoryPath))
        {
            SaveDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        }
        else if (!Directory.Exists(SaveDirectoryPath))
        {
            try
            {
                Directory.CreateDirectory(SaveDirectoryPath);
            }
            catch (Exception ex)
            {
                ShowMessage($"创建目录失败: {ex.Message}", "错误", ControlAppearance.Danger);
                return;
            }
        }

        try
        {
            _isDownload = true;

            // Check if the music URL is available
            if (string.IsNullOrEmpty(music.Url))
            {
                await ParseAsync(music);
            }

            if (string.IsNullOrEmpty(music.Url))
            {
                ShowMessage("音乐URL不可用，请再试一次。", "错误", ControlAppearance.Danger);
                return;
            }

            // Indicate that we're working
            IsSearching = true;

            // Create a new HttpClient for downloading
            using var downloadClient = new HttpClient();

            // Download the music file
            var response = await downloadClient.GetAsync(music.Url);
            response.EnsureSuccessStatusCode();

            // Get file extension from the header
            string fileExtension = ".mp3"; // Default extension
            if (response.Headers.TryGetValues("x-nos-object-name", out var values))
            {
                string objectName = values.FirstOrDefault() ?? string.Empty;
                // Extract file extension from the object name
                int lastDotIndex = objectName.LastIndexOf('.');
                if (lastDotIndex >= 0 && lastDotIndex < objectName.Length - 1)
                {
                    fileExtension = objectName.Substring(lastDotIndex);
                }
            }

            // Get the file name from the URL and use the correct extension
            string fileName = $"{string.Join("_", music.Artist.Select(x => x.Name))}-{music.Name}{fileExtension}";

            var filePath = Path.Combine(SaveDirectoryPath, fileName);

            // 判断文件是否存在
            if (!File.Exists(filePath))
            {
                await File.WriteAllBytesAsync(filePath, await response.Content.ReadAsByteArrayAsync());
            }

            // 写入标签
            await WriteMusicTag(filePath, music);
        }
        catch (HttpRequestException ex)
        {
            ShowMessage($"下载失败: {ex.Message}", "错误", ControlAppearance.Danger);
        }
        catch (Exception ex)
        {
            ShowMessage($"下载时发生了一个错误: {ex.Message}", "错误", ControlAppearance.Danger);
        }
        finally
        {
            _isDownload = false;
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void Add(MusicDetail music)
    {
        ShowMessage("敬请期待", "提示", ControlAppearance.Caution);
    }

    private void ShowMusicDetails(MusicDetail music)
    {
        if (music == null)
        {
            ShowMessage("暂无音乐详情显示", "错误", ControlAppearance.Danger);
            return;
        }

        try
        {
            // Create the dialog content
            var dialogContent = new Views.MusicDetailDialog
            {
                DataContext = music,
                Owner = System.Windows.Application.Current.MainWindow,
            };

            dialogContent.ShowDialog();
        }
        catch (Exception ex)
        {
            ShowMessage($"显示错误详情: {ex.Message}", "错误", ControlAppearance.Danger);
        }
    }

    private async Task WriteMusicTag(string filePath, MusicDetail music)
    {
        // 使用 TagLibSharp 库来写入音乐标签
        try
        {
            var file = TagLib.File.Create(filePath);
            // 设置音乐标签
            file.Tag.Title = music.Name;
            file.Tag.Performers = [.. music.Artist.Select(a => a.Name)];
            file.Tag.Album = music.Album.Name;
            file.Tag.Lyrics = music.Lyric;

            // Year
            if (DateTime.TryParse(music.Album.PublishTime, out DateTime date) && date.Year != 1970)
            {
                file.Tag.Year = (uint)date.Year;
            }

            // 设置封面图片
            if (!string.IsNullOrEmpty(music.CoverUrl))
            {
                // Download the cover image
                using var httpClient = new HttpClient();
                byte[] imageData = await httpClient.GetByteArrayAsync(music.CoverUrl);

                // Convert the downloaded image to a TagLib.Picture
                var picture = new TagLib.Picture
                {
                    Type = TagLib.PictureType.FrontCover,
                    MimeType = GetMimeTypeFromUrl(music.CoverUrl),
                    Description = "Cover",
                    Data = [.. imageData]
                };

                // Add the picture to the file's tag
                file.Tag.Pictures = [picture];
            }
            // 保存更改
            file.Save();

            ShowMessage("标签已经成功添加");
        }
        catch (Exception ex)
        {
            ShowMessage($"标签写入失败: {ex.Message}", "错误", ControlAppearance.Danger);
        }
    }

    private string GetMimeTypeFromUrl(string url)
    {
        // Default to JPEG if we can't determine
        if (string.IsNullOrEmpty(url))
            return "image/jpeg";

        // Extract the file extension from the URL
        string lowerUrl = url.ToLower();

        if (lowerUrl.EndsWith(".jpg") || lowerUrl.EndsWith(".jpeg"))
            return "image/jpeg";
        if (lowerUrl.EndsWith(".png"))
            return "image/png";
        if (lowerUrl.EndsWith(".gif"))
            return "image/gif";
        if (lowerUrl.EndsWith(".bmp"))
            return "image/bmp";
        if (lowerUrl.EndsWith(".webp"))
            return "image/webp";

        // If the URL doesn't have a file extension, try to look for format indicators
        if (lowerUrl.Contains("format=png"))
            return "image/png";
        if (lowerUrl.Contains("format=jpg") || lowerUrl.Contains("format=jpeg"))
            return "image/jpeg";

        // Default to JPEG as it's most common for album covers
        return "image/jpeg";
    }

    private MusicDetail ConvertToMusicDetail(SongData songData)
    {
        // Create a new MusicDetail object
        var musicDetail = new MusicDetail
        {
            Id = songData.Id.ToString(),
            Name = songData.Name,
            CoverUrl = songData.PicUrl,
            IsMember = songData.Fee == 1, // Assuming Fee=1 means VIP/Member content
            AudioQuality = SelectedAudioQuality, // Use selected quality or map from API if available
            Duration = songData.Duration,
        };

        // Add artists
        if (songData.Artists.Count != 0)
        {
            foreach (var artist in songData.Artists)
            {
                musicDetail.Artist.Add(artist);
            }
        }

        // Add album
        if (songData.Album != null)
        {
            musicDetail.Album = songData.Album;
        }

        return musicDetail;
    }
}