namespace PokeBotDiscord.Data.Entities;

public class LocationConnection
{
    public int Id { get; set; }

    // From which location the player starts
    public int FromLocationId { get; set; }
    public Location FromLocation { get; set; } = null!;

    // To which location the player can go
    public int ToLocationId { get; set; }
    public Location ToLocation { get; set; } = null!;

    // Optional gym requirement to be able to move along this connection
    public int? RequiredGymId { get; set; }
    public Gym? RequiredGym { get; set; }
}
