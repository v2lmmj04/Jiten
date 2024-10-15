using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace JapaneseParser.DictionaryTools;

public class JMDictDbContext : DbContext
{
    public DbSet<DbJMDictWordInfo> JMDictWords { get; set; }
    public DbSet<DbDefinition> Definitions { get; set; }
    public DbSet<DbLookup> Lookups { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var configuration = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json")
                            .Build();

        var connectionString = configuration.GetConnectionString("JMDictDatabase");
        optionsBuilder.UseNpgsql(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbDefinition>()
            .HasOne<DbJMDictWordInfo>()
            .WithMany(j => j.Definitions)
            .HasForeignKey(d => d.EntrySequenceId);
        
        modelBuilder.Entity<DbLookup>()
                    .HasOne<DbJMDictWordInfo>()
                    .WithMany(j => j.Lookups)
                    .HasForeignKey(l => l.EntrySequenceId);
    }
}

[PrimaryKey(nameof(EntrySequenceId))]
public class DbJMDictWordInfo
{
    [Key]
    public int EntrySequenceId { get; set; }

    public List<string> Readings { get; set; }

    public List<string> KanaReadings { get; set; }
    
    public List<string> PartsOfSpeech { get; set; }

    public List<DbDefinition> Definitions { get; set; }
    
    public List<DbLookup> Lookups { get; set; }
    
    public DbJMDictWordInfo()
    {
        Readings = new List<string>();
        KanaReadings = new List<string>();
        Definitions = new List<DbDefinition>();
        Lookups = new List<DbLookup>();
    }
}

[PrimaryKey(nameof(EntrySequenceId), nameof(LookupKey))]
public class DbLookup
{
    [Key, Column(Order = 0)]
    public int EntrySequenceId { get; set; }

    [Key, Column(Order = 1)]
    public string LookupKey { get; set; }
}

[PrimaryKey(nameof(DefinitionId))]
public class DbDefinition
{
    [Key]
    public int DefinitionId { get; set; }
    
    public int EntrySequenceId { get; set; }
    public List<string> PartsOfSpeech { get; set; }
    // public List<string> Meanings { get; set; }

    public List<string> EnglishMeanings { get; set; }
    public List<string> DutchMeanings { get; set; }
    public List<string> FrenchMeanings { get; set; }
    public List<string> GermanMeanings { get; set; }
    public List<string> SpanishMeanings { get; set; }
    public List<string> HungarianMeanings { get; set; }
    public List<string> RussianMeanings { get; set; }
    public List<string> SlovenianMeanings { get; set; }

    public DbDefinition()
    {
        PartsOfSpeech = new List<string>();
        EnglishMeanings = new List<string>();
        DutchMeanings = new List<string>();
        FrenchMeanings = new List<string>();
        GermanMeanings = new List<string>();
        SpanishMeanings = new List<string>();
        HungarianMeanings = new List<string>();
        RussianMeanings = new List<string>();
        SlovenianMeanings = new List<string>();
    }
}