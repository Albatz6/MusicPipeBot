namespace MusicPipeBot.StateMachine.Contexts;

public class YandexUploadStateContext : IStateContext
{
    public required string DownloadId { get; set; }
}