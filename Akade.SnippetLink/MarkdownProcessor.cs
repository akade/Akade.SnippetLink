using Akade.SnippetLink.Formatter;
using Akade.SnippetLink.Importer;
using System.Text;

namespace Akade.SnippetLink;

internal sealed class MarkdownProcessor(IEnumerable<SnippetImporter> importers, IEnumerable<SnippetFormatter> formatters)
{
    internal async Task<Result<bool>> ProcessMarkdownAsync(TextReader reader, TextWriter writer)
    {
        bool changed = false;

        StringBuilder errors = new();
        int lineIndex = 0;
        bool withinCodeBlock = false;
        while (reader.ReadLine() is string line)
        {
            lineIndex++;
            writer.WriteLine(line);

            ReadOnlySpan<char> trimmedStart = line.AsSpan().TrimStart();

            if (trimmedStart.StartsWith("```", StringComparison.Ordinal))
            {
                withinCodeBlock = !withinCodeBlock;
            }

            if (!withinCodeBlock && trimmedStart.StartsWith("<!-- begin-snippet: ", StringComparison.OrdinalIgnoreCase))
            {
                int startLine = lineIndex;
                Result r = await HandleSnippetAsync(line, writer);

                if (r is Result.Failure { ErrorMessage: string error })
                {
                    errors.AppendLine($"Line {startLine}: {error}");
                    continue;
                }

                // skip any existing content
                string? nextLine;
                while ((nextLine = reader.ReadLine()) is not null && !nextLine.Trim().Equals("<!-- end-snippet -->", StringComparison.OrdinalIgnoreCase))
                {
                    lineIndex++;
                }

                // check for end snippet
                if (!(nextLine?.Trim().Equals("<!-- end-snippet -->", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    errors.AppendLine($"Line {startLine}: Missing end-snippet tag for snippet starting at line {startLine}.");
                    continue;
                }

                // write end-snippet line
                writer.WriteLine("<!-- end-snippet -->");

                changed = true;
            }
        }

        if (errors.Length > 0)
        {
            return new Result.Failure(errors.ToString());
        }

        return changed;
    }

    private async Task<Result> HandleSnippetAsync(string line, TextWriter writer)
    {
        var linkResult = SnippetLink.Parse(line);

        if (linkResult is Result<SnippetLink>.Failure linkError)
        {
            return linkError;
        }

        var link = ((Result<SnippetLink>.Success)linkResult).Value;

        SnippetImporter? snippetImporter = null;

        if (link.Importer is not null)
        {
            snippetImporter = importers.FirstOrDefault(i => i.Name.Equals(link.Importer, StringComparison.OrdinalIgnoreCase));
            if (snippetImporter is null)
            {
                return new Result.Failure($"Unknown importer '{link.Importer}'");
            }
        }
        else
        {
            StringBuilder results = new();

            foreach (var importer in importers)
            {
                var canImportResult = importer.CanImport(link.SourceFile, link.Name, new Options(link.ImporterQueryString ?? ""));
                if (canImportResult is Result.Success)
                {
                    snippetImporter = importer;
                    break;
                }
                else if (canImportResult is Result.Failure failure)
                {
                    results.AppendLine(failure.ErrorMessage);
                }
            }

            if (snippetImporter is null)
            {
                return new Result.Failure($"No importer could handle the source file '{link.SourceFile}':{Environment.NewLine}{results}");
            }
        }

        SnippetFormatter? snippetFormatter = null;

        if (link.Formatter is not null)
        {
            snippetFormatter = formatters.FirstOrDefault(f => f.Name.Equals(link.Formatter, StringComparison.OrdinalIgnoreCase));
            if (snippetFormatter is null)
            {
                return new Result.Failure($"Unknown formatter '{link.Formatter}'");
            }
        }
        else
        {
            snippetFormatter = formatters.Single(x => x.GetType() == snippetImporter.PreferredFormatter);
        }

        var importResult = await snippetImporter.ImportSnippetAsync(link.SourceFile, link.Name, new Options(link.ImporterQueryString ?? ""));

        if (importResult is Result<Snippet>.Failure importError)
        {
            return importError;
        }

        var snippet = ((Result<Snippet>.Success)importResult).Value;

        snippetFormatter.Write(snippet, writer, new Options(link.FormatterQueryString ?? ""));

        return new Result.Success();
    }
}
