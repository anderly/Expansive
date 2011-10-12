using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;

public static class Expansive
{
    private static Func<string, string> _expansionFactory;
    private static string _startToken = "{";
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

    public static string Expand(this string source, params string[] args)
    {
        var output = source;
        var tokens = new List<string>();
        var pattern = new Regex(@"\" + _startToken + "([^" + _endToken + "][^0-9{1,2}]+[^" + _endToken + @"])\" + _endToken, RegexOptions.IgnoreCase);

        while (pattern.IsMatch(output))
        {
            foreach (Match match in pattern.Matches(output))
            {
                var token = match.Value.Replace(_startToken, "").Replace(_endToken, "");
                var tokenIndex = 0;
                if (!tokens.Contains(token))
                {
                    tokens.Add(token);
                    tokenIndex = tokens.Count - 1;
                }
                else
                {
                    tokenIndex = tokens.IndexOf(token);
                }
                output = Regex.Replace(output, _startToken + token + _endToken, "{" + tokenIndex + "}");
            }
        }
        var newArgs = new List<string>();
        foreach (var arg in args)
        {
            var newArg = arg;
            var tokenPattern = new Regex(@"\" + _startToken + String.Join("|", tokens) + @"\" + _endToken);
            while (tokenPattern.IsMatch(newArg))
            {
                foreach (Match match in tokenPattern.Matches(newArg))
                {
                    var token = match.Value.Replace(_startToken, "").Replace(_endToken, "");
                    newArg = Regex.Replace(newArg, _startToken + token + _endToken, args[tokens.IndexOf(token)]);
                }
                
            }
            newArgs.Add(newArg);
        }
        return string.Format(output, newArgs.ToArray());
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
        string callingKey = null;

        while (tokenBeginPosition != -1)
        {
            var tokenEndPosition = output.IndexOf(endToken, tokenBeginPosition);

            var tempKey = output.Substring(tokenBeginPosition + startToken.Length, (tokenEndPosition - (tokenBeginPosition + startToken.Length)));
            if (calls.Contains(string.Format("{0}:{1}", callingKey, tempKey))) throw new CircularReferenceException("Circular Reference Detected for token '{callingKey}'.".Expand(callingKey));
            calls.Push(string.Format("{0}:{1}", callingKey, tempKey));
            var tempNewKey = expansionFactory(tempKey);
            output = output.Replace(string.Concat(startToken, tempKey, endToken), tempNewKey);
            tokenBeginPosition = output.IndexOf(startToken);
            callingKey = tempKey;
        }
        calls.Clear();

        return output;
    }
}

public class CircularReferenceException : Exception
{
    public CircularReferenceException(string message)
        : base(message)
    {
    }
}