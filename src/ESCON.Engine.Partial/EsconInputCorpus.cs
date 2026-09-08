using System.Security.Cryptography;

namespace DTBK.Engine;

/// <summary>
/// Canonical boundary for consuming the exact ESCON source-data corpus.
/// The corpus is never synthesized or reconstructed here: DTBK reads the
/// same physical files supplied by the ESCON distribution and records their
/// SHA-256 hashes for provenance/differential testing.
/// </summary>
public sealed record EsconInputFile(
    string RelativePath,
    long Length,
    string Sha256);

public sealed class EsconInputCorpus
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bg", ".clib", ".tlib", ".db", ".sqlite", ".sqlite3", ".es", ".xml", ".cdt", ".ctc", ".dmc", ".esd"
    };

    public EsconInputCorpus(string root)
    {
        if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("ESCON input root is required.", nameof(root));
        Root = Path.GetFullPath(root);
    }

    public string Root { get; }

    public IReadOnlyList<EsconInputFile> Inventory()
    {
        if (!Directory.Exists(Root))
            throw new DirectoryNotFoundException($"ESCON input corpus was not found: {Root}");

        return Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path)))
            .Select(path => new EsconInputFile(
                Path.GetRelativePath(Root, path).Replace(Path.DirectorySeparatorChar, '/'),
                new FileInfo(path).Length,
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()))
            .OrderBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public EsconInputFile Require(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        var fullPath = Path.GetFullPath(Path.Combine(Root, relativePath));
        if (!fullPath.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ESCON input path escapes the corpus root.");
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"ESCON input file was not found: {relativePath}", fullPath);
        if (!SupportedExtensions.Contains(Path.GetExtension(fullPath)))
            throw new NotSupportedException($"Unsupported ESCON input extension: {Path.GetExtension(fullPath)}");

        var info = new FileInfo(fullPath);
        return new EsconInputFile(
            Path.GetRelativePath(Root, fullPath).Replace(Path.DirectorySeparatorChar, '/'),
            info.Length,
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fullPath))).ToLowerInvariant());
    }
}
