
namespace MusicDl.Models;

#region 看戏仔Search

public class KxzSearchResp
{
    public int Code { get; set; }
    public List<SongData> Data { get; set; } = [];
    public string Tips { get; set; } = "";
    public string Time { get; set; } = "";
}

public class SongData
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Fee { get; set; }
    public string PicUrl { get; set; } = "";
    public List<Artist> Artists { get; set; } = [];
    public Album Album { get; set; } = new();
    public string Duration { get; set; } = "";
}

#endregion
