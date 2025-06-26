using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicDl.Models;

public partial class Artist : ObservableObject
{
    [ObservableProperty]
    private long _id;

    [ObservableProperty]
    private string _name = "";
}
