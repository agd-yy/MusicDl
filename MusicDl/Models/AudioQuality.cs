
using System.ComponentModel;

namespace MusicDl.Models;

public enum AudioQuality
{
    [Description("标准音质")]
    Standard,

    [Description("极高品质")]
    Exhigh,

    [Description("无损音质")]
    Lossless,

    [Description("Hi-Res音质")]
    Hires,

    [Description("高清环绕声")]
    Jyeffect,

    [Description("沉浸环绕声")]
    Sky,

    [Description("超清母带")]
    Jymaster
}
