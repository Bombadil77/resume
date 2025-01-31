namespace JohnWessel.Tests;

[Trait("Aspect", "Natural Language")]
[Trait("Kind", "Unit")]
public class ParentheticalTest
{
	[Fact]
	public void Null()
	{
		(string? value, int index) = Tokenizer.MatchParenthetical(null);
		Assert.Null(value);
		Assert.Equal(-1, index);
	}

	[Fact]
	public void Empty()
	{
		(string? value, int index) = Tokenizer.MatchParenthetical(null);
		Assert.Null(value);
		Assert.Equal(-1, index);
	}

	[Theory]
	[InlineData("abc", null, -1)]
	[InlineData("(abc)", "(abc)", 0)]
	[InlineData("(abc)(def)", "(abc)", 0)]
	[InlineData("abc(def)ghi", "(def)", 3)]
	[InlineData("(abc(def)ghi)", "(abc(def)ghi)", 0)]
	[InlineData("abc(def(ghi)jkl", "(ghi)", 7)]
	[InlineData("abc)def(ghi)jkl", "(ghi)", 7)]
	[InlineData("abc[def]", "[def]", 3)]
	[InlineData("abc[de{f]", "[de{f]", 3)]
	[InlineData("{abc}def", "{abc}", 0)]
	[InlineData("abc[def(ghi)]jkl", "[def(ghi)]", 3)]
	[InlineData("abc{def(ghi}jkl)", "{def(ghi}", 3)]
	[InlineData("abc{def(ghi]jkl)", "(ghi]jkl)", 7)]
	[InlineData("abc{def[ghi(jkl)lmn]opq}rst", "{def[ghi(jkl)lmn]opq}", 3)]
	public void Parenthesis(string phrase, string? expectedValue, int expectedIndex)
	{
		(string? value, int index) = Tokenizer.MatchParenthetical(phrase);
		Assert.Equal(expectedValue, value);
		Assert.Equal(expectedIndex, index);
	}

	public static TheoryData<string, IEnumerable<IToken>> GraphData
	{
		get
		{
			var data = new TheoryData<string, IEnumerable<IToken>>
			{
				{	// simple
					"abc", [LeafToken.Raw("abc")]
				},
				{	// parenthesis
					"(abc)", [new BranchToken(
						BranchTokenKind.Parenthetical,
						LeafToken.Raw("abc")
					)]
				},
				{	// sequential
					"1(abc)2(def)3", [
						LeafToken.Raw("1"),
						new BranchToken(
							BranchTokenKind.Parenthetical,
							LeafToken.Raw("abc")
						),
						LeafToken.Raw("2"),
						new BranchToken(
							BranchTokenKind.Parenthetical,
							LeafToken.Raw("def")
						),
						LeafToken.Raw("3")
					]
				},
				{	// nested
					"(abc(def))", [
						new BranchToken(
							BranchTokenKind.Parenthetical,
							LeafToken.Raw("abc"),
							new BranchToken(
								BranchTokenKind.Parenthetical,
								LeafToken.Raw("def")
							)
						)
					]
				}
			};

			return data;
		}
	}

	[Theory]
	[MemberData(nameof(GraphData))]
	public void Graph(string phrase, IEnumerable<IToken> expected)
	{
		var graph = Tokenizer.GraphParentheticals(phrase);
		Assert.Equal(expected, graph);
	}

	[Theory]
	[InlineData("()")]
	[InlineData("[()]")]
	public void EmptyParenthetical(string phrase)
	{
		var graph = Tokenizer.GraphParentheticals(phrase, GraphParentheticalOptions.RemoveEmptyEntries);
		Assert.Empty(graph);
	}
}