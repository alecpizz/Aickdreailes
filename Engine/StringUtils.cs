using System.Text.RegularExpressions;

namespace Engine;

public static class StringUtils
{
    public static string GetPretty(string str)
    {
        if (string.IsNullOrEmpty(str)) return string.Empty;

        string result = str;
        result = result.TrimStart('_').TrimEnd('_');
        string[] words = Regex.Split(result, @"(?<=[a-z])(?=[A-Z])");
        for (int i = 0; i < words.Length; i++)
        {
            if (!string.IsNullOrEmpty(words[i]))
            {
                words[i] = char.ToUpper(words[i][0]) + words[i][1..].ToLower();
            }
        }

        return string.Join(" ", words);
    }
}