using System.ComponentModel.DataAnnotations;

namespace PokeBotDiscord.Data.Entities;

public class ItemType
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = String.Empty;

    [MaxLength(50)]
    public string IconCode { get; set; } = string.Empty;

}
