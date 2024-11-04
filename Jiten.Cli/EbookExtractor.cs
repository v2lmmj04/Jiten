using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using VersOne.Epub;

namespace Jiten.Cli;

public class EbookExtractor
{
    public async Task<string> ExtractTextFromEbook(string filePath)
    {
        try
        {
            EpubBook book = await EpubReader.ReadBookAsync(filePath);

            StringBuilder extractedText = new();

            // Select only the chapters with key partXXXX.html or p-XXX.html or XXXX.html
            // I'm not sure if all epub files have this format, but it works for now
            Regex regex = new(@"\b(part\d{4}\.x?html|p-\d{3}\.x?html|\d{4}\.x?html)\b");
            
            var filteredChapters = book.ReadingOrder
                                       .Where(chapter => regex.IsMatch(chapter.Key));


            foreach (var chapter in filteredChapters)
            {
                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(chapter.Content);

                var nodes = htmlDocument.DocumentNode.SelectNodes("//text()[not(ancestor::title)]|//ruby");

                if (nodes != null)
                {
                    StringBuilder lineBuilder = new StringBuilder();

                    foreach (HtmlNode node in nodes)
                    {
                        if (node.Name == "ruby")
                        {
                            // Extract only kanji from ruby text
                            var rbNodes = node.SelectNodes(".//rb");
                            if (rbNodes != null)
                            {
                                foreach (var rbNode in rbNodes)
                                {
                                    lineBuilder.Append(rbNode.InnerText.Trim());
                                }
                            }
                        }
                        else if (node.NodeType == HtmlNodeType.Text &&
                                 !node.ParentNode.Name.Equals("rt", StringComparison.OrdinalIgnoreCase))
                        {
                            string text = node.InnerText.Trim();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                lineBuilder.Append(text);
                            }
                        }

                        // Check if we've reached the end of a sentence or paragraph
                        if (lineBuilder.Length <= 0 ||
                            (!node.InnerText.EndsWith("。") && !node.InnerText.EndsWith("！") && !node.InnerText.EndsWith("？"))) continue;
                        extractedText.AppendLine(lineBuilder.ToString());
                        lineBuilder.Clear();
                    }

                    // Add any remaining text
                    if (lineBuilder.Length > 0)
                    {
                        extractedText.AppendLine(lineBuilder.ToString());
                    }
                }
                else
                {
                    Console.WriteLine("No text or ruby nodes found in the chapter.");
                }
            }

            return extractedText.ToString();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return "";
        }
    }
}