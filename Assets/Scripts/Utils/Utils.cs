using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class Utils
{
    private static readonly Random rng = new();

    public static string Format(string text, params object[] args)
    {
        return string.Format(text, args);
    }

    public static int Range(int v1, int v2)
    {
        return UnityEngine.Random.Range(v1, v2);
    }

    public static float Range(float v1, float v2)
    {
        return UnityEngine.Random.Range(v1, v2);
    }

    public static int WordCount(string word, string text)
    {
        var regex = new Regex(string.Format(@"\b{0}\b", word), RegexOptions.IgnoreCase);
        return regex.Matches(text).Count;
    }

    public static bool IsDigit(string str)
    {
        return str.All(char.IsDigit);
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}