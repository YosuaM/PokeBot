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
    public DbSet<GymTrainer> GymTrainers { get; set; } = null!;
    public DbSet<ItemType> ItemTypes { get; set; } = null!;
    public DbSet<LocationConnection> LocationConnections { get; set; } = null!;
    public DbSet<PlayerGymBadge> PlayerGymBadges { get; set; } = null!;
    public DbSet<PlayerGymTrainerProgress> PlayerGymTrainerProgresses { get; set; } = null!;
    public DbSet<PokemonEncounter> PokemonEncounters { get; set; } = null!;
    public DbSet<StoreType> StoreTypes { get; set; } = null!;
    public DbSet<StoreTypeItem> StoreTypeItems { get; set; } = null!;
    public DbSet<LocationStore> LocationStores { get; set; } = null!;
    public DbSet<PokemonRarity> PokemonRarities { get; set; } = null!;
    public DbSet<PokemonRarityCatchRate> PokemonRarityCatchRates { get; set; } = null!;
    public DbSet<MoveRandomItemReward> MoveRandomItemRewards { get; set; } = null!;

    public DbSet<TutorialStep> TutorialSteps { get; set; } = null!;
    public DbSet<TutorialMission> TutorialMissions { get; set; } = null!;
    public DbSet<PlayerTutorialMissionProgress> PlayerTutorialMissionProgresses { get; set; } = null!;
    public DbSet<PlayerTutorialStepProgress> PlayerTutorialStepProgresses { get; set; } = null!;

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

        modelBuilder.Entity<LocationConnection>(entity =>
        {
            entity.HasOne(lc => lc.FromLocation)
                .WithMany()
                .HasForeignKey(lc => lc.FromLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(lc => lc.ToLocation)
                .WithMany()
                .HasForeignKey(lc => lc.ToLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(lc => lc.RequiredGym)
                .WithMany()
                .HasForeignKey(lc => lc.RequiredGymId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Gym>(entity =>
        {
            entity.Property(g => g.Code).HasMaxLength(50);

            entity.HasOne(g => g.Location)
                .WithMany()
                .HasForeignKey(g => g.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GymTrainer>(entity =>
        {
            entity.Property(gt => gt.Name).HasMaxLength(100);

            entity.HasOne(gt => gt.Gym)
                .WithMany(g => g.Trainers)
                .HasForeignKey(gt => gt.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(gt => gt.RewardItemType)
                .WithMany()
                .HasForeignKey(gt => gt.RewardItemTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StoreType>(entity =>
        {
            entity.Property(st => st.Code).HasMaxLength(50);
            entity.Property(st => st.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<StoreTypeItem>(entity =>
        {
            entity.HasOne(sti => sti.StoreType)
                .WithMany(st => st.Items)
                .HasForeignKey(sti => sti.StoreTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sti => sti.ItemType)
                .WithMany()
                .HasForeignKey(sti => sti.ItemTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(sti => new { sti.StoreTypeId, sti.ItemTypeId }).IsUnique();
        });

        modelBuilder.Entity<LocationStore>(entity =>
        {
            entity.HasOne(ls => ls.Location)
                .WithMany()
                .HasForeignKey(ls => ls.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ls => ls.StoreType)
                .WithMany(st => st.LocationStores)
                .HasForeignKey(ls => ls.StoreTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(ls => new { ls.LocationId, ls.StoreTypeId }).IsUnique();
        });

        modelBuilder.Entity<PokemonEncounter>(entity =>
        {
            entity.HasOne(e => e.Species)
                .WithMany()
                .HasForeignKey(e => e.PokemonSpeciesId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Location)
                .WithMany()
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.EncounterMethodId)
                .HasDefaultValue((int)PokemonEncounterMethod.Normal);
        });

        modelBuilder.Entity<PlayerGymBadge>(entity =>
        {
            entity.HasOne(pgb => pgb.Player)
                .WithMany(p => p.GymBadges)
                .HasForeignKey(pgb => pgb.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pgb => pgb.Gym)
                .WithMany()
                .HasForeignKey(pgb => pgb.GymId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(pgb => new { pgb.PlayerId, pgb.GymId }).IsUnique();
        });

        modelBuilder.Entity<PlayerGymTrainerProgress>(entity =>
        {
            entity.HasOne(p => p.Player)
                .WithMany()
                .HasForeignKey(p => p.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.GymTrainer)
                .WithMany()
                .HasForeignKey(p => p.GymTrainerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(p => new { p.PlayerId, p.GymTrainerId }).IsUnique();
        });

        modelBuilder.Entity<PokemonSpecies>(entity =>
        {
            entity.HasOne(ps => ps.Rarity)
                .WithMany(r => r.Species)
                .HasForeignKey(ps => ps.PokemonRarityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PokemonRarityCatchRate>(entity =>
        {
            entity.HasOne(cr => cr.PokemonRarity)
                .WithMany(r => r.CatchRates)
                .HasForeignKey(cr => cr.PokemonRarityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(cr => new { cr.PokemonRarityId, cr.BallCode }).IsUnique();
        });

        modelBuilder.Entity<TutorialStep>(entity =>
        {
            entity.Property(ts => ts.Code).HasMaxLength(50);
            entity.HasMany(ts => ts.Missions)
                .WithOne(m => m.TutorialStep)
                .HasForeignKey(m => m.TutorialStepId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TutorialMission>(entity =>
        {
            entity.Property(tm => tm.ConditionCode).HasMaxLength(100);
        });

        modelBuilder.Entity<PlayerTutorialMissionProgress>(entity =>
        {
            entity.HasOne(p => p.Player)
                .WithMany()
                .HasForeignKey(p => p.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.TutorialMission)
                .WithMany(m => m.PlayerProgress)
                .HasForeignKey(p => p.TutorialMissionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(p => new { p.PlayerId, p.TutorialMissionId }).IsUnique();
        });

        modelBuilder.Entity<PlayerTutorialStepProgress>(entity =>
        {
            entity.HasOne(p => p.Player)
                .WithMany()
                .HasForeignKey(p => p.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.TutorialStep)
                .WithMany(ts => ts.PlayerProgress)
                .HasForeignKey(p => p.TutorialStepId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(p => new { p.PlayerId, p.TutorialStepId }).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
