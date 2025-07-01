using System.ComponentModel;

namespace MusicDl.Models;

public enum SaveType
{
    [Description("直接下载")]
    Direct,

    [Description("歌手/专辑")]
    Hierarchy1,
}
