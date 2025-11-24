using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data.Entities;

namespace PokeBotDiscord.Data;

public class PokeBotDbContext : DbContext
{
    public DbSet<GuildSettings> GuildSettings { get; set; } = null!;
    public DbSet<Player> Players { get; set; } = null!;
    public DbSet<PokemonInstance> PokemonInstances { get; set; } = null!;
    public DbSet<InventoryItem> InventoryItems { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<PokemonSpecies> PokemonSpecies { get; set; } = null!;
    public DbSet<LocationType> LocationTypes { get; set; } = null!;
    public DbSet<Gym> Gyms { get; set; } = null!;
    public DbSet<ItemType> ItemTypes { get; set; } = null!;

    public PokeBotDbContext(DbContextOptions<PokeBotDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GuildSettings>(entity =>
        {
            entity.HasKey(e => e.GuildId);
            entity.Property(e => e.GuildId).ValueGeneratedNever();
            entity.Property(e => e.Language).HasMaxLength(5);
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasIndex(e => new { e.GuildId, e.DiscordUserId }).IsUnique();
            entity.HasOne(e => e.CurrentLocation)
                .WithMany()
                .HasForeignKey(e => e.CurrentLocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PokemonInstance>(entity =>
        {
            entity.HasOne(p => p.Owner)
                .WithMany(o => o.Party)
                .HasForeignKey(p => p.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Species)
                .WithMany()
                .HasForeignKey(p => p.PokemonSpeciesId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasOne(i => i.Owner)
                .WithMany(o => o.Inventory)
                .HasForeignKey(i => i.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.ItemType)
                .WithMany()
                .HasForeignKey(i => i.ItemTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LocationType>(entity =>
        {
            entity.HasMany(lt => lt.Locations)
                .WithOne(l => l.LocationType)
                .HasForeignKey(l => l.LocationTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(lt => lt.Gym)
                .WithMany()
                .HasForeignKey(lt => lt.GymId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(modelBuilder);
    }
}
