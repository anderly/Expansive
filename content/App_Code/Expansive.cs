using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;

#region : TokenStyle Enum :

public enum TokenStyle
{
	MvcRoute = 1,
	Razor = 2,
	NAnt = 3,
	MSBuild = 4,
}

#endregion

public static class Expansive
{
	private static Dictionary<TokenStyle, PatternStyle> _patternStyles;

	#region : Public Properties :
	
	public static bool RequireAllExpansions { get; set; }

	public static Func<string, string> DefaultExpansionFactory { get; set; }

	public static TokenStyle DefaultTokenStyle { get; set; }

	#endregion : Public Properties :

	static Expansive()
	{
		Initialize();
	}

	public static string Expand(this string source)
	{
		return source.Expand(DefaultExpansionFactory);
	}

	public static string Expand(this string source, TokenStyle tokenStyle)
	{
		return source.ExpandInternal(DefaultExpansionFactory, tokenStyle);
	}

	public static string Expand(this string source, params string[] args)
	{
		var output = source;
		var tokens = new List<string>();
		var patternStyle = _patternStyles[DefaultTokenStyle];
		var pattern = new Regex(patternStyle.TokenMatchPattern, RegexOptions.IgnoreCase);
		var calls = new Stack<string>();
		string callingToken = null;

		while (pattern.IsMatch(output))
		{
			foreach (Match match in pattern.Matches(output))
			{
				var token = patternStyle.TokenReplaceFilter(match.Value);
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
				output = Regex.Replace(output, patternStyle.OutputFilter(match.Value), "{" + tokenIndex + "}");
			}
		}
		var newArgs = new List<string>();
		foreach (var arg in args)
		{
			var newArg = arg;
			var tokenPattern = new Regex(patternStyle.TokenFilter(String.Join("|", tokens)));
			while (tokenPattern.IsMatch(newArg))
			{
				foreach (Match match in tokenPattern.Matches(newArg))
				{
					var token = patternStyle.TokenReplaceFilter(match.Value);
					if (calls.Contains(string.Format("{0}:{1}", callingToken, token))) throw new CircularReferenceException(string.Format("Circular Reference Detected for token '{0}'.", callingToken));
					calls.Push(string.Format("{0}:{1}", callingToken, token));
					callingToken = token;
					newArg = Regex.Replace(newArg, patternStyle.OutputFilter(match.Value), args[tokens.IndexOf(token)]);
				}

			}
			newArgs.Add(newArg);
		}
		return string.Format(output, newArgs.ToArray());
	}

	public static string Expand(this string source, Func<string, string> expansionFactory)
	{
		return source.ExpandInternal(expansionFactory, DefaultTokenStyle);
	}

	public static string Expand(this string source, Func<string, string> expansionFactory, TokenStyle tokenStyle)
	{
		return source.ExpandInternal(expansionFactory, tokenStyle);
	}

	public static string Expand(this string source, object model)
	{
		return source.Expand(model, DefaultTokenStyle);
	}

	public static string Expand(this string source, params object[] models)
	{
		var mergedModel = new ExpandoObject().ToDictionary();
		models.ToList().ForEach(m =>
			{
				var md = m.ToDictionary();
				var keys = md.Keys;
				keys.ToList().ForEach(k =>
					{
						if (!mergedModel.ContainsKey(k)) {
							mergedModel.Add(k, md[k]);
						}
					});
			});
		return source.Expand(mergedModel as ExpandoObject);
	}

	public static string Expand(this string source, object model, TokenStyle tokenStyle)
	{
		return source.ExpandInternal(
				name =>
				{
					IDictionary<string, object> modelDict = model.ToDictionary();
					if (RequireAllExpansions && !modelDict.ContainsKey(name))
					{
						return "";
					}

					if (modelDict[name] == null)
					{
						return "";
					}

					return modelDict[name].ToString();
				}
				, tokenStyle);
	}

	#region : Private Helper Methods :

	private static void Initialize()
	{
		DefaultExpansionFactory = name => ConfigurationManager.AppSettings[name];
		DefaultTokenStyle = TokenStyle.MvcRoute;
		_patternStyles = new Dictionary<TokenStyle, PatternStyle>
		                	{
		                		{
		                			TokenStyle.MvcRoute, new PatternStyle
		                			                     	{
		                			                     		TokenMatchPattern = @"\{[a-zA-Z]\w*\}",
		                			                     		TokenReplaceFilter = token => token.Replace("{", "").Replace("}", ""),
		                			                     		OutputFilter = output => (output.StartsWith("{") && output.EndsWith("}") ? output : @"\{" + output + @"\}"),
																TokenFilter = tokens => "{(" + tokens + ")}"
		                			                     	}
		                			}
		                		,
		                		{
		                			TokenStyle.Razor, new PatternStyle
		                			                  	{
		                			                  		TokenMatchPattern = @"@([a-zA-Z]\w*|\([a-zA-Z]\w*\))",
		                			                  		TokenReplaceFilter = token => token.Replace("@", "").Replace("(", "").Replace(")", ""),
		                			                  		OutputFilter = output => (output.StartsWith("@") ? output.Replace("(", @"\(").Replace(")",@"\)") : "@" + output.Replace("(", @"\(").Replace(")",@"\)")),
															TokenFilter = tokens => @"@(" + tokens + @"|\(" + tokens + @"\))"
		                			                  	}
		                			}
		                		,
		                		{
		                			TokenStyle.NAnt, new PatternStyle
		                			                     	{
		                			                     		TokenMatchPattern = @"\$\{[a-zA-Z]\w*\}",
		                			                     		TokenReplaceFilter = token => token.Replace("${", "").Replace("}", ""),
		                			                     		OutputFilter = output => (output.StartsWith("${") && output.EndsWith("}") ? output.Replace("$",@"\$").Replace("{",@"\{").Replace("}",@"\}") : @"\$\{" + output + @"\}"),
																TokenFilter = tokens => @"\$\{(" + tokens + @")\}"
		                			                     	}
		                			}
		                		,
		                		{
		                			TokenStyle.MSBuild, new PatternStyle
		                			                     	{
		                			                     		TokenMatchPattern = @"\$\([a-zA-Z]\w*\)",
		                			                     		TokenReplaceFilter = token => token.Replace("$(", "").Replace(")", ""),
		                			                     		OutputFilter = output => (output.StartsWith("$(") && output.EndsWith(")") ? output.Replace("$",@"\$").Replace("(",@"\(").Replace(")",@"\)") : @"\$\(" + output + @"\)"),
																TokenFilter = tokens => @"\$\((" + tokens + @")\)"
		                			                     	}
		                			}
		                	};
	}

	private static string ExpandInternal(this string source, Func<string, string> expansionFactory, TokenStyle tokenStyle)
	{
		if (expansionFactory == null) throw new ApplicationException("ExpansionFactory not defined.\nDefine a DefaultExpansionFactory or call Expand(source, Func<string, string> expansionFactory))");

		var patternStyle = _patternStyles[tokenStyle];
		var pattern = new Regex(patternStyle.TokenMatchPattern, RegexOptions.IgnoreCase);

		var callTreeParent = new Tree<string>("root").Root;

		return source.Explode(pattern, patternStyle, expansionFactory, callTreeParent);
	}

	private static string Explode(this string source, Regex pattern, PatternStyle patternStyle, Func<string, string> expansionFactory, TreeNode<string> parent)
	{
		var output = source;
		while (output.HasChildren(pattern))
		{
			foreach (Match match in pattern.Matches(source))
			{
				var child = match.Value;
				var token = patternStyle.TokenReplaceFilter(match.Value);

				var thisNode = parent.Children.Add(token);

				// if we have already encountered this token in this call tree, we have a circular reference
				if (thisNode.CallTree.Contains(token))
					throw new CircularReferenceException(string.Format("Circular Reference Detected for token '{0}'. Call Tree: {1}->{2}",
																	   token,
					                                                   String.Join("->", thisNode.CallTree.ToArray().Reverse()), token));

				// expand this match
				var expandedValue = expansionFactory(token);

				// Replace the match with the expanded value
				child = Regex.Replace(child, patternStyle.OutputFilter(match.Value), expandedValue);

				// Recursively expand the child until we no longer encounter nested tokens (or hit a circular reference)
				child = child.Explode(pattern, patternStyle, expansionFactory, thisNode);

				// finally, replace the match in the output with the fully-expanded value
				output = Regex.Replace(output, patternStyle.OutputFilter(match.Value), child);
			}
		}
		return output;
	}

	private static bool HasChildren(this string token, Regex pattern)
	{
		return pattern.IsMatch(token);
	}

	/// <summary>
	/// Turns the object into an ExpandoObject
	/// </summary>
	private static dynamic ToExpando(this object o)
	{
		var result = new ExpandoObject();
		var d = result as IDictionary<string, object>; //work with the Expando as a Dictionary
		if (o is ExpandoObject) return o; //shouldn't have to... but just in case
		if (o is NameValueCollection || o.GetType().IsSubclassOf(typeof(NameValueCollection)))
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

	#endregion
}

public class CircularReferenceException : Exception
{
	public CircularReferenceException(string message)
		: base(message)
	{
	}
}

#region : Internal Members :

	#region : PatternStyle :

	internal class PatternStyle
	{
		public string TokenMatchPattern { get; set; }
		public Func<string, string> TokenFilter { get; set; }
		public Func<string, string> TokenReplaceFilter { get; set; }
		public Func<string, string> OutputFilter { get; set; }
	}

	#endregion

	#region : Tree :

	internal class Tree<T> : TreeNode<T>
	{
		public Tree(T RootValue)
			: base(RootValue)
		{
			Value = RootValue;
		}
	}

	#endregion : Tree :

	#region : TreeNode :

	internal class TreeNode<T>
	{
		private TreeNode<T> _Parent;
		public TreeNode<T> Parent
		{
			get { return _Parent; }
			set
			{
				if (value == _Parent)
				{
					return;
				}

				if (_Parent != null)
				{
					_Parent.Children.Remove(this);
				}

				if (value != null && !value.Children.Contains(this))
				{
					value.Children.Add(this);
				}

				_Parent = value;
			}
		}

		public TreeNode<T> Root
		{
			get
			{
				//return (Parent == null) ? this : Parent.Root;

				TreeNode<T> node = this;
				while (node.Parent != null)
				{
					node = node.Parent;
				}
				return node;
			}
		}

		private TreeNodeList<T> _Children;
		public TreeNodeList<T> Children
		{
			get { return _Children; }
			private set { _Children = value; }
		}

		private List<T> _CallTree;
		public List<T> CallTree
		{
			get
			{
				_CallTree = new List<T>();
				TreeNode<T> node = this;
				while (node.Parent != null)
				{
					node = node.Parent;
					_CallTree.Add(node.Value);
				}
				return _CallTree;
			}
			private set { _CallTree = value; }
		}

		private T _Value;
		public T Value
		{
			get { return _Value; }
			set
			{
				_Value = value;
			}
		}

		public TreeNode(T Value)
		{
			this.Value = Value;
			Parent = null;
			Children = new TreeNodeList<T>(this);
			_CallTree = new List<T>();
		}

		public TreeNode(T Value, TreeNode<T> Parent)
		{
			this.Value = Value;
			this.Parent = Parent;
			Children = new TreeNodeList<T>(this);
			_CallTree = new List<T>();
		}
	}

	#endregion : TreeNode :

	#region : TreeNodeList :

	internal class TreeNodeList<T> : List<TreeNode<T>>
	{
		public TreeNode<T> Parent;

		public TreeNodeList(TreeNode<T> Parent)
		{
			this.Parent = Parent;
		}

		public new TreeNode<T> Add(TreeNode<T> Node)
		{
			base.Add(Node);
			Node.Parent = Parent;
			return Node;
		}

		public TreeNode<T> Add(T Value)
		{
			return Add(new TreeNode<T>(Value));
		}


		public override string ToString()
		{
			return "Count=" + Count.ToString();
		}
	}

	#endregion : TreeNodeList :

#endregion