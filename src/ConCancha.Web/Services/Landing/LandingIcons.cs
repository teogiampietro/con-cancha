namespace ConCancha.Web.Services.Landing;

/// <summary>Inline SVG paths (24x24 viewBox, stroked) for the landing page.</summary>
public static class LandingIcons
{
    private static readonly IReadOnlyDictionary<string, string> Paths = new Dictionary<string, string>
    {
        ["calendar"] = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"17\" rx=\"2\"/><path d=\"M3 9h18M8 2v4M16 2v4\"/>",
        ["grid"] = "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"2\"/><path d=\"M3 12h18M12 3v18\"/>",
        ["wallet"] = "<path d=\"M3 7a2 2 0 012-2h13v3M3 7v11a2 2 0 002 2h15V8H5a2 2 0 01-2-1zM16 14h2\"/>",
        ["users"] = "<circle cx=\"9\" cy=\"8\" r=\"3.5\"/><path d=\"M2 20c0-4 3-6 7-6s7 2 7 6M17 4.5a3.5 3.5 0 010 7M22 20c0-3-2-5-4-5.5\"/>",
        ["list"] = "<path d=\"M9 6h12M9 12h12M9 18h12M3 6l1 1 2-2M3 12l1 1 2-2M3 18l1 1 2-2\"/>",
        ["bell"] = "<path d=\"M6 8a6 6 0 0112 0c0 7 3 8 3 8H3s3-1 3-8M10 20a2 2 0 004 0\"/>",
        ["chart"] = "<path d=\"M4 20V10M10 20V4M16 20v-7M22 20H2\"/>",
        ["shield"] = "<path d=\"M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6z\"/><path d=\"M9 12l2 2 4-4\"/>",
        ["palette"] = "<path d=\"M12 3a9 9 0 100 18c1.5 0 2-1 1.5-2-.6-1.2.2-2.5 1.6-2.5H17a4 4 0 004-4c0-5-4-9.5-9-9.5z\"/><circle cx=\"7.5\" cy=\"11\" r=\"1\"/><circle cx=\"11\" cy=\"7\" r=\"1\"/><circle cx=\"16\" cy=\"8\" r=\"1\"/>",
        ["clock"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v5l3 2\"/>",
        ["rocket"] = "<path d=\"M5 15c-1.5 1.5-2 5-2 5s3.5-.5 5-2M14 4c4 0 6 2 6 6-2 4-5 7-8 8l-6-6c1-3 4-6 8-8zM9 15l-3-3\"/><circle cx=\"15\" cy=\"9\" r=\"1.5\"/>",
        ["chat"] = "<path d=\"M21 12a8 8 0 01-11.6 7.1L4 20l1-4.6A8 8 0 1121 12z\"/>",
        ["flag"] = "<path d=\"M5 21V4M5 4h12l-2 4 2 4H5\"/>",
        ["check"] = "<path d=\"M5 12.5l4.5 4.5L19 7.5\"/>",
        ["close"] = "<path d=\"M6 6l12 12M18 6L6 18\"/>",
        ["send"] = "<path d=\"M4 12l16-8-6 16-3-7z\"/>"
    };

    public static string PathsOf(string name) => Paths.GetValueOrDefault(name, string.Empty);
}
