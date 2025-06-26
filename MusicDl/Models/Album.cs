using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicDl.Models;

public partial class Album : ObservableObject
{
    [ObservableProperty]
    private long _id;

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _publishTime = "";
}