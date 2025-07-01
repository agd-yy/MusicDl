using MusicDl.Attributes;

namespace MusicDl.Models;

public enum ApiProvider
{
    [TupleDesc("看戏仔", "api.kxzjoker.cn/api/163_music")]
    Kxz,

    [TupleDesc("BugPk", "api.bugpk.com/api/163_music")]
    BugPk,
}
