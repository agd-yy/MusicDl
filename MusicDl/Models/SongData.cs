using Newtonsoft.Json;

namespace MusicDl.Models;

public class SongData
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("pic_url")]
    public string PicUrl { get; set; } = "";

    [JsonProperty("fee")]
    public int Fee { get; set; }

    [JsonProperty("duration")]
    public string Duration { get; set; } = "";

    [JsonProperty("artist")]
    public List<Artist> Artists { get; set; } = [];

    [JsonProperty("album")]
    public Album? Album { get; set; }
}