using System.Text.Json;
using CommandLine;
using Jiten.Cli;
using Jiten.Core.Data;
using Jiten.Core.Data.JMDict;

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
        public string ExtractFilePath { get; set; }

        [Option('p', "parse", Required = false, HelpText = "Parse text.")]
        public bool Parse { get; set; }

        [Option('t', "threads", Required = false, HelpText = "Number of threads to use.")]
        public int Threads { get; set; } = 1;

        [Option('s', "script", Required = false, HelpText = "Choose an available extraction script.")]
        public string Script { get; set; }

        [Option('o', "output", Required = false, HelpText = "Output the operation to a file.")]
        public string Output { get; set; }

        [Option('x', "extra", Required = false, HelpText = "Extra arguments for some operations.")]
        public string Extra { get; set; }
        
        [Option('m', "metadata", Required = false, HelpText = "Download metadata for a folder.")]
        public string Metadata { get; set; }
        
        [Option('a', "api", Required = false, HelpText = "API to retrieve metadata from.")]
        public string Api { get; set; }
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

                        if (o.ExtractFilePath != null)
                        {
                            if (o.Script == null)
                            {
                                Console.WriteLine("Please specify an extraction script.");
                                return;
                            }

                            string result = "";
                            switch (o.Script)
                            {
                                case "epub":
                                    var extractor = new EbookExtractor();

                                    // if it's a directory or a single file
                                    if (Directory.Exists(o.ExtractFilePath))
                                    {
                                        var files = Directory.GetFiles(o.ExtractFilePath, "*.*",
                                                                       new EnumerationOptions()
                                                                       {
                                                                           IgnoreInaccessible = true, RecurseSubdirectories = true
                                                                       });

                                        if (o.Verbose)
                                            Console.WriteLine($"Found {files.Length} files to extract.");

                                        var options = new ParallelOptions() { MaxDegreeOfParallelism = o.Threads };

                                        await Parallel.ForEachAsync(files, options, async (file, _) =>
                                        {
                                            await ExtractEpub(file, extractor, o);
                                            if (o.Verbose)
                                            {
                                                Console
                                                    .WriteLine($"Progress: {Array.IndexOf(files, file) + 1}/{files.Length}, {Array.IndexOf(files, file) * 100 / files.Length}%, {watch.ElapsedMilliseconds} ms");
                                            }
                                        });
                                    }
                                    else
                                    {
                                        var file = o.ExtractFilePath;
                                        await ExtractEpub(file, extractor, o);
                                    }

                                    break;
                                case "krkr":
                                    result = await new KiriKiriExtractor().Extract(o.ExtractFilePath, o.Verbose);
                                    if (o.Output != null)
                                    {
                                        await File.WriteAllTextAsync(o.Output, result);
                                    }

                                    break;
                                case "generic":
                                    result = await new GenericExtractor().Extract(o.ExtractFilePath, "SHIFT-JIS", o.Verbose);
                                    if (o.Output != null)
                                    {
                                        await File.WriteAllTextAsync(o.Output, result);
                                    }

                                    break;
                                case "generic-utf8":
                                    result = await new GenericExtractor().Extract(o.ExtractFilePath, "UTF8", o.Verbose);
                                    if (o.Output != null)
                                    {
                                        await File.WriteAllTextAsync(o.Output, result);
                                    }

                                    break;
                                case "psb":
                                    result = await new PsbExtractor().Extract(o.ExtractFilePath, o.Verbose);
                                    if (o.Output != null)
                                    {
                                        await File.WriteAllTextAsync(o.Output, result);
                                    }

                                    break;
                                case "msc":
                                    result = await new MscExtractor().Extract(o.ExtractFilePath, o.Verbose);
                                    if (o.Output != null)
                                    {
                                        await File.WriteAllTextAsync(o.Output, result);
                                    }

                                    break;
                                case "bgi":
                                    if (o.Extra == null)
                                    {
                                        Console.WriteLine("Please specify a filter file for BGI extraction with the -x option.");
                                        return;
                                    }

                                    result = await new BgiExtractor().Extract(o.ExtractFilePath, o.Extra, o.Verbose);
                                    if (o.Output != null)
                                    {
                                        await File.WriteAllTextAsync(o.Output, result);
                                    }

                                    break;
                            }
                        }

                        if (o.Metadata != null)
                        {
                            if (string.IsNullOrEmpty(o.Api))
                            {
                                Console.WriteLine("Please specify an API to retrieve metadata from.");
                                return;
                            }
                            
                            await MetadataDownloader.DownloadMetadata(o.Metadata, o.Api);
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
        if (extension is ".epub" or ".txt")
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
                // await File.WriteAllTextAsync("parsed.json", JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            
                if (o.Verbose)
                    Console.WriteLine($"Text extracted from {file}. Found {result.CharacterCount} characters, {result.WordCount} words, {result.UniqueWordCount} unique words.");
            
                result.OriginalTitle = Path.GetFileNameWithoutExtension(file);
                //await JmDictHelper.InsertDeck(result);
            
                if (o.Verbose)
                    Console.WriteLine($"Deck {result.OriginalTitle} inserted into the database.");
            }
        }
    }
}