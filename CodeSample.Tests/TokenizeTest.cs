namespace JohnWessel.Tests;

[Trait("Aspect", "Natural Language")]
[Trait("Kind", "Unit")]
public class TokenizeTest
{
	static readonly HashSet<string> _stopWords = [
		"a","about","am","an","and","are","as","at",
		"be","by",
		"con",
		"de","del","do",
		"el","en",
		"for","from",
		"how",
		"i","if","in","is","it",
		"la","las","los",
		"me","my",
		"new","no",
		"of","on","only","or",
		"per",
		"so",
		"that","the","this","through","thru","to",
		"us",
		"was","way","we","what","when","where","who","will","with"
	];

	[Fact]
	public void Null() =>
		Assert.Empty(Tokenizer.Tokenize(null));

	[Fact]
	public void Empty() =>
		Assert.Empty(Tokenizer.Tokenize(""));

	[Fact]
	public void Whitespace() =>
		Assert.Empty(Tokenizer.Tokenize(" "));

	[Fact]
	public void DelimiterOnly() =>
		Assert.Empty(Tokenizer.Tokenize("-"));

	[Theory]
	[InlineData("lorem ipsum", new[] { "lorem", "ipsum" })]
	[InlineData("Animi recusandae nisi cumque deserunt quam", new[] { "animi", "recusandae", "nisi", "cumque", "deserunt", "quam" })]
	public void TokenizeSimple(string phrase, string[] expected)
	{
		string[] tokens = Tokenizer.Tokenize(phrase, _stopWords);
		Assert.Equal(expected, tokens);
	}

	[Theory]
	[InlineData("mahi mahi", new[] { "mahi" })]
	[InlineData("the chairperson took the chair", new[] { "chairperson", "took" })]
	public void RedundantWords(string phrase, string[] expected)
	{
		string[] tokens = Tokenizer.Tokenize(phrase, _stopWords);
		Assert.Equal(expected, tokens);
	}

	[Theory]
	[InlineData("the quick brown fox", new[] { "quick", "brown", "fox" })]
	[InlineData("the", new[] { "the" })]
	public void StopWords(string phrase, string[] expected)
	{
		string[] tokens = Tokenizer.Tokenize(phrase, _stopWords);
		Assert.Equal(expected, tokens);
	}

	[Fact]
	public void EmptyParenthetical() =>
		Assert.Empty(Tokenizer.Tokenize("()"));

	[Theory]
	[InlineData("(P)", new[] { "(p)" })]
	[InlineData("[R]", new[] { "(r)" })]
	public void ParentheticalSymbol(string phrase, string[] expected)
	{
		string[] tokens = Tokenizer.Tokenize(phrase);
		Assert.Equal(expected, tokens);
	}

	[Theory]
	[InlineData("abc - def", new[] { "abc", "def" })]
	[InlineData("abc - de fg", new[] { "abc", "de fg" })]
	[InlineData("ab - cde", new[] { "ab cde", "cde" })]
	[InlineData("ab - cd", new[] { "ab cd" })]
	public void Subphrase(string phrase, string[] expected)
	{
		string[] tokens = Tokenizer.Tokenize(phrase);
		Assert.Equal(expected, tokens);
	}

	#region real world
	// These test cases were gathered from real world data input to Lucid.
	[Theory]
	[InlineData("#?", new[] { "#?" })]
	[InlineData("100 Crawford Ave", new[] { "100", "crawford", "ave" })]
	[InlineData("202007363`", new[] { "202007363" })]
	[InlineData("B23-180", new[] { "b23", "180" })]
	[InlineData("BD23-6405-REV-01", new[] { "bd23", "6405", "rev-01" })]
	[InlineData("BP22-00472-A", new[] { "bp22", "00472-a" })]
	[InlineData("BP22-00679-AS", new[] { "bp22", "00679-as" })]
	[InlineData("BP24-00005 thru 00008", new[] { "bp24", "00005", "00008" })]
	[InlineData("BP24-00034 and BP24-00035", new[] { "bp24", "00034", "00035" })]
	[InlineData("CBLD-1221-578099 ADD008", new[] { "cbld", "1221", "578099", "add008" })]
	[InlineData("CIP 0017:HSIPL-5484(003)", new[] { "cip", "0017", "hsipl", "5484", "003" })]
	[InlineData("COM-2321946", new[] { "com", "2321946" })]
	[InlineData("Contract CON0003512; PO 64289", new[] { "contract", "con0003512", "po 64289", "64289" })]
	[InlineData("FPS Number 1238.12", new[] { "fps", "number", "1238.12" })]
	[InlineData("Monthly:  $13,600", new[] { "monthly", "$13600" })]
	[InlineData("N/A", new[] { "n-a" })]
	[InlineData("NTE $17,675", new[] { "nte", "$17675" })]
	[InlineData("PLN21-20007_PM_001", new[] { "pln21", "20007 pm", "pm 001", "001" })]
	[InlineData("PMT24-11161", new[] { "pmt24", "11161" })]
	[InlineData("PN E18-000-151  [CD -0652]", new[] { "pn e18", "e18", "000", "151", "cd 0652", "0652" })]
	[InlineData("PO 395", new[] { "po 395", "395" })]
	[InlineData("PO 5812", new[] { "po 5812", "5812" })]
	[InlineData("PO No.: 170108", new[] { "po 170108", "170108" })]
	public void RealWorldProjectClientRef(string phrase, string[] expected)
	{
		string[] tokens = Tokenizer.Tokenize(phrase, _stopWords);
		Assert.Equal(expected, tokens);
	}

	[Theory]
	[InlineData("16 X 20 Tuff Shed", new[] { "16x20", "tuff", "shed" })]
	[InlineData("22812 Palomarst(101-102)[B]T.I.", new[] { "22812", "palomarst", "101", "102", "(b) ti" })]
	[InlineData("24032 Clintonkeith[B}NewCom", new[] { "24032", "clintonkeith b", "b newcom", "newcom" })]
	[InlineData("49 Paseo de Toner AT+T", new[] { "49 paseo", "paseo", "toner", "at-t" })]
	[InlineData("AG - 3 Story SFR w/garage", new[] { "ag 3", "3 story", "story", "sfr", "garage" })]
	[InlineData("AG - REV to BLD21-000521", new[] { "ag rev", "rev", "bld21", "000521" })]
	[InlineData("American Canyon, City of", new[] { "american", "canyon", "city" })]
	[InlineData("B11-1081 4301 Hacienda Dr., #500", new[] { "b11", "1081", "4301", "hacienda dr", "500" })]
	[InlineData("Bereavement (Max 24 hours)", new[] { "bereavement", "max 24", "24 hours", "hours" })]
	[InlineData("Chukchansi Gold Resort & Casino", new[] { "chukchansi", "gold", "resort", "casino" })]
	[InlineData("City of La Mesa", new[] { "city", "mesa" })]
	[InlineData("COVID Sick Leave (Through 12/31/22 only)", new[] { "covid", "sick", "leave", "12/31/22" })]
	[InlineData("EP-22-05784 Crown Castle SAC_LAGUNA_070 - 8510 Lotz Pkwy", new[] { "ep 22", "22 05784", "05784", "crown", "castle", "sac", "laguna", "070", "8510", "lotz", "pkwy" })]
	[InlineData("Fire Sprinklers- 1185 E. Grand-New plan", new[] { "fire", "sprinklers", "1185 e", "e grand", "grand", "plan" })]
	[InlineData("Fire Station #3", new[] { "fire", "station 3" })]
	[InlineData("Fortuna - City of", new[] { "fortuna", "city" })]
	[InlineData("Gardena Building Dept. Services", new[] { "gardena", "building", "dept", "services" })]
	[InlineData("GLS", new[] { "gls" })]
	[InlineData("GOBELI - Fourplex Apartment ???", new[] { "gobeli", "fourplex", "apartment" })]
	[InlineData("HWD21-0080 > NFL TI – REV D: Revise Convenience Stairs", new[] { "hwd21", "0080", "nfl ti", "rev d", "revise", "convenience", "stairs" })]
	[InlineData("I - New Residential, Residential Additions, TI<=3,000sf", new[] { "residential", "additions ti", "3000sf" })]
	[InlineData("ICC Course Teaching/Training", new[] { "icc", "course", "teaching", "training" })]
	[InlineData("K2 Development Co", new[] { "k2 development", "development co" })]
	[InlineData("M/E Revision #2? Ausin General", new[] { "m-e", "revision 2", "2 ausin", "ausin", "general" })]
	[InlineData("New MOB for ADVENTIST HEALTH {OSHPD 3}", new[] { "mob", "adventist", "health", "oshpd 3" })]
	[InlineData("Reimbursables - BOOK NO TIME!", new[] { "reimbursables", "book", "time" })]
	[InlineData("SFR (2,505 sf) & Solar System for WOOD *Creek Fire*", new[] { "sfr", "2505sf", "solar", "system", "wood", "creek", "fire" })]
	[InlineData("STD Plan 5002 (Moonlight) + Solar Sys for LENNAR HOMES", new[] { "std", "plan", "5002", "moonlight", "solar", "sys", "lennar", "homes" })]
	[InlineData("Webb Residence= Fire Sprinklers", new[] { "webb", "residence", "fire", "sprinklers" })]
	[InlineData("EP - SCG @ 32890 VALLEYVIEW RD X/ST LAKEVIEW TERRACE, W.O.: 30115673", new[] { "ep scg", "scg", "32890", "valleyview rd", "rd x-st", "x-st", "lakeview", "terrace wo", "30115673" })]
	[InlineData("LLA 2020-003, Baxter and I-15 [P] Strata Equity Group", new[] { "lla", "2020", "003", "baxter", "i-15", "(p)", "strata", "equity", "group" })]
	public void RealWorldProjectNames(string phrase, string[] expected)
	{
		string[] tokens = Tokenizer.Tokenize(phrase, _stopWords);
		Assert.Equal(expected, tokens);
	}

	[Theory]
	[InlineData("1", new string[] { "1" })]
	[InlineData("36", new[] { "36" })]
	[InlineData("117", new[] { "117" })]
	[InlineData("003010", new[] { "003010" })]
	public void RealWorldUserClientRef(string phrase, string[] expected)
	{
		string[] tokens = Tokenizer.Tokenize(phrase, _stopWords);
		Assert.Equal(expected, tokens);
	}

	[Theory]
	[InlineData("Al-Qudsi", new[] { "al qudsi", "qudsi" })]
	[InlineData("Anderson-Smith", new[] { "anderson", "smith" })]
	[InlineData("Childers - MKTG", new[] { "childers", "mktg" })]
	[InlineData("de Chambeau", new[] { "chambeau" })]
	[InlineData("John", new[] { "john" })]
	[InlineData("M R", new[] { "m r" })]
	[InlineData("Munguia Sanchez", new[] { "munguia", "sanchez" })]
	[InlineData("O'Flaherty", new[] { "oflaherty" })]
	[InlineData("Ron", new[] { "ron" })]
	[InlineData("Van Ryn", new[] { "van", "ryn" })]
	[InlineData("Vu", new[] { "vu" })]
	[InlineData("Wessel", new[] { "wessel" })]
	[InlineData("Whisler", new[] { "whisler" })]
	public void RealWorldUserNames(string phrase, string[] expected)
	{
		string[] tokens = Tokenizer.Tokenize(phrase, _stopWords);
		Assert.Equal(expected, tokens);
	}
	#endregion
}
