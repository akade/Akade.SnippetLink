namespace Akade.SnippetLink.Formatter;

internal abstract class SnippetFormatter
{
    internal abstract string Name { get; }

    internal abstract void Write(Snippet snippet, TextWriter writer, Options options);
}
