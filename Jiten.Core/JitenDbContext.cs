using JapaneseParser.DictionaryTools;
using Microsoft.Extensions.Configuration;

namespace Jiten.Core;

using Microsoft.EntityFrameworkCore;

public class JitenDbContext : DbContext
{
    public DbSet<Deck> Decks { get; set; }
    public DbSet<DeckWord> DeckWords { get; set; }

    public DbSet<JmDictWord> JMDictWords { get; set; }
    public DbSet<JmDictDefinition> Definitions { get; set; }
    public DbSet<JmDictLookup> Lookups { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var configuration = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json")
                            .Build();

        var connectionString = configuration.GetConnectionString("JitenDatabase");
        optionsBuilder.UseNpgsql(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("jiten"); // Set a default schema

        // Deck entity configuration
        modelBuilder.Entity<Deck>(entity =>
        {
            entity.Property(d => d.Id)
                  .ValueGeneratedOnAdd();

            entity.Property(d => d.ParentDeckId)
                  .HasDefaultValue(0);

            entity.Property(d => d.OriginalTitle)
                  .HasMaxLength(100);

            entity.Property(d => d.RomajiTitle)
                  .HasMaxLength(100);

            entity.Property(d => d.EnglishTitle)
                  .HasMaxLength(100);
        });


        // DeckWord entity configuration
        modelBuilder.Entity<DeckWord>(entity =>
        {
            entity.HasKey(dw => new
                                {
                                    dw.DeckId,
                                    dw.WordId,
                                    dw.ReadingType,
                                    dw.ReadingIndex
                                });

            entity.HasIndex(dw => new { dw.WordId, dw.ReadingType, dw.ReadingIndex })
                  .HasDatabaseName("IX_WordReadingIndex");

            entity.HasIndex(dw => dw.DeckId)
                  .HasDatabaseName("IX_DeckId");
            
            entity.HasOne(dw => dw.Deck)
                  .WithMany(d => d.DeckWords)
                  .HasForeignKey(dw => dw.DeckId);
        });


        modelBuilder.Entity<JmDictWord>(entity =>
        {
            entity.ToTable("Words", "jmdict");
            entity.HasKey(e => e.WordId);
            entity.Property(e => e.WordId).ValueGeneratedNever();
            entity.HasMany(e => e.Definitions)
                  .WithOne()
                  .HasForeignKey(d => d.WordId);
            entity.HasMany(e => e.Lookups)
                  .WithOne()
                  .HasForeignKey(l => l.WordId);
        });

        modelBuilder.Entity<JmDictDefinition>(entity =>
        {
            entity.ToTable("Definitions", "jmdict");
            entity.HasKey(e => e.DefinitionId);
            entity.Property(e => e.DefinitionId).ValueGeneratedOnAdd();
            entity.Property(e => e.WordId).IsRequired();
        });

        modelBuilder.Entity<JmDictLookup>(entity =>
        {
            entity.ToTable("Lookups", "jmdict");
            entity.HasKey(e => new { EntrySequenceId = e.WordId, e.LookupKey });
            entity.Property(e => e.WordId).IsRequired();
            entity.Property(e => e.LookupKey).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}