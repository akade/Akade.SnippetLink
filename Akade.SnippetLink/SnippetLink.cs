using System.Collections.Immutable;

namespace Akade.SnippetLink;

public sealed record SnippetLink(string SourceFile, string Name, string? Importer = null, string? ImporterQueryString = null, string? Formatter = null, string? FormatterQueryString = null)
{
    internal static Result<SnippetLink> Parse(ReadOnlySpan<char> input)
    {
        if (!input.StartsWith("<!-- begin-snippet: "))
        {
            return new Result<SnippetLink>.Failure("Snippet links need to start with '<!-- begin-snippet: '");
        }

        ReadOnlySpan<char> remainder = input["<!-- begin-snippet: ".Length..];

        if (!remainder.EndsWith("-->", StringComparison.OrdinalIgnoreCase))
        {
            return new Result<SnippetLink>.Failure("Snippet link must end with ' -->'.");
        }

        int separatorIndex = remainder.IndexOf(' ');
        if (separatorIndex < 1)
        {
            return new Result<SnippetLink>.Failure("Snippet expects a source file followed by a space.");
        }
        
        remainder = remainder[..^3].Trim();

        string sourceFile = remainder[..separatorIndex].TrimStart().ToString();
        remainder = remainder[separatorIndex..].TrimStart();

        string snippetName;

        // find snippet name, either seperated by whitespace, open round bracket or end of string
        separatorIndex = remainder.IndexOfAny([' ', '(']);

        if (separatorIndex < 0)
        {
            if (remainder.Length == 0)
            {
                return new Result<SnippetLink>.Failure("Snippet name is missing.");
            }
            snippetName = remainder.ToString();
            remainder = [];
        }
        else
        {
            snippetName = remainder[..separatorIndex].ToString();
            remainder = remainder[separatorIndex..];
        }

        remainder = remainder.Trim();

        // parse optional parameters within round brackets and seperated by ';'

        string? importer = null;
        string? importerQueryString = null;
        string? formatter = null;
        string? formatterQueryString = null;

        if (!remainder.IsEmpty)
        {
            if (remainder[0] != '(' || remainder[^1] != ')')
            {
                return new Result<SnippetLink>.Failure("SnippetLink parameters must be enclosed in round brackets '()'.");
            }
            remainder = remainder[1..^1];

            foreach (Range paramRange in remainder.Split(';'))
            {
                ReadOnlySpan<char> key = [];
                ReadOnlySpan<char> value = [];
                ReadOnlySpan<char> queryString = [];

                // format: key:value[?queryString]
                ReadOnlySpan<char> paramSpan = remainder[paramRange].Trim();
                int colonIndex = paramSpan.IndexOf(':');

                if (colonIndex < 1)
                {
                    return new Result<SnippetLink>.Failure("SnippetLink parameters must be in the format key:value[?queryString].");
                }

                key = paramSpan[..colonIndex].Trim();

                ReadOnlySpan<char> valueAndQuery = paramSpan[(colonIndex + 1)..].Trim();
                int questionMarkIndex = valueAndQuery.IndexOf('?');

                if (questionMarkIndex < 0)
                {
                    if (valueAndQuery.IsEmpty)
                    {
                        return new Result<SnippetLink>.Failure($"SnippetLink parameter '{key}' is missing a value.");
                    }
                    value = valueAndQuery;
                }
                else
                {
                    value = valueAndQuery[..questionMarkIndex].Trim();
                    ReadOnlySpan<char> queryStringSpan = valueAndQuery[(questionMarkIndex + 1)..].Trim();

                    if (queryStringSpan.IsEmpty)
                    {
                        return new Result<SnippetLink>.Failure($"Expected query string for SnippetLink parameter '{key}' following after '?'.");
                    }

                    queryString = queryStringSpan;
                }

                if (key.Equals("importer", System.StringComparison.OrdinalIgnoreCase))
                {
                    importer = value.ToString();
                    importerQueryString = queryString.IsEmpty ? null : queryString.ToString();
                }
                else if (key.Equals("formatter", System.StringComparison.OrdinalIgnoreCase))
                {
                    formatter = value.ToString();
                    formatterQueryString = queryString.IsEmpty ? null : queryString.ToString();
                }
                else
                {
                    return new Result<SnippetLink>.Failure($"Unknown SnippetLink parameter key '{key}'. Expected 'importer' or 'formatter'.");
                }
            }
        }

        return new SnippetLink(sourceFile, snippetName, importer, importerQueryString, formatter, formatterQueryString);
    }
}
