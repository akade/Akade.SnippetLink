using BenchmarkDotNet.Attributes;
using System.Security.Cryptography;

namespace Akade.SnippetLink.Benchmarks;

[MemoryDiagnoser]
public class SnippetLinkBenchmarks
{
    [Benchmark]
    public Result<SnippetLink> Parsing()
    {
        return SnippetLink.Parse("<!-- begin-snippet: file.cs name (importer:cs?foo=bar;formatter:md?lang=csharp) -->");
    }

}
