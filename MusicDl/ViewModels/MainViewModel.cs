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
        if (songData.Artists != null)
        {
            foreach (var artist in songData.Artists)
            {
                musicDetail.Artist.Add(artist);
            }
        }

        // Add album
        if (songData.Album != null)
        {
            musicDetail.Album.Add(songData.Album);
        }

        return musicDetail;
    }

    public MainViewModel()
    {
        _httpClient = new HttpClient();
    }
}
