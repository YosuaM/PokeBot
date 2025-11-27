using System.ComponentModel.DataAnnotations;

namespace PokeBotDiscord.Data.Entities;

public class StoreType
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public List<StoreTypeItem> Items { get; set; } = new();

    public List<LocationStore> LocationStores { get; set; } = new();
}
