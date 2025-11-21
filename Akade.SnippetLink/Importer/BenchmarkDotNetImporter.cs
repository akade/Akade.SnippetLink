using Akade.SnippetLink.Formatter;

namespace Akade.SnippetLink.Importer;

internal sealed class BenchmarkDotNetImporter(IFileSystem fileSystem) : SnippetImporter
{
    internal override string Name => "BenchmarkDotNet";

    internal override Type PreferredFormatter => typeof(RawMarkdownFormatter);

    internal override Result CanImport(string sourceFile, string name, Options options)
    {
        string? filePath = GetBenchmarkOutputFile(sourceFile, name);

        if (filePath is null || !fileSystem.FileExists(filePath))
        {
            return new Result.Failure($"BenchmarkDotNet output file {filePath} not found.");
        }

        return new Result.Success();
    }

    private string? GetBenchmarkOutputFile(string sourceFile, string name)
    {
        string projectDirectory = sourceFile; // sourceFile is the net folder
        string releasePath = Path.Combine(projectDirectory, "bin", "Release");

        // find latest net folder
        if (!fileSystem.DirectoryExists(releasePath))
        {
            return null;
        }


        string? netFolder = fileSystem.GetDirectories(releasePath, "net*")
            .OrderByDescending(dir => double.Parse(Path.GetFileName(dir)["net".Length..]))
            .FirstOrDefault();

        string? filePath = netFolder is not null ? Path.Combine(netFolder, "BenchmarkDotNet.Artifacts", "results", $"{sourceFile}.{name}-report-github.md") : null;
        return filePath;
    }

    internal override async Task<Result<Snippet>> ImportSnippetAsync(string sourceFile, string name, Options options)
    {
        bool includeEnv = options.Get("env", false);

        string? filePath = GetBenchmarkOutputFile(sourceFile, name);

        if (filePath is null || !fileSystem.FileExists(filePath))
            return new Result.Failure($"BenchmarkDotNet output file {filePath} not found.");

        ReadOnlySpan<char> content = await fileSystem.ReadAllTextAsync(filePath);

        // The environment information is within a code block
        if (!includeEnv)
        {
            int envStart = content.IndexOf("```", StringComparison.OrdinalIgnoreCase);

            if (envStart >= 0)
            {
                content = content[3..];
                int envEnd = content.IndexOf("```", StringComparison.OrdinalIgnoreCase);
                if (envEnd >= 0)
                {
                    content = content[(envEnd + 3)..].TrimStart();
                }
            }
        }

        return new Snippet
        (
            SourceFile: sourceFile,
            Name: name,
            Content: content.ToString(),
            StartLine: null,
            EndLine: null,
            Language: "markdown"
        );
    }
}
