using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data.Entities;

namespace PokeBotDiscord.Data;

public class PokeBotDbContext : DbContext
{
    public DbSet<GuildSettings> GuildSettings { get; set; } = null!;

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

        base.OnModelCreating(modelBuilder);
    }
}
