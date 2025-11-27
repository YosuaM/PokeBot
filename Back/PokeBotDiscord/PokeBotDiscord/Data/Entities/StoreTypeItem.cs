namespace PokeBotDiscord.Data.Entities;

public class StoreTypeItem
{
    public int Id { get; set; }

    public int StoreTypeId { get; set; }
    public StoreType StoreType { get; set; } = null!;

    public int ItemTypeId { get; set; }
    public ItemType ItemType { get; set; } = null!;

    public int Price { get; set; }

    public bool Enabled { get; set; } = true;

    public int SortOrder { get; set; } = 0;
}
