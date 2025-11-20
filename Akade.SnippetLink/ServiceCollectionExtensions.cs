using Akade.SnippetLink.Formatter;
using Akade.SnippetLink.Importer;
using Microsoft.Extensions.DependencyInjection;

namespace Akade.SnippetLink;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSnippetFormatters(this IServiceCollection services)
    {
        services.AddSingleton<SnippetFormatter, CodeBlockFormatter>();
        services.AddSingleton<SnippetFormatter, RawMarkdownFormatter>();

        return services;
    }

    public static IServiceCollection AddSnippetImporters(this IServiceCollection services)
    {
        services.AddSingleton<SnippetImporter, CSharpImporter>();
        services.AddSingleton<SnippetImporter, BenchmarkDotNetImporter>();

        return services;
    }
}
