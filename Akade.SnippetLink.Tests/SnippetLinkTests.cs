namespace Akade.SnippetLink.Tests;

public class SnippetLinkTests
{
    public static TheoryData<string, Result<SnippetLink>> ParseTestCases() => new()
    {
        {
            "<!-- begin-snippet: file.cs snippetName -->",
            new Result<SnippetLink>.Success(new SnippetLink("file.cs", "snippetName"))
        },
        {
            "<!-- begin-snippet: file.txt name -->",
            new Result<SnippetLink>.Success(new SnippetLink("file.txt", "name"))
        },
        {
            "<!-- begin-snippet: file.cs name (importer:cs?foo=bar;formatter:md?lang=csharp) -->",
            new Result<SnippetLink>.Success(new SnippetLink("file.cs", "name", "cs", "foo=bar", "md", "lang=csharp"))
        },
        {
            "<!-- begin-snippet: file.cs name (importer:cs) -->",
            new Result<SnippetLink>.Success(new SnippetLink("file.cs", "name", "cs"))
        },
        {
            "file.cs name",
            new Result<SnippetLink>.Failure("Snippet links need to start with '<!-- begin-snippet: '")
        },
        {
            "<!-- begin-snippet:  -->",
            new Result<SnippetLink>.Failure("Snippet expects a source file followed by a space.")
        },
        {
            "<!-- begin-snippet: file.cs snippet",
            new Result<SnippetLink>.Failure("Snippet link must end with ' -->'.")
        },
        {
            "<!-- begin-snippet: file.cs -->",
            new Result<SnippetLink>.Failure("Snippet name is missing.")
        },
        {
            "<!-- begin-snippet: file.cs name (unknown:val) -->",
            new Result<SnippetLink>.Failure("Unknown SnippetLink parameter key 'unknown'. Expected 'importer' or 'formatter'.")
        },
        {
            "<!-- begin-snippet: file.cs name (importer) -->",
            new Result<SnippetLink>.Failure("SnippetLink parameters must be in the format key:value[?queryString].")
        },
        {
            "<!-- begin-snippet: file.cs name (importer:) -->",
            new Result<SnippetLink>.Failure("SnippetLink parameter 'importer' is missing a value.")
        },
        {
            "<!-- begin-snippet: file.cs name (importer:cs?) -->",
            new Result<SnippetLink>.Failure("Expected query string for SnippetLink parameter 'importer' following after '?'.")
        },
        {
            "<!-- begin-snippet: file.cs name importer:cs -->",
            new Result<SnippetLink>.Failure("SnippetLink parameters must be enclosed in round brackets '()'.")
        }
    };

    [Theory]
    [MemberData(nameof(ParseTestCases))]
    public void Parse(string input, Result<SnippetLink> expected)
    {
        var actual = SnippetLink.Parse(input);
        Assert.Equal(expected, actual);
    }
}
