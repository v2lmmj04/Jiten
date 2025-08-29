using System.Security.Claims;
using Jiten.Core;
using Jiten.Core.Data.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jiten.Api.Controllers;

[ApiController]
[Route("api/user")]
[Authorize]
public class UserController(JitenDbContext jitenContext, UserDbContext userContext) : ControllerBase
{
    /// <summary>
    /// Add known words for the current user by JMdict word IDs. ReadingIndex defaults to 0.
    /// </summary>
    [HttpPost("vocabulary/import-from-ids")]
    public async Task<IResult> ImportWordsFromIds([FromBody] List<long> wordIds)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
        if (wordIds == null || wordIds.Count == 0) return Results.BadRequest("No word IDs provided");

        var distinctIds = wordIds.Where(id => id > 0).Distinct().ToList();
        if (distinctIds.Count == 0) return Results.BadRequest("No valid words found");

        var jmdictWords = await jitenContext.JMDictWords
                                               .AsNoTracking()
                                               .Where(w => distinctIds.Contains(w.WordId))
                                               .ToListAsync();

        if (jmdictWords.Count == 0) return Results.BadRequest("Invalid words provided");

        var jmdictWordIds = jmdictWords.Select(w => w.WordId).ToList();
        
        var alreadyKnown = await userContext.UserKnownWords
                                            .AsNoTracking()
                                            .Where(uk => uk.UserId == userId && jmdictWordIds.Contains(uk.WordId))
                                            .ToListAsync();

        List<UserKnownWord> toInsert = new();
        
        foreach (var word in jmdictWords)
        {
            for (var i = 0; i < word.Readings.Count; i++)
            {
                if (alreadyKnown.Any(uk => uk.WordId == word.WordId && uk.ReadingIndex == i))
                    continue;
                
                toInsert.Add(new UserKnownWord
                             {
                                 UserId = userId,
                                 WordId = word.WordId,
                                 ReadingIndex = i,
                                 LearnedDate = DateTime.UtcNow,
                                 KnownState = KnownState.Known,
                             });
            }
        }
        
        if (toInsert.Count > 0)
        {
            await userContext.UserKnownWords.AddRangeAsync(toInsert);
            await userContext.SaveChangesAsync();
        }

        return Results.Ok(new { added = toInsert.Count, skipped = alreadyKnown.Count });
    }

    /// <summary>
    /// Parse an Anki-exported TXT file and add all parsed words as known for the current user.
    /// Behavior mirrors VocabularyController.ParseAnkiTxt but persists to DB.
    /// </summary>
    [HttpPost("vocabulary/import-from-anki-txt")]
    [Consumes("multipart/form-data")]
    public async Task<IResult> AddKnownFromAnkiTxt(IFormFile? file)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

        if (file == null || file.Length == 0)
            return Results.BadRequest("File is empty or not provided");

        using var reader = new StreamReader(file.OpenReadStream());
        var lineCount = 0;
        var validWords = new List<string>();

        while (await reader.ReadLineAsync() is { } line)
        {
            lineCount++;
            if (lineCount > 50000)
                return Results.BadRequest("File has more than 50,000 lines");
            if (line.StartsWith("#"))
                continue;

            var tabIndex = line.IndexOf('\t');
            if (tabIndex <= 0)
                tabIndex = line.IndexOf(',');
            if (tabIndex <= 0) continue;

            var word = line.Substring(0, tabIndex);
            if (word.Length <= 25)
                validWords.Add(word);
        }

        if (validWords.Count == 0)
            return Results.BadRequest("No valid words found in file");

        var combinedText = string.Join(Environment.NewLine, validWords);
        var parsedWords = await Parser.Parser.ParseText(jitenContext, combinedText);
        var wordIds = parsedWords.Select(w => w.WordId).Distinct().ToList();
        if (wordIds.Count == 0)
            return Results.BadRequest("No dictionary words could be parsed from file");

        // Insert as readingIndex 0 by default. Avoid duplicates.
        var alreadyKnown = await userContext.UserKnownWords
                                            .AsNoTracking()
                                            .Where(uk => uk.UserId == userId && wordIds.Contains(uk.WordId) && uk.ReadingIndex == 0)
                                            .Select(uk => uk.WordId)
                                            .ToListAsync();

        var toInsert = wordIds.Except(alreadyKnown)
                              .Select(id => new UserKnownWord
                                            {
                                                UserId = userId,
                                                WordId = id,
                                                ReadingIndex = 0,
                                                LearnedDate = DateTime.UtcNow,
                                                KnownState = KnownState.Known
                                            })
                              .ToList();

        if (toInsert.Count > 0)
        {
            await userContext.UserKnownWords.AddRangeAsync(toInsert);
            await userContext.SaveChangesAsync();
        }

        return Results.Ok(new { parsed = wordIds.Count, added = toInsert.Count, skipped = alreadyKnown.Count });
    }
}