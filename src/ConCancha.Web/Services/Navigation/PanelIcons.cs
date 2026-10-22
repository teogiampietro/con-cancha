namespace ConCancha.Web.Services.Navigation;

/// <summary>Inline SVG paths (24x24 viewBox, stroked) keyed by icon name.</summary>
public static class PanelIcons
{
    public const string Home = "home";
    public const string Calendar = "calendar";
    public const string List = "list";
    public const string Bell = "bell";
    public const string Wallet = "wallet";
    public const string Grid = "grid";
    public const string Users = "users";
    public const string More = "more";
    public const string Share = "share";
    public const string Eye = "eye";
    public const string Logout = "logout";

    private static readonly IReadOnlyDictionary<string, string> Paths = new Dictionary<string, string>
    {
        [Home] = "<path d=\"M3 11l9-8 9 8M5 10v10h5v-6h4v6h5V10\"/>",
        [Calendar] = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"17\" rx=\"2\"/><path d=\"M3 9h18M8 2v4M16 2v4M8 14h.01M12 14h.01M16 14h.01M8 17.5h.01M12 17.5h.01\"/>",
        [List] = "<path d=\"M9 6h12M9 12h12M9 18h12M3 6l1 1 2-2M3 12l1 1 2-2M3 18l1 1 2-2\"/>",
        [Bell] = "<path d=\"M6 8a6 6 0 0112 0c0 7 3 8 3 8H3s3-1 3-8M10 20a2 2 0 004 0\"/>",
        [Wallet] = "<path d=\"M3 7a2 2 0 012-2h13v3M3 7v11a2 2 0 002 2h15V8H5a2 2 0 01-2-1zM16 14h2\"/>",
        [Grid] = "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"2\"/><path d=\"M3 12h18M12 3v18\"/>",
        [Users] = "<circle cx=\"9\" cy=\"8\" r=\"3.5\"/><path d=\"M2 20c0-4 3-6 7-6s7 2 7 6M17 4.5a3.5 3.5 0 010 7M22 20c0-3-2-5-4-5.5\"/>",
        [More] = "<circle cx=\"5\" cy=\"12\" r=\"1.2\"/><circle cx=\"12\" cy=\"12\" r=\"1.2\"/><circle cx=\"19\" cy=\"12\" r=\"1.2\"/>",
        [Share] = "<circle cx=\"18\" cy=\"5\" r=\"3\"/><circle cx=\"6\" cy=\"12\" r=\"3\"/><circle cx=\"18\" cy=\"19\" r=\"3\"/><path d=\"M8.6 10.5l6.8-4M8.6 13.5l6.8 4\"/>",
        [Eye] = "<path d=\"M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/>",
        [Logout] = "<path d=\"M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4M16 17l5-5-5-5M21 12H9\"/>"
    };

    public static string PathsOf(string name) => Paths.GetValueOrDefault(name, string.Empty);
}
