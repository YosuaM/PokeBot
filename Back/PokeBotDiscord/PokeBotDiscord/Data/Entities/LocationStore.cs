namespace PokeBotDiscord.Data.Entities;

public class LocationStore
{
    public int Id { get; set; }

    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;

    public int StoreTypeId { get; set; }
    public StoreType StoreType { get; set; } = null!;

    public int SortOrder { get; set; } = 0;
}
