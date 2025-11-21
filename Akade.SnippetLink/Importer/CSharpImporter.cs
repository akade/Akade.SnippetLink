using Akade.SnippetLink.Formatter;
using System.Text;

namespace Akade.SnippetLink.Importer;

internal sealed class CSharpImporter(IFileSystem fileSystem) : SnippetImporter
{
    internal override string Name => "cs";

    internal override Type PreferredFormatter => typeof(CodeBlockFormatter);

    internal override Result CanImport(string sourceFile, string name, Options options)
    {
        if (!sourceFile.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return new Result.Failure($"'{sourceFile}' not end with .cs");

        if (!fileSystem.FileExists(sourceFile))
            return new Result.Failure($"Source file '{sourceFile}' not found.");

        return new Result.Success();
    }

    internal override async Task<Result<Snippet>> ImportSnippetAsync(string sourceFile, string name, Options options)
    {
        ReadOnlySpan<char> sourceText = await fileSystem.ReadAllTextAsync(sourceFile);

        int? startLine = null;
        StringBuilder snippetContent = new();

        int lineNumber = 0;
        int indentationLevel = 0;

        bool isRegionSnippet = false;

        foreach (ReadOnlySpan<char> rawLine in sourceText.EnumerateLines())
        {
            ReadOnlySpan<char> line = rawLine.Trim();
            if ((line.StartsWith("#region ", StringComparison.OrdinalIgnoreCase) && line["#region ".Length..].StartsWith(name, StringComparison.OrdinalIgnoreCase))
             || (line.StartsWith("// begin-snippet: ") && line["// begin-snippet: ".Length..].StartsWith(name, StringComparison.OrdinalIgnoreCase)))
            {
                isRegionSnippet = line.StartsWith("#region ", StringComparison.OrdinalIgnoreCase);
                startLine = lineNumber;
                indentationLevel = isRegionSnippet ? rawLine.IndexOf('#') : rawLine.IndexOf('/');
            }
            else if (startLine.HasValue && (isRegionSnippet && line.StartsWith("#endregion", StringComparison.OrdinalIgnoreCase)
                                         || line.StartsWith("// end-snippet")))
            {
                snippetContent.Length -= Environment.NewLine.Length; // Remove last newline
                return new Snippet(
                    SourceFile: sourceFile,
                    Name: name,
                    StartLine: startLine.Value,
                    EndLine: lineNumber,
                    Content: snippetContent.ToString(),
                    Language: "cs"
                );
            }
            else if (startLine.HasValue)
            {
                int currentIndentation = rawLine.IndexOfAnyExcept([' ', '\t']);
                int trim = currentIndentation > 0 ? Math.Min(indentationLevel, currentIndentation) : 0;

                snippetContent.Append(rawLine[trim..]);
                snippetContent.AppendLine();
            }
            lineNumber++;
        }

        return new Result.Failure($"Snippet '{name}' not found in file '{sourceFile}'.");
    }
}
