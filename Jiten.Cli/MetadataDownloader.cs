using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using VndbRecommender.Model;

namespace Jiten.Cli;

public static class MetadataDownloader
{
    private static Func<string, Task<List<Metadata>>> _currentApi;

    public static async Task DownloadMetadata(string directory, string api)
    {
        if (!Directory.Exists(directory))
        {
            Console.WriteLine("Folder does not exist");
            return;
        }

        _currentApi = api.Trim() switch
        {
            "vndb" => VndbApi,
            "api2" => CallApi,
            _ => throw new ArgumentException("Invalid API")
        };

        var directories = Directory.GetDirectories(directory);
        foreach (var dir in directories)
        {
            await ProcessDirectory(dir);
        }
    }

    private static async Task ProcessDirectory(string directoryPath)
    {
        var metadataPath = Path.Combine(directoryPath, "metadata.json");
        if (File.Exists(metadataPath))
        {
            Console.WriteLine($"Skipping {directoryPath} - metadata.json exists");
            return;
        }

        var files = Directory.GetFiles(directoryPath)
                             .Where(f => f.EndsWith(".epub", StringComparison.OrdinalIgnoreCase)
                                         || f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                             .ToList();

        if (!files.Any())
        {
            return;
        }

        var orderedFiles = await GetFileOrder(files);
        if (!orderedFiles.Any())
        {
            return;
        }

        var metadatas = new List<Metadata>();
        foreach (var file in orderedFiles)
        {
            var apiResult = await ProcessFileWithApi(file);
            if (apiResult != null)
            {
                apiResult.FilePath = file;
                metadatas.Add(apiResult);
            }
        }

        if (metadatas.Count == 0)
        {
            return;
        }

        var suggestedOriginalTitle = metadatas.First().OriginalTitle;
        Console.WriteLine($"Enter Original Title (suggested: {suggestedOriginalTitle}):");
        var originalTitle = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(originalTitle))
            originalTitle = suggestedOriginalTitle;

        var suggestedRomajiTitle = metadatas.First().RomajiTitle;
        Console.WriteLine($"Enter Romaji Title (suggested: {suggestedRomajiTitle}):");
        var romajiTitle = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(romajiTitle))
            romajiTitle = suggestedRomajiTitle;

        var suggestedEnglishTitle = metadatas.First().EnglishTitle;
        Console.WriteLine($"Enter English Title (suggested: {suggestedEnglishTitle}):");
        var englishTitle = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(englishTitle))
            englishTitle = suggestedEnglishTitle;
        
        var imageUrl = metadatas.First().Image;
        if (!string.IsNullOrEmpty(imageUrl))
        {
            var coverImagePath = Path.Combine(directoryPath, "cover.jpg");
            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
            await File.WriteAllBytesAsync(coverImagePath, imageBytes);
        }

        // Encoder necessary to not have \uXXXX in the output
        var serializerOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

        if (metadatas.Count > 1)
        {
            var metadata = new Metadata
                           {
                               OriginalTitle = originalTitle,
                               RomajiTitle = romajiTitle,
                               EnglishTitle = englishTitle,
                               Image = metadatas.First().Image,
                               Children = metadatas,
                           };

            await File.WriteAllTextAsync(metadataPath,
                                         JsonSerializer.Serialize(metadata, serializerOptions));
        }
        else
        {
            var metadata = metadatas.First();
            metadata.OriginalTitle = originalTitle;
            metadata.RomajiTitle = romajiTitle;
            metadata.EnglishTitle = englishTitle;

            await File.WriteAllTextAsync(metadataPath,
                                         JsonSerializer.Serialize(metadata, serializerOptions));
        }

        Console.WriteLine("====================================");
    }

    private static async Task<List<string>> GetFileOrder(List<string> files)
    {
        string? input = "";

        var directory = new DirectoryInfo(Path.GetDirectoryName(files.First()) ?? string.Empty);
        Console.WriteLine($"Directory: {directory.Name}");

        Console.WriteLine("Files in directory:");
        for (int i = 0; i < files.Count; i++)
        {
            Console.WriteLine($"{i + 1} - {Path.GetFileName(files[i])}");
        }

        if (files.Count <= 1)
        {
            Console.WriteLine("Only one file found, press enter to proceed or 's' to skip:");
            input = Console.ReadLine()?.Trim().ToLower();
            return input is "s" or "skip" ? [] : files;
        }

        Console.WriteLine("\nEnter file order (comma-separated numbers) or 's' to skip:");
        input = Console.ReadLine()?.Trim().ToLower();

        if (string.IsNullOrEmpty(input) || input == "s" || input == "skip")
            return [];

        var orderedFiles = new List<string>();
        var numbers = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var num in numbers)
        {
            if (int.TryParse(num.Trim(), out int index) && index >= 1 && index <= files.Count)
            {
                orderedFiles.Add(files[index - 1]);
            }
        }

        return orderedFiles;
    }

    private static async Task<Metadata?> ProcessFileWithApi(string filePath)
    {
        while (true)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            Console.WriteLine($"Press enter to query **{fileName}**, press 'q' or 'query' to write a custom query, or type a string to give a custom title and return immediately:");

            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
            {
                input = filePath;
            }
            else if (input.Equals("q", StringComparison.OrdinalIgnoreCase) || input.Equals("query", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Enter new query text:");
                var queryText = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(queryText))
                {
                    filePath = queryText;
                    continue;
                }
            }
            else
            {
                return new Metadata { OriginalTitle = input };
            }

            var results = await _currentApi.Invoke(input); // This function needs to be implemented
            Console.WriteLine($"\nResults for {Path.GetFileName(filePath)}:");

            for (int i = 0; i < results.Count; i++)
            {
                Console.WriteLine($"{i + 1} - {results[i].OriginalTitle} - {results[i].RomajiTitle} - {results[i].EnglishTitle}");
            }

            Console.WriteLine("Choose a number or 'q' to query with different text:");
            input = Console.ReadLine()?.Trim().ToLower();

            if (string.IsNullOrEmpty(input))
                return null;

            if (input is "q" or "query")
            {
                Console.WriteLine("Enter new query text:");
                var queryText = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(queryText))
                {
                    filePath = queryText;
                    continue;
                }
            }

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= results.Count)
            {
                return results[choice - 1];
            }

            Console.WriteLine("Invalid choice");
        }
    }

    // Placeholder for API call
    private static async Task<List<Metadata>> CallApi(string input)
    {
        // Implementation needed
        return new List<Metadata>()
               {
                   new Metadata() { OriginalTitle = "Test 1" },
                   new Metadata() { OriginalTitle = "Test 2" },
                   new Metadata() { OriginalTitle = "Test 3" },
               };
    }

    private static async Task<List<Metadata>> VndbApi(string query)
    {
        List<VndbRequestResult> requestResults = new List<VndbRequestResult>();

        VnDbRequestPageResult? result = new VnDbRequestPageResult();
        var filter = new List<object> { "search", "=", query };

        var requestContent = new StringContent(JsonSerializer.Serialize(new
                                                                        {
                                                                            filters = filter,
                                                                            fields =
                                                                                "id,title,titles{main,official,lang,title,latin},image{url,sexual}, extlinks{label,url, name}",
                                                                            results = 10,
                                                                            page = 1
                                                                        }));
        var http = new HttpClient();
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await http.PostAsync("https://api.vndb.org/kana/vn", requestContent);

        if (response.IsSuccessStatusCode)
        {
            var contentStream = await response.Content.ReadAsStringAsync();


            result = JsonSerializer.Deserialize<VnDbRequestPageResult>(contentStream,
                                                                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            requestResults.AddRange(result.Results);
        }

        List<Metadata> metadatas = [];
        foreach (var requestResult in requestResults)
        {
            var metadata = new Metadata
                           {
                               OriginalTitle = requestResult.Titles.FirstOrDefault(t => t.Lang == "ja")?.Title ?? requestResult.Title,
                               RomajiTitle = requestResult.Titles.FirstOrDefault(t => t.Lang == "ja")?.Latin,
                               EnglishTitle = requestResult.Titles.FirstOrDefault(t => t.Lang == "en")?.Title,
                               Image = requestResult.Image.Url
                           };

            metadatas.Add(metadata);
        }

        return metadatas;
    }
}