namespace DTBK.Engine;

public static partial class CanonicalCellDataLinkRegistry
{
    // Compatibility overloads for legacy contract rows that carry an explicit dependency/gui token.
    private static CellDataLink Input(string s, string t, string c, string source, string output, string dependency, string gui) =>
        new(s, t, c, "Input", source, null, null, new[] { dependency }, "INPUT→STATE", output, gui, "CONTRACT_ONLY", "Legacy input contract preserved; concrete provider/control wiring must be verified.");

    private static CellDataLink Lookup(string s, string t, string c, string provider, string deps, string output, string dependency, string gui) =>
        new(s, t, c, "Lookup", "Provider", provider, null, new[] { deps, dependency }, "LOOKUP→STATE", output, gui, "CONTRACT_ONLY", "Legacy lookup contract preserved; runtime implementation must be verified.");
}
