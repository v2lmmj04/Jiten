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

        [Option('e', "extract", Required = false, HelpText = "Extract text from a file or a folder and all its subfolders.")]
        public string Extract { get; set; }

        [Option('p', "parse", Required = false, HelpText = "Parse text.")]
        public bool Parse { get; set; }
    }

    static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<Options>(args)
                    .WithParsedAsync<Options>(async o =>
                    {
                        var watch = System.Diagnostics.Stopwatch.StartNew();

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

                            // if it's a directory or a single file
                            if (Directory.Exists(o.Extract))
                            {
                                var files = Directory.GetFiles(o.Extract, "*.*", SearchOption.AllDirectories);

                                if (o.Verbose)
                                    Console.WriteLine($"Found {files.Length} files to extract.");

                                for (var i = 100; i < files.Length; i++)
                                {
                                    string? file = files[i];
                                    await ExtractEpub(file, extractor, o);
                                    if (o.Verbose)
                                        Console
                                            .WriteLine($"Progress: {i + 1}/{files.Length}, {i * 100 / files.Length}%, {watch.ElapsedMilliseconds} ms");
                                }
                            }
                            else
                            {
                                var file = o.Extract;
                                await ExtractEpub(file, extractor, o);
                            }
                        }

                        if (o.Verbose)
                            Console.WriteLine($"Execution time: {watch.ElapsedMilliseconds} ms");
                    });
    }

    private static async Task ExtractEpub(string file, EbookExtractor extractor, Options o)
    {
        if (o.Verbose)
        {
            Console.WriteLine("=========================================");
            Console.WriteLine($"=== Processing {file} ===");
            Console.WriteLine("=========================================");
        }

        var extension = Path.GetExtension(file).ToLower();
        if (extension == ".epub" || extension == ".txt")
        {
            var text = extension switch
            {
                ".epub" => await extractor.ExtractTextFromEbook(file),
                ".txt" => await File.ReadAllTextAsync(file),
                _ => throw new NotSupportedException($"File extension {extension} not supported")
            };

            if (String.IsNullOrEmpty(text))
            {
                Console.WriteLine("ERROR: TEXT RETURNED EMPTY");
                
                return;
            }
            
            if (o.Parse)
            {
                var result = await Jiten.Parser.Program.ParseText(text);
                // serialize result and write to file
                await File.WriteAllTextAsync("parsed.json",
                                             JsonSerializer.Serialize(result,
                                                                      new JsonSerializerOptions { WriteIndented = true }));

                if (o.Verbose)
                    Console.WriteLine($"Text extracted from {file}. Found {result.CharacterCount} characters, {result.WordCount} words, {result.UniqueWordCount} unique words.");

                result.OriginalTitle = Path.GetFileNameWithoutExtension(file);
                await JmDictHelper.InsertDeck(result);

                if (o.Verbose)
                    Console.WriteLine($"Deck {result.OriginalTitle} inserted into the database.");
            }
        }
    }
}