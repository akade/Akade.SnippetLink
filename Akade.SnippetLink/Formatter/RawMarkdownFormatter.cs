namespace Akade.SnippetLink.Formatter;

internal sealed class RawMarkdownFormatter : SnippetFormatter
{
    internal override string Name => "raw";

    internal override void Write(Snippet snippet, TextWriter writer, Options options)
    {
        writer.WriteLine(snippet.Content);
    }
}
