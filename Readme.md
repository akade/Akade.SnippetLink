# Akade.SnippetLink

![.Net Version](https://img.shields.io/badge/dynamic/xml?color=%23512bd4&label=version&query=%2F%2FTargetFrameworks%5B1%5D&url=https://raw.githubusercontent.com/akade/Akade.SnippetLink/refs/heads/main/Akade.SnippetLink/Akade.SnippetLink.csproj&logo=.net)
[![CI Build](https://github.com/akade/Akade.SnippetLink/actions/workflows/ci-build.yml/badge.svg?branch=main)](https://github.com/akade/Akade.SnippetLink/actions/workflows/ci-build.yml)
[![NuGet version (Akade.SnippetLink)](https://img.shields.io/nuget/v/Akade.SnippetLink.svg?label=dotnet%20tool)](https://www.nuget.org/packages/Akade.SnippetLink/)
[![MIT](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/akade/Akade.SnippetLink#readme)

A streamlined, extensible and opinionated dotnet tool for importing snippets into Markdown files.

Shamelessly inspired by [Simon Crop's excellent MarkdownSnippets](https://github.com/SimonCropp/MarkdownSnippets) but
designed for easier extensibility and per snippet parametrization. The main motivations for this incarnation are **support for directly referencing C# symbols** like methods and **importing BenchmarkDotNet results**. In contrast to MarkdownSnippets, SnippetLink ironically does not render any user-clickable links.

## Features
- Import code snippets from C# source files into Markdown files
- Import markdown snippets from other Markdown files with special support for **BenchmarkDotNet results**
- Extensible architecture for different source files and output formats

## Getting started

> :information_source: Prerequisite: .NET 10 SDK

If you have the following method in YourFile.cs:
```cs
public void MyMethod()
{
    // Implementation
}
```
You can reference it in your markdown-file:
```markdown
<!-- begin-snippet: path/to/YourFile.cs MyMethod -->
<!-- end-snippet -->
```

Run `dnx Akade.SnippetLink` and all markdown files in the current and subdirectories will now include any linked snippet:

````markdown
<!-- begin-snippet: path/to/YourFile.cs MyMethod -->
```cs
public void MyMethod()
{
    // Implementation
}
```
<!-- end-snippet -->
````

As you can see, the links will still be there: You can repeatedly run the tool to update snippets as needed.

Alternatively, install it as a local or global tool:
- `dotnet tool install Akade.SnippetLink`
- Run it using `dotnet snippet-link`

## C-Sharp snippets

You can directly reference most symbols (see below) by name. If you want to reference a different part of your file,
use either `// begin-snippet: MarkdownProcessorTest` or `#region MarkdownProcessorTest` in the specified source file.
Closed by either `// end-snippet` or `#endregion`. Indentation matches the start of the content.
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
Imports C# code snippets from `.cs` files, based on a **symbol**, **region name**, or a reference to a *snippet-comment*.

> :information_source: For **Symbols**, the name does not need to be fully qualified if it is unique within the file. It matches from the innermost to outermost scope:
i.e. If only one method `A` exists within the file, `A` is sufficient. If multiple methods `A` exist within different classes, the class name must be included as well: `ClassName.A`.

> :heavy_exclamation_mark: Currently, generic type parameters and method overloads are not supported for symbol resolution. This will likely be added in a future release.
Use regions or snippet-comments in the meantime.

- Supported Symbols
  - Constructors
  - Methods
  - Properties
  - Types
    - Enums
    - Classes
    - Extension blocks
    - Extensions
    - Interfaces
    - Records
    - Structs
  - Delegates
- Regions, referenced by name
- Snippet-Comments: `// begin-snippet: snippet-name` and `// end-snippet`

| Name | Source-file (input) | Snippet-name (input)                           | Language | Parameters                 | Default Formatter    |
|------|---------------------|------------------------------------------------|----------|----------------------------|--------------------- |
| cs   | .cs file path       | Symbol, region name or link to snippet-comment | cs       | body-only (bool, optional) | code-block           |

- **Parameters:**  
  - body-only (optional, bool): If true, only the body of the symbol is included (if applicable), excluding the signature or declaration. Defaults to false and
    ignored for regions and snippet-comments.

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
