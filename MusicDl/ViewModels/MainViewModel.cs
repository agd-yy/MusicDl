using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicDl.Models;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace MusicDl.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly HttpClient _httpClient;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isSearching = false;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private int _selectedLimit = 10;

    [ObservableProperty]
    private List<int> _limitOptions = [1, 10, 15, 20, 30, 50, 100];

    [ObservableProperty]
    private AudioQuality _selectedAudioQuality = AudioQuality.Lossless;

    [ObservableProperty]
    private ObservableCollection<MusicDetail> _musicDetails = [];

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            ErrorMessage = "Please enter a search term";
            return;
        }

        try
        {
            IsSearching = true;
            ErrorMessage = string.Empty;
            MusicDetails.Clear();

            // Build the API URL with the search text and selected limit
            string encodedSearchText = HttpUtility.UrlEncode(SearchText);
            string apiUrl = $"https://api.kxzjoker.cn/api/163_search?name={encodedSearchText}&limit={SelectedLimit}";

            // Make the API request
            var response = await _httpClient.GetFromJsonAsync<KxzSearchResp>(apiUrl);

            if (response == null || response.Data == null)
            {
                ErrorMessage = "No results found";
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
            ErrorMessage = $"Network error: {ex.Message}";
        }
        catch (JsonException ex)
        {
            ErrorMessage = $"Invalid response format: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
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
            ErrorMessage = "Invalid music selection";
            return;
        }

        try
        {
            // Indicate that we're working
            IsSearching = true;
            ErrorMessage = string.Empty;

            // Convert AudioQuality enum to lowercase string for API parameter
            string qualityLevel = music.AudioQuality.ToString().ToLower();
            
            // Build the API URL with the music ID and quality level
            string apiUrl = $"https://api.kxzjoker.cn/api/163_music?ids={music.Id}&level={qualityLevel}&type=json";

            // Make the API request
            var response = await _httpClient.GetFromJsonAsync<KxzMusicResp>(apiUrl);

            if (response == null || response.Status != 200)
            {
                ErrorMessage = "Failed to retrieve music details";
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
            ErrorMessage = $"Network error: {ex.Message}";
        }
        catch (JsonException ex)
        {
            ErrorMessage = $"Invalid response format: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
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
            ErrorMessage = "Invalid music selection";
            return;
        }
        try
        {
            // Check if the music URL is available
            if (string.IsNullOrEmpty(music.Url))
            {
                await ParseAsync(music);
            }

            // Indicate that we're working
            IsSearching = true;
            ErrorMessage = string.Empty;
            
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

            // Save the file to disk (you may want to use a save dialog in a real app)
            var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, await response.Content.ReadAsByteArrayAsync());

            // Notify user of success (you might want to use a dialog or notification in a real app)
            ErrorMessage = $"Downloaded: {fileName} to {filePath}";
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = $"Download failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
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

    public MainViewModel()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20) // Set a reasonable timeout for HTTP requests
        };
    }
}
