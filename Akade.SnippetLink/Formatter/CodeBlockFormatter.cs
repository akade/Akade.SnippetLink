namespace Akade.SnippetLink.Formatter;

internal sealed class CodeBlockFormatter : SnippetFormatter
{
    internal override string Name => "code-block";

    internal override void Write(Snippet snippet, TextWriter writer, Options options)
    {
        writer.WriteLine($"```{snippet.Language}");
        writer.WriteLine(snippet.Content);
        writer.WriteLine("```");
    }
}
