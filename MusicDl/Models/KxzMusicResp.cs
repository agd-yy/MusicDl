using Newtonsoft.Json;


namespace MusicDl.Models;

public class KxzSearchResp
{
    [JsonProperty("status")]
    public int Status { get; set; }

    [JsonProperty("data")]
    public List<SongData>? Data { get; set; }
}

public class KxzMusicResp
{
    [JsonProperty("status")]
    public int Status { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("pic")]
    public string Pic { get; set; } = "";

    [JsonProperty("ar_name")]
    public string ArtistName { get; set; } = "";

    [JsonProperty("al_name")]
    public string AlbumName { get; set; } = "";

    [JsonProperty("level")]
    public string Level { get; set; } = "";

    [JsonProperty("size")]
    public string Size { get; set; } = "";

    [JsonProperty("url")]
    public string Url { get; set; } = "";

    [JsonProperty("lyric")]
    public string Lyric { get; set; } = "";

    [JsonProperty("tlyric")]
    public string TranslatedLyric { get; set; } = "";
}