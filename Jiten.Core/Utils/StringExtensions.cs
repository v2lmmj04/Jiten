using System.Text;

namespace Jiten.Core.Utils;

public static class StringExtensions
{
    private static readonly Dictionary<char, char> HalfWidthToFullWidth = new()
                                                                          {
                                                                              { '0', '０' },
                                                                              { '1', '１' },
                                                                              { '2', '２' },
                                                                              { '3', '３' },
                                                                              { '4', '４' },
                                                                              { '5', '５' },
                                                                              { '6', '６' },
                                                                              { '7', '７' },
                                                                              { '8', '８' },
                                                                              { '9', '９' }
                                                                          };

    private static readonly Dictionary<char, char> FullWidthToHalfWidth = new()
                                                                          {
                                                                              { '０', '0' },
                                                                              { '１', '1' },
                                                                              { '２', '2' },
                                                                              { '３', '3' },
                                                                              { '４', '4' },
                                                                              { '５', '5' },
                                                                              { '６', '6' },
                                                                              { '７', '7' },
                                                                              { '８', '8' },
                                                                              { '９', '9' }
                                                                          };

    public static string ToFullWidthDigits(this string input)
    {
        var result = new StringBuilder(input.Length);
        foreach (var ch in input)
            result.Append(HalfWidthToFullWidth.GetValueOrDefault(ch, ch));

        return result.ToString();
    }

    public static string ToHalfWidthDigits(this string input)
    {
        var result = new StringBuilder(input.Length);
        foreach (var ch in input)
            result.Append(FullWidthToHalfWidth.GetValueOrDefault(ch, ch));

        return result.ToString();
    }
    
    public static bool IsAsciiOrFullWidthLetter(this string input)
    {
        var c = input[0];
        
        return c is >= 'a' and <= 'z' ||
               c is >= 'A' and <= 'Z' ||
               c is >= 'ａ' and <= 'ｚ' ||
               c is >= 'Ａ' and <= 'Ｚ';
    }
}