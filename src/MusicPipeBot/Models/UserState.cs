using System.ComponentModel.DataAnnotations.Schema;
using AqueductCommon.Models;
using Microsoft.EntityFrameworkCore;

namespace MusicPipeBot.Models;

[Index(nameof(TelegramId), IsUnique = true)]
public class UserState : BaseStoredModel
{
    public required long TelegramId { get; set; }

    public required string ConnectionPhrase { get; set; }

    public required StateName Name { get; set; }

    [Column(TypeName = "jsonb")]
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string? Context { get; set; }
}