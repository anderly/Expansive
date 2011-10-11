using System;
using System.Collections.Generic;
using System.Configuration;

public static class Expansive
{
    private static Func<string, string> _expansionFactory;
    private static string _startToken = "${";
    private static string _endToken = "}";

    static Expansive()
    {
        _expansionFactory = name => ConfigurationManager.AppSettings[name];
    }

    public static void SetDefaultExpansionFactory(Func<string, string> expansionFactory)
    {
        if (expansionFactory == null) throw new ArgumentOutOfRangeException("expansionFactory", "expansionFactory cannot be null");
        _expansionFactory = expansionFactory;
    }

    public static void SetDefaultTokenDelimiters(string startToken, string endToken)
    {
        _startToken = startToken;
        _endToken = endToken;
    }

    public static string Expand(this string source)
    {
        return Expand(source, _expansionFactory);
    }

    public static string Expand(this string source, string startToken, string endToken)
    {
        return ExpandInternal(source, _expansionFactory, startToken, endToken);
    }

    public static string Expand(this string source, Func<string, string> expansionFactory)
    {
        return ExpandInternal(source, expansionFactory, _startToken, _endToken);
    }

    public static string Expand(this string source, string startToken, string endToken, Func<string, string> expansionFactory)
    {
        return ExpandInternal(source, expansionFactory, startToken, endToken);
    }

    private static string ExpandInternal(string value, Func<string, string> expansionFactory, string startToken, string endToken)
    {
        if (string.IsNullOrWhiteSpace(startToken)) throw new ArgumentOutOfRangeException("startToken", "startToken cannot be null or empty");
        if (string.IsNullOrWhiteSpace(endToken)) throw new ArgumentOutOfRangeException("endToken", "endToken cannot be null or empty");
        if (expansionFactory == null) throw new ApplicationException("ExpansionFactory not defined.\nUse SetDefaultExpansionFactory(Func<string, string> expansionFactory) to define a default ExpansionFactory or call Expand(source, Func<string, string> expansionFactory))");

        var output = value;
        var tokenBeginPosition = output.IndexOf(startToken);
        var calls = new Stack<string>();

        while (tokenBeginPosition != -1)
        {
            var tokenEndPosition = output.IndexOf(endToken, tokenBeginPosition);

            var tempKey = output.Substring(tokenBeginPosition + startToken.Length, (tokenEndPosition - (tokenBeginPosition + startToken.Length)));
            if (calls.Contains(tempKey)) throw new CircularReferenceException("Token '${tempKey}' was encountered more than once.".Expand(name => tempKey));
            calls.Push(tempKey);
            var tempNewKey = expansionFactory(tempKey);
            output = output.Replace(string.Concat(startToken, tempKey, endToken), tempNewKey);
            tokenBeginPosition = output.IndexOf(startToken);
        }
        calls.Clear();

        return output;
    }
}

public class CircularReferenceException : Exception
{
    public CircularReferenceException(string message) : base(message)
    {
    }
}