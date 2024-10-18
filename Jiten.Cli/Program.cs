// See https://aka.ms/new-console-template for more information


using System.Text.Json;
using CommandLine;
using Jiten.Cli;

public class Program
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('i', "import", Required = false, HelpText = "Import the latest JMdict file.")]
        public bool Import { get; set; }

        [Option("xml", Required = false, HelpText = "Path to the JMDict dtd_xml file.")]
        public string XmlPath { get; set; }

        [Option("dic", Required = false, HelpText = "Path to the JMdict dictionary file.")]
        public string DictionaryPath { get; set; }

        [Option('e', "extract", Required = false, HelpText = "Extract text from a file.")]
        public string Extract { get; set; }

        [Option('p', "parse", Required = false, HelpText = "Parse text.")]
        public bool Parse { get; set; }
    }

    static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<Options>(args)
              .WithParsedAsync<Options>(async o =>
              {
                  if (o.Import)
                  {
                      //TODO: auto download them from the latest version

                      if (string.IsNullOrEmpty(o.XmlPath) || string.IsNullOrEmpty(o.DictionaryPath))
                      {
                          Console.WriteLine("For import, you need to specify -xml path/to/jmdict_dtd.xml and -dic path/to/jmdict");
                          return;
                      }

                      Console.WriteLine("Importing JMdict...");
                      await JmDictHelper.Import(o.XmlPath, o.DictionaryPath);
                  }

                  if (o.Extract != null)
                  {
                      var extractor = new EbookExtractor();
                      // get extension then do switch condition file extension epub, txt
                      var extension = Path.GetExtension(o.Extract).ToLower();
                      string text = extension switch
                      {
                          ".epub" => await extractor.ExtractTextFromEbook(o.Extract),
                          ".txt" => await File.ReadAllTextAsync(o.Extract),
                          _ => throw new NotSupportedException($"File extension {extension} not supported")
                      };

                      if (o.Parse)
                      {
                          var result = await Jiten.Parser.Program.ParseText(text);
                          // serialize result and write to file
                            await File.WriteAllTextAsync("parsed.json", JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                          
                      }
                  }
              });
    }
}