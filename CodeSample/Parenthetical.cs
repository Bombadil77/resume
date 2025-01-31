using System.Text;

namespace JohnWessel;

[Flags]
public enum GraphParentheticalOptions
{
	None = 0,

	/// <summary>remove empty parenthetical phrases such as &quot;()&quot;</summary>
	RemoveEmptyEntries = 1
}

partial class Tokenizer
{
	/// <summary>match the first (outermost and left-most) balanced parenthetical phrase in <paramref name="s"/></summary>
	/// <param name="s">string to search</param>
	/// <returns>parenthetical phrase and its position if matched; otherwise (null, -1)</returns>
	/// <remarks>
	/// <para>
	/// This function matches (), [] and {}.
	/// The parenthesis/brackets are included in the result.
	/// Improperly nested brackets are ignored; they do not raise exceptions.
	/// For example, if s = ((abc) then the result is (abc).
	/// For example, if s = (a{b)c} then the result is = (a{b).
	/// </para>
	/// </remarks>
	public static (string? value, int index) MatchParenthetical(string? s)
	{
		// short circuit when s is null
		if (s == null)
			return (null, -1);

		var phrase = new StringBuilder();   // parenthetical phrase
		int index = -1;                     // index of opening bracket
		Stack<char> stack = [];             // stack of brackets

		// iterate over each character in the search string
		for (int i = 0; i < s.Length; ++i)
		{
			// If the stack contains a bracket
			// then we are potentially inside a parenthetical phrase.
			if (stack.Count > 0)
				phrase.Append(s[i]);

			switch (s[i])
			{
				case '(':
				case '[':
				case '{':
					if (stack.Count == 0)
					{
						// This is the first (i.e. outermost) bracket
						// so it begins the parenthetical phrase.
						phrase.Append(s[i]);
						index = i;
					}

					stack.Push(s[i]);
					break;

				case ')':
				case ']':
				case '}':
					// If it were an error to have improperly nested brackets
					// then we would simply pop an opening bracket.
					// If it did not correspond to the closing bracket then we would raise an exception.
					// However, we ignore improperly nested brackets
					// so we need to peek up the whole stack for a matching opening bracket.
					int j = 0;
					foreach (char c in stack)
					{
						++j;
						if ((c == '(' && s[i] == ')') || (c == '[' && s[i] == ']') || (c == '{' && s[i] == '}'))
						{
							// We found a matching opening bracket.
							// Pop through it.
							for (int k = 0; k < j; ++k)
								stack.Pop();

							// If the stack is empty that means we matched a balanced parenthetical phrase.
							if (stack.Count == 0)
								return (phrase.ToString(), index);

							break;
						}
					}
					break;
			}
		}

		if (stack.Count > 0 && s.Length > index + 1)
		{
			// No balanced, closing bracket exists.
			// Continue searching the remainder of the string. 
			var next = MatchParenthetical(s[(index + 1)..]);
			if (next.value != null)
				return (next.value, index + 1 + next.index);
		}

		return (null, -1);
	}

	public static IEnumerable<IToken> GraphParentheticals(string? phrase, GraphParentheticalOptions options = GraphParentheticalOptions.None)
	{
		// short circuit when phrase is null
		if (phrase == null)
			yield break;

		// short circuit when phrase is empty or whitespace
		phrase = phrase.Trim();
		if (phrase == "")
			yield break;

		(string? value, int index) = MatchParenthetical(phrase);
		if (value == null)
		{
			// no parenthetical found
			yield return LeafToken.Raw(phrase);
		}
		else
		{
			if (index > 0)
			{
				// there is text before the parenthetical
				yield return LeafToken.Raw(phrase[..index]);
			}

			var parenthetical = new BranchToken(BranchTokenKind.Parenthetical);

			// value includes the brackets
			// if length = 2 then the value is only brackets
			if (value.Length > 2)
				parenthetical.Children.AddRange(GraphParentheticals(value[1..^1], options));

			if (parenthetical.Children.Count > 0 || !options.HasFlag(GraphParentheticalOptions.RemoveEmptyEntries))
				yield return parenthetical;

			if (phrase.Length > index + value.Length)
			{
				// there is text after the parenthetical
				foreach (var follower in GraphParentheticals(phrase[(index + value.Length)..], options))
					yield return follower;
			}
		}
	}
}