# Akade.SnippetLink

A streamlined, extensible and opinionated dotnet tool for importing snippets into Markdown files.

Shamelessly inspired by [Simon Crop's excellent MarkdownSnippets](https://github.com/SimonCropp/MarkdownSnippets) but
designed for easier extensibility and per snippet customization. The included special support for importing BenchmarkDotNet results
is the main reason for the library.

> :information_source: You can use both tools in the same project. Akade.SnippetLink to import BenchmarkDotNet results, 
and MarkdownSnippets to render links to the original source files.

## Features
- Import code snippets from C# source files into Markdown files
- Import markdown snippets from other Markdown files with special support for **BenchmarkDotNet results**
- Extensible architecture for different source files and output formats

## Getting started

Add the snippet in your csharp-file:
```cs
#region MySnippet
public void MyMethod()
{
    // Implementation
}
#endregion
```
Add it in your markdown-file:
```markdown
<!-- begin-snippet: path/to/YourFile.cs MySnippet -->
<!-- end-snippet -->
```

Run `dnx Akade.SnippetLink` and all markdown files in the current and subdirectories will be processed:

````markdown
<!-- begin-snippet: path/to/YourFile.cs MySnippet -->
```cs
public void MyMethod()
{
    // Implementation
}
```
<!-- end-snippet -->
````

Note that the links will still be there, so you can re-run the tool to update snippets as needed.

Alternatively, install it as a local or global tool:
- `dotnet tool install Akade.SnippetLink`
- Run it using `dotnet snippet-link`


## C-Sharp snippets based on comment or `#region` directives
The following matches either `// begin-snippet: MarkdownProcessorTest` or `#region MarkdownProcessorTest` in the specified source file.
Closed by either `// end-snippet` or `#endregion`. Indentation matches the start.
`<!-- begin-snippet: Akade.SnippetLink.Tests/MarkdownProcessorTests.cs MarkdownProcessorTest -->`
<!-- begin-snippet: Akade.SnippetLink.Tests/MarkdownProcessorTests.cs MarkdownProcessorTest -->
```cs
public string Test()
{
    return string.Empty;
}
```
<!-- end-snippet -->
`<!-- end-snippet -->`

## BenchmarkDotNet result snippets based on benchmark class name

`<!-- begin-snippet: Akade.SnippetLink.Benchmarks SnippetLinkBenchmarks(importer:benchmarkdotnet) -->`
<!-- begin-snippet: Akade.SnippetLink.Benchmarks SnippetLinkBenchmarks(importer:benchmarkdotnet) -->
| Method  | Mean     | Error    | StdDev   | Gen0   | Allocated |
|-------- |---------:|---------:|---------:|-------:|----------:|
| Parsing | 89.79 ns | 1.288 ns | 1.075 ns | 0.0186 |     312 B |

<!-- end-snippet -->
`<!-- end-snippet -->`

## Snippet-Link Format defintion

*<>* means reqruired, *[]* means optional

`<!-- begin-snippet: <source-file-path> <snippet-identifier>[(options)] -->`
`<!-- end-snippet -->`

The options within brackets are semicolon separated key-value pairs where the value is an optional query-string.
Available options:
- importer: Specifies which importer to use for this snippet. If not specified, the default importer for the source file type will be used.
- formatter: Specifies the formatter that determines the rendering of the snippet. If not specified, the default formatter for the selected importer will be used.

## Importers

### CSharpImporter

Imports C# code regions as snippets from `.cs` files, based on `#region` and `#endregion` directives. The `source-file` is the 
path to the `.cs` file, and `snippet-name` is the name of the region.

| Name | Source-file (input) | Snippet-name (input) | Language | Parameters | Default Formatter    |
|------|---------------------|----------------------|----------|------------|--------------------- |
| cs   | .cs file path       | Region name          | cs       | None       | code-block           |

- **Parameters:**  
  - None

**Examples:**
```markdown
<!-- begin-snippet: Akade.SnippetLink.Tests/MarkdownProcessorTests.cs MarkdownProcessorTest -->
<!-- end-snippet -->

<!-- begin-snippet: src/Utils/Helpers.cs UtilityRegion(importer:cs) -->
<!-- end-snippet -->

<!-- begin-snippet: MyApp/Controllers/HomeController.cs IndexAction -->
<!-- end-snippet -->
```

### BenchmarkDotNetImporter

Imports BenchmarkDotNet markdown reports as snippets. The `source-file` should be the project directory containing the benchmark project
, and `snippet-name` is the benchmark class name. If the `env` parameter is set to `true`, environment information is included.

| Name            | Source-file (input)         | Snippet-name (input) | Language   | Parameters           | Default Formatter |
|-----------------|-----------------------------|----------------------|------------|----------------------|-------------------|
| benchmarkdotnet | Project directory (string)  | Benchmark class name | markdown   | env (bool, optional) | raw               |

- **Parameters:**  
  - `env` (optional, bool): If true, includes environment info from the report. Defaults to false

**Examples:**
```markdown
<!-- begin-snippet: Akade.SnippetLink.Benchmarks SnippetLinkBenchmarks(importer:benchmarkdotnet) -->
<!-- end-snippet -->

<!-- begin-snippet: MyProject.Benchmarks MyBenchmarks(importer:benchmarkdotnet;env=true) -->
<!-- end-snippet -->

<!-- begin-snippet: BenchmarksProject AnotherBenchmark(importer:benchmarkdotnet) -->
<!-- end-snippet -->
```

---


## Formatters

- **code-block:**  
  Renders the snippet as a fenced code block in markdown, using the snippet's language (e.g., ```cs for C#).

- **raw:**  
  Outputs the snippet content as raw markdown, without wrapping it in a code block. This is useful for markdown-formatted content such as BenchmarkDotNet reports.
