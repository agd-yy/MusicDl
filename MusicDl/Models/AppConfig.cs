using Newtonsoft.Json;

namespace MusicDl.Models;

public class AppConfig
{
    [JsonProperty("selectedLimit")]
    public int SelectedLimit { get; set; } = 10;

    [JsonProperty("selectedAudioQuality")]
    public AudioQuality SelectedAudioQuality { get; set; } = AudioQuality.Lossless;

    [JsonProperty("selectedSaveType")]
    public SaveType SelectedSaveType { get; set; } = SaveType.Hierarchy1;

    [JsonProperty("saveDirectoryPath")]
    public string SaveDirectoryPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
}
