using System.Diagnostics;
using System.Text.RegularExpressions;

namespace JohnWessel;

public static partial class Tokenizer
{
	// non-delimiting punctuation is noise
	private static readonly char[] _noise = ['!', '"', '#', '\'', '*', '?', '^', '`'];

	#region delimiters
	// Delimiters have precedence:
	// 1. parenthetical phrases delimited by balanced pairs of brackets (), [] or {}
	// 2. sub-phrases delimited by sequences of of delimiters
	//    Such a sequence indicates that tokens on either side are unrelated.
	//    For example, contrast:
	//      a. NFL TI-REV
	//      b. NFL TI - REV
	//    In (a) "TI" is related to "REV" but not in (b).
	private static readonly string[] _subPhraseDelimiters = [" & ", " + ", ", ", "- ", "\u2013 ", "\u2014 ", ": ", "; ", "< ", "<=", "=>", "> ", ">="];
	// 3. whitespace
	// 4. other delimiters such as commas or hyphens
	private static readonly char[] _nonWhitespaceDelimiters = ['&', '(', ')', '+', ',', '-', '\u2013', '\u2014', '.', '[', '/', ']', ':', ';', '<', '=', '>', '@', '\\', '_', '{', '|', '}', '~'];
	#endregion

	#region special token regex
	// Certain tokens include delimiters.
	// They must be extracted before splitting on those delimiters.
	// For example, the dollar amount $123.45 is a single token
	// but it would be split at the decimal point into two tokens.
	// We use regular expressions to extract the tokens.

	// W.O.
	[GeneratedRegex(@"(?:\w\.){2,}")]
	private static partial Regex Abbreviation();

	// 12/25/85
	// 12/25/1985
	[GeneratedRegex(@"\d{1,2}/\d{1,2}/\d{2}(?:\d{2})?")]
	private static partial Regex Date();

	// 1234.5
	// 1,234.5
	[GeneratedRegex(@"(?<=^|\s)\-?(?:0|[1-9]\d{0,2}(?:,\d{3})+|\d+)\.\d+(?=\s|$)")]
	private static partial Regex DecimalNumber();

	// $0
	// $0.00
	// $1234
	// $1234.00
	// $1,234
	// $1,234.00
	[GeneratedRegex(@"\$(?:0|[1-9]\d{0,2}(?:,\d{3})+|\d+)(?:\.\d{2})?")]
	private static partial Regex Dollars();

	// 10m
	// 10 m
	// 3in
	// 3 in
	[GeneratedRegex(@"(?<=^|\s)([1-9](?:(?:\d{0,2}(?:,\d{3})*)|\d+))\s?(?!(?:n|s|e|w)(?=\s))([a-z]{1,2})(?=\s|$)")]
	private static partial Regex NumberWithUnits();

	// 16 x 20
	[GeneratedRegex(@"(\d+)\sx\s(\d+)")]
	private static partial Regex NxN();

	[Flags]
	enum SpecialTokens
	{
		None = 0,
		Abbreviation = 1,
		Date = 2,
		DecimalNumber = 4,
		Dollars = 8,
		NumberWithUnits = 16,
		NxN = 32
	}

	// w/onions
	[GeneratedRegex(@"(?<=^|\s)w/")]
	private static partial Regex ShorthandWith();

	#endregion

	/// <summary>tokenize a phrase for search</summary>
	/// <param name="phrase">phrase to tokenize</param>
	/// <param name="stopWords">optional words to suppress</param>
	/// <returns>array of token values</returns>
	public static string[] Tokenize(string? phrase, IReadOnlySet<string>? stopWords = null)
	{
		phrase = phrase?
			.Trim()
			.ToLowerInvariant();

		// short circuit if phrase is null, empty or whitespace
		if (String.IsNullOrEmpty(phrase))
			return [];

		// remove noisy characters
		string quietedPhrase = phrase
			.Split(_noise)
			.StringJoin();

		IEnumerable<LeafToken> ParseLeaves(string raw, SpecialTokens skip = SpecialTokens.None)
		{
			// ParseLeaves is called by Parse.
			// Parse should have found all branches.

			#region special token extraction
			// We extract special tokens before proceeding to the general splitting algorithm.
			// The algorithm is:
			//   1. match the first special token
			//   2. search the substring before the match for other kinds of special tokens
			//   3. search the substring after the match for all kinds of special tokens
			IEnumerable<LeafToken> ExtractSpecial(LeafToken special, Match match)
			{
				if (match.Index > 0)
				{
					// parse substring before the special token
					foreach (var token in ParseLeaves(raw[..match.Index], skip))
						yield return token;
				}

				yield return special;

				if (raw.Length > match.Index + match.Length)
				{
					// parse substring after the special token
					foreach (var token in ParseLeaves(raw[(match.Index + match.Length)..]))
						yield return token;
				}
			}

			// extract abbreviations
			if (!skip.HasFlag(SpecialTokens.Abbreviation))
			{
				skip |= SpecialTokens.Abbreviation;
				Match match = Abbreviation().Match(raw);
				if (match.Success)
				{
					var special = LeafToken.Literal(match.Value.Split('.').StringJoin());
					foreach (var token in ExtractSpecial(special, match)) yield return token;
					yield break;
				}
			}

			// extract dates
			if (!skip.HasFlag(SpecialTokens.Date))
			{
				skip |= SpecialTokens.Date;
				Match match = Date().Match(raw);
				if (match.Success)
				{
					var special = LeafToken.Date(match.Value);
					foreach (var token in ExtractSpecial(special, match)) yield return token;
					yield break;
				}
			}

			// extract dollar amounts
			if (!skip.HasFlag(SpecialTokens.Dollars))
			{
				skip |= SpecialTokens.Dollars;
				Match match = Dollars().Match(raw);
				if (match.Success)
				{
					var special = LeafToken.Dollars(match.Value);
					foreach (var token in ExtractSpecial(special, match)) yield return token;
					yield break;
				}
			}

			// extract dimensions
			if (!skip.HasFlag(SpecialTokens.NxN))
			{
				skip |= SpecialTokens.NxN;
				Match match = NxN().Match(raw);
				if (match.Success)
				{
					var special = LeafToken.Dimensions($"{match.Groups[1].Value}x{match.Groups[2].Value}");
					foreach (var token in ExtractSpecial(special, match)) yield return token;
					yield break;
				}
			}

			// extract numbers with units
			if (!skip.HasFlag(SpecialTokens.NumberWithUnits))
			{
				skip |= SpecialTokens.NumberWithUnits;
				Match match = NumberWithUnits().Match(raw);
				if (match.Success)
				{
					var special = LeafToken.Number($"{match.Groups[1].Value}{match.Groups[2]}");
					foreach (var token in ExtractSpecial(special, match)) yield return token;
					yield break;
				}
			}

			// extract decimal numbers
			if (!skip.HasFlag(SpecialTokens.DecimalNumber))
			{
				skip |= SpecialTokens.DecimalNumber;
				Match match = DecimalNumber().Match(raw);
				if (match.Success)
				{
					var special = LeafToken.Number(match.Value);
					foreach (var token in ExtractSpecial(special, match)) yield return token;
					yield break;
				}
			}
			#endregion

			// Here is the general splitting algorithm.
			IEnumerable<string> values = raw
				// 1. split on whitespace
				.Split(['\t', ' '], StringSplitOptions.RemoveEmptyEntries)
				// We call strings delimited by whitespace "words"
				// because that is how ordinary English words are delimited.
				.SelectMany(word =>
				{
					// 2. split each word on other delimiters
					string[] parts = word.Split(_nonWhitespaceDelimiters, StringSplitOptions.RemoveEmptyEntries);

					if (parts.Length >= 2 && parts[^1].Length <= 2)
					{
						// 3. The last part of the word is short.
						//    Append it to the next-to-last part.
						//    E.g., "at&t" becomes the token "at-t" instead of the tokens "at" "t".
						//    Also, non-whitespace delimiters are normalized as hyphens.
						parts[^2] += $"-{parts[^1]}";
						return parts[..^1];
					}
					else
					{
						return parts;
					}
				});

			if (stopWords != null)
			{
				// Removing stop words late vs. early in the splitting process
				// is a type I/II error trade-off:
				//   Type I (false positive) late removal: removing a valid token
				//   Type II (false negative) early removal: keeping a stop word
				//
				// Early means removal before splitting on non-whitespace delimiters.
				// Late means removal after.
				// 
				// Consider the user input "AT&T".
				// If we remove early then the result is "at" "t".
				// If we remove late then the result is "t" because "at" is a stop word.
				// We would have liked to remove early.
				// (point of fact: "AT&T" results in "at-t" because of step (3) in the splitting algorithm.)
				//
				// Consider the user input "Grand Ave-NEW PLAN".
				// If we remove early then the result is "grand" "ave" "new" "plan".
				// If we remove late then the result is "grand" "ave" "plan" because "new" is a stop word.
				// We would have liked to remove late.
				values = values
					.Where(value => !stopWords.Contains(value));
			}

			foreach (var value in values)
				yield return LeafToken.Literal(value);
		}

		IEnumerable<IToken> Parse(IToken token)
		{
			// Parse is called after GraphParentheticals.
			// GraphParentheticals yields only Parenthetical and Raw tokens.

			if (token is LeafToken leaf)
			{
				Debug.Assert(leaf.Kind == LeafTokenKind.Raw);

				string raw = leaf.Value;

				// strip shorthand
				raw = ShorthandWith().Replace(raw, "");

				// split raw into sub-phrases
				string[] subPhrases = raw.Split(_subPhraseDelimiters, StringSplitOptions.None);

				// parse each sub-phrase into tokens
				var branches = subPhrases
					.Select(subPhrase => new BranchToken(
						BranchTokenKind.SubPhrase,
						ParseLeaves(subPhrase).Cast<IToken>()
					))
					// Parsing may not have yielded any significant tokens.
					// For example, all tokens may have been stop words.
					.Where(branch => branch.Children.Count > 0)
					.ToList();

				if (branches.Count == 1)
				{
					// Sub-phrases only have meaning in relationship to each other.
					// There is only 1 sub-phrase which is equivalent to none
					// so just yield the leaves.
					foreach (var child in branches[0].Children)
						yield return child;
				}
				else
				{
					foreach (var branch in branches)
						yield return branch;
				}
			}
			else if (token is BranchToken branch)
			{
				Debug.Assert(branch.Kind == BranchTokenKind.Parenthetical);

				yield return new BranchToken(
					branch.Kind,
					branch.Children.SelectMany(Parse)
				);
			}
		}

		// What do we do about short words?
		// MySQL full text search, for example, discards any word shorter than 3 characters.
		// That is appropriate for indexing books, articles, etc.
		// However, discarding does not work for us because:
		//   1. source material is scarce, i.e. few words to spare
		//   2. many of our words are short abbreviations and codes
		//
		// Instead, we try to merge short words with adjacent words.
		//
		// Consider the address "123 e grand".
		// The short word is 'e'.
		// We avoid tokenizing 'e' because it is too short and therefore would match too often.
		// For example, if we tokenize 'e' then a search for "e grand" would match "e main".
		// However, we avoid discarding 'e' completely because then a search for "e grand" would return "w grand" addresses.
		// What we do is create 3 tokens "123 e", "e grand" and "grand".
		//
		// Appending a short word to the token on its left is cheap,
		//   e.g. "123 e" is no worse than "123" when the user searches "123"
		//   yet filters better when the user searches "123 e".
		// Prepending a short word to the token on its right
		//   is expensive to index because it's an additional token.
		//   We can't index "e grand" instead of "grand" because a search for "grand" won't match "e grand".
		//   So we have to index "e grand" in addition to "grand".
		//   The payoff is better filtering. A search for "e grand" won't match "w grand" addresses.
		// So we wish to append most of the time and prepend only when useful.
		// If we can infer that a short word is not related to the token on its right then we can avoid prepending.
		// That's where identifying parenthetical phrases and sub-phrases is useful.
		IEnumerable<LeafToken> DFSAndMerge(IEnumerable<IToken> tokens)
		{
			// We will become one with Turing, by which I mean imagine our words as a tape.
			// The words are the leaves of the tree so we perform a depth first search
			// to read them as a tape (a,b,c,d,e).
			//
			// a () d e
			//   /\
			//   b c
			//
			// To make a decision on merging the current token, we must examine the adjacent tokens.
			// We read 3 tokens on the tape.
			LeafToken? prev = null;
			LeafToken? current = null;
			LeafToken? next = null;

			// Whether to exhaust or splice the tape for the next branch
			// depends on the absolute position of the token in the whole tree.
			int leafIndex = 0;

			// We advance the tape one token at at time.
			void AdvanceTape(LeafToken? leaf)
			{
				prev = current;
				current = next;
				next = leaf;
			}

			IEnumerable<LeafToken> Merge()
			{
				if (prev.HasValue)
				{
					// The previous position is the last opportunity to yield a token before the tape passes the reader.
					// We wait to yield a token until it is in the previous position
					// because we may mutate or discard it depending on the current token.
					if (prev.Value.Kind == LeafTokenKind.Literal && current.HasValue && current.Value.Kind == LeafTokenKind.Literal)
					{
						// Current and Previous both exist and are literals.
						if (current.Value.Value.Length < 3)
						{
							// Current is short so append it to Previous.
							yield return LeafToken.Literal($"{prev.Value.Value} {current.Value.Value}");

							// If we leave Current on the tape
							// then, when the tape advances, it will be prepended to Next.
							// However, if Next does not exist then Current will stand alone.
							// That would be incorrect; short tokens should not stand alone.
							// Therefore we must examine Next now
							// and possibly clear Current from the tape.
							if (next == null || next.Value.Kind != LeafTokenKind.Literal)
								current = null;
						}
						else if (prev.Value.Value.Length < 3)
						{
							// Previous is short so prepend it to Current.
							yield return LeafToken.Literal($"{prev.Value.Value} {current.Value.Value}");
						}
						else
						{
							// Both Previous and Current may stand alone so yield Previous as is.
							yield return prev.Value;
						}
					}
					else
					{
						// We did not append Current to Previous so yield Previous as is.
						yield return prev.Value;
					}
				}
			}

			// Merge behavior for tokens at the boundaries of the tape differs from tokens in the body of the tape.
			// The first token can't be appended and the last token can't be prepended.
			// When we want that behavior for a branch, we feed it as its own tape,
			// allowing the tape to exhaust, before starting a new tape for the next branch.
			IEnumerable<LeafToken> ExhaustTape()
			{
				AdvanceTape(null);
				foreach (var token in Merge())
					yield return token;

				AdvanceTape(null);
				foreach (var token in Merge())
					yield return token;
			}

			IEnumerable<LeafToken> DepthFirstSearch(IEnumerable<IToken> tokens)
			{
				foreach (var token in tokens)
				{
					if (token is BranchToken branch)
					{
						Debug.Assert(branch.Children.Count > 0);

						// If a parenthetical contains a single character then we handle it as a leaf.
						// For example, users have input '[P]' as a symbol for 'property'.
						if (branch.Kind == BranchTokenKind.Parenthetical && branch.Children.Count == 1 && branch.Children[0] is LeafToken symbol && symbol.Kind == LeafTokenKind.Literal && symbol.Value.Length == 1)
						{
							AdvanceTape(LeafToken.Literal($"({symbol.Value})"));
							foreach (var merged in Merge())
								yield return merged;

							++leafIndex;
						}
						else
						{
							// By default, the depth first search will feed all tokens on a single tape.
							// That would defeat the purpose of graphing the branches in the first place.
							// If the current branch can stand alone
							// then we feed it as a new tape,
							// which is equivalent to exhausting the current tape.

							// A branch can stand alone when:
							//   1. it has two or more children (because if they are both short they will be merged)
							//   2. or its sole child is another branch (which must be a nested parenthetical)
							//   3. or its sole child is leaf that can stand alone

							// There are two edge cases near the absolute first leaf:
							// 1. If this branch contains the absolute first leaf (leafIndex=0)
							//    then there is nothing on the tape to exhaust.
							if (leafIndex != 0)
							{
								// 2. If this branch contains the absolute second leaf (leafIndex = 1)
								//    and the absolute first leaf can't stand alone
								//    then the first leaf must splice into this branch.
								if (!(leafIndex == 1 && next!.Value.Value.Length < 3))
								{
									// when i=0 there is no tape to exhaust
									// when i=1 this branch immediately follows the first leaf
									// it doesn't much matter if it was inside a branch or not
									if (branch.Children.Count >= 2 || branch.Children[0] is BranchToken || (branch.Children[0] is LeafToken leaf && leaf.Value.Length >= 3))
									{
										// note that if this is the first branch then there is no tape to exhaust

										foreach (var t in ExhaustTape())
											yield return t;
									}
								}
							}

							foreach (var child in DepthFirstSearch(branch.Children))
								yield return child;
						}
					}
					else if (token is LeafToken leaf)
					{
						AdvanceTape(leaf);
						foreach (var merged in Merge())
							yield return merged;

						// We will need to know whether the first leaf has been fed.
						++leafIndex;
					}
				}
			}

			foreach (var token in DepthFirstSearch(tokens))
				yield return token;

			foreach (var token in ExhaustTape())
				yield return token;
		}

		var graph = GraphParentheticals(quietedPhrase, GraphParentheticalOptions.RemoveEmptyEntries)
			.SelectMany(Parse);

		var tokens = DFSAndMerge(graph)
			.Select(leaf => leaf.Value)
			.ToList();

		if (tokens.Count == 0)
		{
			// We want to return something if at all possible.
			if (stopWords != null)
			{
				// All tokens may have been stop words.
				// Try again without stop words.
				return Tokenize(phrase, null);
			}
			else
			{
				// All characters may have been noise.
				// Return the noise (but not delimiters).
				string noise = phrase.Split([' ', .. _nonWhitespaceDelimiters]).StringJoin();
				return noise == "" ? [] : [noise];
			}
		}
		else
		{
			if (tokens.Count > 1)
			{
				// Remove redundant tokens.
				// A token is redundant when it is the beginning of some other token.
				for (int i = tokens.Count - 1; i >= 0; --i)
				{
					for (int j = 0; j < tokens.Count; ++j)
					{
						if (i != j && tokens[j].StartsWith(tokens[i]))
						{
							tokens.RemoveAt(i);
							break;
						}
					}
				}
			}

			return [.. tokens];
		}
	}
}