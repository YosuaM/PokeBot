using System.ComponentModel.DataAnnotations;

namespace PokeBotDiscord.Data.Entities;

public class Location
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = String.Empty;

    public int LocationTypeId { get; set; }
    public LocationType LocationType { get; set; }  = null!;
}
