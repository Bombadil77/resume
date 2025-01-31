namespace JohnWessel;

/// <summary>F is for functional programming</summary>
public static class F
{
	/// <summary>functional programming style <see cref="String.Join{T}(char, IEnumerable{T})"/></summary>
	/// <param name="delimiter">delimiter between items</param>
	public static string StringJoin<T>(this IEnumerable<T> items, char delimiter) =>
		String.Join(delimiter, items);

	/// <summary>functional programming style <see cref="String.Join{T}(string?, IEnumerable{T})"/></summary>
	/// <param name="delimiter">delimiter between items</param>
	public static string StringJoin<T>(this IEnumerable<T> items, string delimiter = "") =>
		String.Join(delimiter, items);
}