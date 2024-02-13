using System.ComponentModel.DataAnnotations;
using AqueductCommon.Models;

namespace MusicPipeBot.Models;

public class User : BaseStoredModel
{
    [MaxLength(25)]
    public string TelegramId { get; set; } = null!;
}