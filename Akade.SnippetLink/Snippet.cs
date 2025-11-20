namespace Akade.SnippetLink;

internal sealed record class Snippet(
    string SourceFile,
    string Name,
    int? StartLine,
    int? EndLine,
    string Content,
    string Language);