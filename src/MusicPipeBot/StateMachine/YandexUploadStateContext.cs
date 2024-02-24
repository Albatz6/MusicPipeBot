namespace MusicPipeBot.StateMachine;

public class YandexUploadStateContext : StateContext
{
    public required string DownloadId { get; set; }
}