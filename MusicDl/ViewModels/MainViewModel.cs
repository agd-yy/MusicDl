using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using MusicDl.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace MusicDl.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public readonly ISnackbarMessageQueue SnackbarMessageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(2));

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
            SnackbarMessageQueue.Enqueue($"Could not open folder dialog: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SnackbarMessageQueue.Enqueue("Please enter a search term");
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
                SnackbarMessageQueue.Enqueue("No results found");
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
            SnackbarMessageQueue.Enqueue($"Network error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            SnackbarMessageQueue.Enqueue($"Invalid response format: {ex.Message}");
        }
        catch (Exception ex)
        {
            SnackbarMessageQueue.Enqueue($"An error occurred: {ex.Message}");
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
            SnackbarMessageQueue.Enqueue("Invalid music selection");
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
                SnackbarMessageQueue.Enqueue("Failed to retrieve music details");
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
            SnackbarMessageQueue.Enqueue($"Network error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            SnackbarMessageQueue.Enqueue($"Invalid response format: {ex.Message}");
        }
        catch (Exception ex)
        {
            SnackbarMessageQueue.Enqueue($"An error occurred: {ex.Message}");
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task DownloadAsync(MusicDetail music)
    {
        if (music == null)
        {
            SnackbarMessageQueue.Enqueue("Invalid music selection");
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
                SnackbarMessageQueue.Enqueue($"Failed to create directory: {ex.Message}");
                return;
            }
        }

        try
        {
            // Check if the music URL is available
            if (string.IsNullOrEmpty(music.Url))
            {
                await ParseAsync(music);
            }

            if (string.IsNullOrEmpty(music.Url))
            {
                SnackbarMessageQueue.Enqueue("Music URL is not available, please try again.");
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

                // Notify user of success (you might want to use a dialog or notification in a real app)
                SnackbarMessageQueue.Enqueue($"Download success");
            }

            // 写入标签
            await WriteMusicTag(filePath, music);
        }
        catch (HttpRequestException ex)
        {
            SnackbarMessageQueue.Enqueue($"Download failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            SnackbarMessageQueue.Enqueue($"An error occurred: {ex.Message}");
        }
        finally
        {
            IsSearching = false;
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

            SnackbarMessageQueue.Enqueue("Tags written successfully");
        }
        catch (Exception ex)
        {
            SnackbarMessageQueue.Enqueue($"Failed to write tags: {ex.Message}");
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
