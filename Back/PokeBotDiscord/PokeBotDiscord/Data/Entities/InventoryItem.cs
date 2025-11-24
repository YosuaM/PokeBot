namespace PokeBotDiscord.Data.Entities;

public class InventoryItem
{
    public long Id { get; set; }

    public int ItemTypeId { get; set; }
    public ItemType ItemType { get; set; } = null!;

    public int Quantity { get; set; } = 0;

    // FK
    public long PlayerId { get; set; }
    public Player Owner { get; set; } = null!;
}
