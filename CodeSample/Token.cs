namespace JohnWessel;

/// <summary>marker interface for natural language tokens</summary>
public interface IToken
{
}

/// <summary>token semantic</summary>
/// <remarks>
/// Token semantics can be modeled using the type system.
/// However, performance is of the essence for tokenization.
/// An enumeration should be faster than reflection.
/// </remarks>
public enum LeafTokenKind
{
	/// <summary>calendar date</summary>
	Date,

	/// <summary>dimensions such 5x5</summary>
	Dimensions,

	/// <summary>dollar amount</summary>
	Dollars,

	/// <summary>parsed string</summary>
	Literal,

	/// <summary>quantity</summary>
	Number,

	/// <summary>unparsed string</summary>
	Raw
}

/// <summary>natural language token without child tokens</summary>
public readonly record struct LeafToken : IToken
{
	private LeafToken(LeafTokenKind kind, string value)
	{
		Kind = kind;
		Value = value;
	}

	public static LeafToken Date(string value) => new(LeafTokenKind.Date, value);
	public static LeafToken Dollars(string value) => new(LeafTokenKind.Dollars, value.Split(',').StringJoin());
	public static LeafToken Dimensions(string value) => new(LeafTokenKind.Dimensions, value);
	public static LeafToken Literal(string value) => new(LeafTokenKind.Literal, value);
	public static LeafToken Number(string value) => new(LeafTokenKind.Number, value.Split(',').StringJoin());
	public static LeafToken Raw(string value) => new(LeafTokenKind.Raw, value);

	public LeafTokenKind Kind { get; }

	public string Value { get; }

	public override readonly string ToString() => Value;
}

public enum BranchTokenKind
{
	Parenthetical,
	SubPhrase
}

public readonly struct BranchToken : IToken, IEquatable<BranchToken>
{
	public BranchToken(BranchTokenKind kind, params IEnumerable<IToken> children)
	{
		Kind = kind;
		Children = children.ToList();
	}

	public List<IToken> Children { get; }
	public BranchTokenKind Kind { get; }

	public override string ToString()
	{
		string content = Children.Select(child => child.ToString()).StringJoin(',');

		return Kind switch
		{
			BranchTokenKind.Parenthetical => $"({content})",
			_ => $"{{{content}}}"
		};
	}

	#region IEquatable
	public bool Equals(BranchToken other) =>
		Kind == other.Kind
		&& (ReferenceEquals(Children, other.Children) || Children.SequenceEqual(other.Children));

	public override bool Equals(object? obj) =>
		obj is BranchToken other && Equals(other);

	public override int GetHashCode() => Children.GetHashCode();
	public static bool operator ==(BranchToken a, BranchToken b) => a.Equals(b);
	public static bool operator !=(BranchToken a, BranchToken b) => !a.Equals(b);
	#endregion
}
