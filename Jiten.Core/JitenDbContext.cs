using System.Text.Json;
using Jiten.Core.Data;
using Jiten.Core.Data.JMDict;
using Microsoft.Extensions.Configuration;

namespace Jiten.Core;

using Microsoft.EntityFrameworkCore;

public class JitenDbContext : DbContext
{
    public DbSet<MediaType> MediaTypes { get; set; }
    public DbSet<Deck> Decks { get; set; }
    public DbSet<DeckWord> DeckWords { get; set; }

    public DbSet<JmDictWord> JMDictWords { get; set; }
    public DbSet<JmDictDefinition> Definitions { get; set; }
    public DbSet<JmDictLookup> Lookups { get; set; }

    public JitenDbContext()
    {
    }

    public JitenDbContext(DbContextOptions<JitenDbContext> options) : base(options)
    {
    }

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

        modelBuilder.Entity<MediaType>().HasData(
                                                 new MediaType { MediaTypeId = 1, Name = "Animes" },
                                                 new MediaType { MediaTypeId = 2, Name = "Dramas" },
                                                 new MediaType { MediaTypeId = 3, Name = "Movies" },
                                                 new MediaType { MediaTypeId = 4, Name = "Novels" },
                                                 new MediaType { MediaTypeId = 5, Name = "Non-fiction" },
                                                 new MediaType { MediaTypeId = 6, Name = "Video games" },
                                                 new MediaType { MediaTypeId = 7, Name = "Visual novels" },
                                                 new MediaType { MediaTypeId = 8, Name = "Web novels" }
                                                );

        modelBuilder.Entity<Deck>(entity =>
        {
            entity.Property(d => d.Id)
                  .ValueGeneratedOnAdd();

            entity.HasOne(d => d.MediaType)
                  .WithMany()
                  .HasForeignKey(d => d.Id);

            entity.Property(d => d.ParentDeckId)
                  .HasDefaultValue(0);

            entity.Property(d => d.OriginalTitle)
                  .HasMaxLength(100);

            entity.Property(d => d.RomajiTitle)
                  .HasMaxLength(100);

            entity.Property(d => d.EnglishTitle)
                  .HasMaxLength(100);

            entity.HasMany(d => d.Links)
                  .WithOne(l => l.Deck)
                  .HasForeignKey(l => l.DeckId);

            entity.HasIndex(d => d.OriginalTitle).HasDatabaseName("IX_OriginalTitle");
            entity.HasIndex(d => d.RomajiTitle).HasDatabaseName("IX_RomajiTitle");
            entity.HasIndex(d => d.EnglishTitle).HasDatabaseName("IX_EnglishTitle");
        });


        modelBuilder.Entity<DeckWord>(entity =>
        {
              entity.Property(d => d.Id)
                    .ValueGeneratedOnAdd();
              
            entity.HasKey(dw => new { dw.Id, });

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

            entity.Property(e => e.Readings)
                  .HasColumnType("text[]");

            entity.Property(e => e.KanaReadings)
                  .HasColumnType("text[]");

            entity.Property(e => e.PartsOfSpeech)
                  .HasColumnType("text[]");
        });

        modelBuilder.Entity<JmDictDefinition>(entity =>
        {
            entity.ToTable("Definitions", "jmdict");
            entity.HasKey(e => e.DefinitionId);
            entity.Property(e => e.DefinitionId).ValueGeneratedOnAdd();
            entity.Property(e => e.WordId).IsRequired();

            entity.Property(e => e.PartsOfSpeech)
                  .HasColumnType("text[]");
            entity.Property(e => e.EnglishMeanings)
                  .HasColumnType("text[]");
            entity.Property(e => e.DutchMeanings)
                  .HasColumnType("text[]");
            entity.Property(e => e.FrenchMeanings)
                  .HasColumnType("text[]");
            entity.Property(e => e.GermanMeanings)
                  .HasColumnType("text[]");
            entity.Property(e => e.SpanishMeanings)
                  .HasColumnType("text[]");
            entity.Property(e => e.HungarianMeanings)
                  .HasColumnType("text[]");
            entity.Property(e => e.RussianMeanings)
                  .HasColumnType("text[]");
            entity.Property(e => e.SlovenianMeanings)
                  .HasColumnType("text[]");
        });

        modelBuilder.Entity<JmDictLookup>(entity =>
        {
            entity.ToTable("Lookups", "jmdict");
            entity.HasKey(e => new { EntrySequenceId = e.WordId, e.LookupKey });
            entity.Property(e => e.WordId).IsRequired();
            entity.Property(e => e.LookupKey).IsRequired();
        });

        modelBuilder.Entity<Link>(entity =>
        {
            entity.HasKey(l => l.LinkId);
            entity.Property(l => l.Url).IsRequired();
            entity.Property(l => l.LinkType).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}