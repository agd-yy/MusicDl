using Newtonsoft.Json;

namespace MusicDl.Models;

public class AppConfig
{
    [JsonProperty("selectedLimit")]
    public int SelectedLimit { get; set; } = 10;

    [JsonProperty("selectedAudioQuality")]
    public AudioQuality SelectedAudioQuality { get; set; } = AudioQuality.Lossless;

    [JsonProperty("saveDirectoryPath")]
    public string SaveDirectoryPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
}
