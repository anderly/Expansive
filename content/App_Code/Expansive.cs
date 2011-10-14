using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Dynamic;
using System.Linq;
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
        var calls = new Stack<string>();
        string callingToken = null;

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
                    if (calls.Contains(string.Format("{0}:{1}", callingToken, token))) throw new CircularReferenceException("Circular Reference Detected for token '{callingToken}'.".Expand(callingToken));
                    calls.Push(string.Format("{0}:{1}", callingToken, token));
                    callingToken = token;
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

    public static string Expand(this string source, object model)
    {
        return ExpandInternal(source, name => model.ToDictionary()[name].ToString(), _startToken, _endToken);
    }

    private static string ExpandInternal(string value, Func<string, string> expansionFactory, string startToken, string endToken)
    {
        if (string.IsNullOrWhiteSpace(startToken)) throw new ArgumentOutOfRangeException("startToken", "startToken cannot be null or empty");
        if (string.IsNullOrWhiteSpace(endToken)) throw new ArgumentOutOfRangeException("endToken", "endToken cannot be null or empty");
        if (expansionFactory == null) throw new ApplicationException("ExpansionFactory not defined.\nUse SetDefaultExpansionFactory(Func<string, string> expansionFactory) to define a default ExpansionFactory or call Expand(source, Func<string, string> expansionFactory))");

        var pattern = new Regex(@"\" + startToken + @"\w+\" + endToken);
        var output = value;
        var calls = new Stack<string>();
        string callingToken = null;

        while (pattern.IsMatch(output))
        {
            foreach (Match match in pattern.Matches(output))
            {
                var token = match.Value.Replace(_startToken, "").Replace(_endToken, "");
                if (calls.Contains(string.Format("{0}:{1}", callingToken, token))) throw new CircularReferenceException("Circular Reference Detected for token '{callingToken}'.".Expand(callingToken));
                calls.Push(string.Format("{0}:{1}", callingToken, token));
                var nextToken = expansionFactory(token);
                output = Regex.Replace(output, _startToken + token + _endToken, nextToken);
                callingToken = token;
            }
        }

        calls.Clear();

        return output;
    }

    /// <summary>
    /// Turns the object into an ExpandoObject
    /// </summary>
    private static dynamic ToExpando(this object o)
    {
        var result = new ExpandoObject();
        var d = result as IDictionary<string, object>; //work with the Expando as a Dictionary
        if (o.GetType() == typeof(ExpandoObject)) return o; //shouldn't have to... but just in case
        if (o.GetType() == typeof(NameValueCollection) || o.GetType().IsSubclassOf(typeof(NameValueCollection)))
        {
            var nv = (NameValueCollection)o;
            nv.Cast<string>().Select(key => new KeyValuePair<string, object>(key, nv[key])).ToList().ForEach(i => d.Add(i));
        }
        else
        {
            var props = o.GetType().GetProperties();
            foreach (var item in props)
            {
                d.Add(item.Name, item.GetValue(o, null));
            }
        }
        return result;
    }
    /// <summary>
    /// Turns the object into a Dictionary
    /// </summary>
    private static IDictionary<string, object> ToDictionary(this object thingy)
    {
        return (IDictionary<string, object>)thingy.ToExpando();
    }
}

public class CircularReferenceException : Exception
{
    public CircularReferenceException(string message)
        : base(message)
    {
    }
}