// Gather all markdown files
using Akade.SnippetLink;
using Akade.SnippetLink.Importer;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

var markdownFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.md", SearchOption.AllDirectories);

ServiceCollection services = new();
services.AddSingleton<IFileSystem, RealFileSystem>();
services.AddSingleton<MarkdownProcessor>();
services.AddSnippetImporters();
services.AddSnippetFormatters();

using ServiceProvider serviceProvider = services.BuildServiceProvider();

foreach (string file in markdownFiles)
{
    AnsiConsole.WriteLine($"{file}");
    using IServiceScope scope = serviceProvider.CreateScope();
    MarkdownProcessor processor = scope.ServiceProvider.GetRequiredService<MarkdownProcessor>();
    string content = await File.ReadAllTextAsync(file);

    using StringReader reader = new(content);
    using StringWriter writer = new();

    Result<bool> result = await processor.ProcessMarkdownAsync(reader, writer);


    if(result is Result<bool>.Success { Value: true })
    {
        writer.Flush();

        if (writer.GetStringBuilder().Equals(content))
        {
            AnsiConsole.MarkupLine("[yellow]No changes[/]");
        }
        else
        {
            await File.WriteAllTextAsync(file, writer.GetStringBuilder().ToString());
            AnsiConsole.MarkupLine("[green] Updated[/]");
        }
    }
    else if (result is Result<bool>.Success { Value: false })
    {
        AnsiConsole.MarkupLine("[yellow]No snippets[/]");
    }
    else if (result is Result<bool>.Failure { ErrorMessage: string error })
    {
        AnsiConsole.MarkupLine("[red]Failed[/]");
        AnsiConsole.WriteLine(error);
    }

}