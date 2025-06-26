using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace MusicDl.Models;

public partial class MusicDetail : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;
    [ObservableProperty]
    private string _name = string.Empty;
    [ObservableProperty]
    private bool _isMember = false;
    [ObservableProperty]
    private string _coverUrl = string.Empty;
    [ObservableProperty]
    private ObservableCollection<Artist> _artist = [];
    [ObservableProperty]
    private ObservableCollection<Album> _album = [];
    [ObservableProperty]
    private string _url = string.Empty;
    [ObservableProperty]
    private AudioQuality _audioQuality = AudioQuality.Lossless;
    [ObservableProperty]
    private int _size = 0;
    [ObservableProperty]
    private string _duration = string.Empty;
}

