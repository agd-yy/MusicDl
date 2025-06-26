using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace MusicDl.Models;

public partial class Artist : ObservableObject
{
    [ObservableProperty]
    [property: JsonProperty("id")]
    private long _id;

    [ObservableProperty]
    [property: JsonProperty("name")]
    private string _name = "";
}
