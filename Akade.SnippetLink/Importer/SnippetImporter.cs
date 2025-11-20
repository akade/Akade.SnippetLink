namespace Akade.SnippetLink.Importer;

internal abstract class SnippetImporter
{
    internal abstract Task<Result<Snippet>> ImportSnippetAsync(string sourceFile, string name, Options options);

    internal abstract string Name { get; }

    internal abstract Result CanImport(string sourceFile, string name, Options options);

    internal abstract Type PreferredFormatter { get; }
}
