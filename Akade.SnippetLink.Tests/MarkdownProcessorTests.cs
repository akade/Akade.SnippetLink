using Akade.SnippetLink.Importer;
using Microsoft.Extensions.DependencyInjection;

namespace Akade.SnippetLink.Tests;

public class MarkdownProcessorTests
{

    public static TheoryData<string, (string fileName, string content)[], string> SuccessfulTestCases() => new()
    {
        { "Input markdown content", [], "Input markdown content" },
        // C# snippet test case, first time insertion
        {
            """
            Here is a code snippet:
            <!-- begin-snippet: Example.cs MySnippet -->
            <!-- end-snippet -->
            """,
            [
                (
                    "Example.cs",
                    """
                    // Some C# code
                    #region MySnippet
                    public class MyClass
                    {
                        public void MyMethod()
                        {
                            // method body
                        }
                    }
                    #endregion
                    """
                )
            ],
            """
            Here is a code snippet:
            <!-- begin-snippet: Example.cs MySnippet -->
            ```cs
            public class MyClass
            {
                public void MyMethod()
                {
                    // method body
                }
            }
            ```
            <!-- end-snippet -->
            """
        },
        // C# snippet test case, second time insertion
        {
            """
            Here is a code snippet:
            <!-- begin-snippet: Example.cs MySnippet -->
            ```cs
            public class MyClass
            {
                public void MyMethod()
                {
                    // method body
                }
            }
            ```
            <!-- end-snippet -->
            """,
            [
                (
                    "Example.cs",
                    """
                    // Some C# code
                    #region MySnippet
                    public class MyClass
                    {
                        public void MyMethod()
                        {
                            // Snippet has been changed
                        }
                    }
                    #endregion
                    """
                )
            ],
            """
            Here is a code snippet:
            <!-- begin-snippet: Example.cs MySnippet -->
            ```cs
            public class MyClass
            {
                public void MyMethod()
                {
                    // Snippet has been changed
                }
            }
            ```
            <!-- end-snippet -->
            """
        },
        // BenchmarkDotNet result snippet
        {
            """
            Benchmark results:
            <!-- begin-snippet: Akade.IndexedSet.Benchmarks ConcurrentSetBenchmarks (importer:benchmarkdotnet) -->
            <!-- end-snippet -->
            """,
            [
                (
                    "Akade.IndexedSet.Benchmarks/bin/Release/net10.0/BenchmarkDotNet.Artifacts/results/Akade.IndexedSet.Benchmarks.ConcurrentSetBenchmarks-report-github.md",
                    """
                    ```
                    Environment PC information
                    ```
                    | Method | Mean | Error | StdDev |
                    |------- |-----:|------:|-------:|
                    | Test1  | 1 ms | 0.1 ms| 0.2 ms |
                    """
                )
            ],
            """
            Benchmark results:
            <!-- begin-snippet: Akade.IndexedSet.Benchmarks ConcurrentSetBenchmarks (importer:benchmarkdotnet) -->
            | Method | Mean | Error | StdDev |
            |------- |-----:|------:|-------:|
            | Test1  | 1 ms | 0.1 ms| 0.2 ms |
            <!-- end-snippet -->
            """
        }
        ,
        // BenchmarkDotNet result snippet with environment
        {
            """
            Benchmark results:
            <!-- begin-snippet: Akade.IndexedSet.Benchmarks ConcurrentSetBenchmarks (importer:benchmarkdotnet?env=true) -->
            <!-- end-snippet -->
            """,
            [
                (
                    "Akade.IndexedSet.Benchmarks/bin/Release/net10.0/BenchmarkDotNet.Artifacts/results/Akade.IndexedSet.Benchmarks.ConcurrentSetBenchmarks-report-github.md",
                    """
                    ```
                    Environment PC information
                    ```
                    | Method | Mean | Error | StdDev |
                    |------- |-----:|------:|-------:|
                    | Test1  | 1 ms | 0.1 ms| 0.2 ms |
                    """
                )
            ],
            """
            Benchmark results:
            <!-- begin-snippet: Akade.IndexedSet.Benchmarks ConcurrentSetBenchmarks (importer:benchmarkdotnet?env=true) -->
            ```
            Environment PC information
            ```
            | Method | Mean | Error | StdDev |
            |------- |-----:|------:|-------:|
            | Test1  | 1 ms | 0.1 ms| 0.2 ms |
            <!-- end-snippet -->
            """
        }

    };

    [Theory]
    [MemberData(nameof(SuccessfulTestCases))]
    public async Task SuccessfulTestsAsync(string input, (string fileName, string content)[] files, string expectedOutput)
    {
        (Result<bool> result, string output) = await ProcessAsync(input, files);

        bool actualResult = Assert.IsType<Result<bool>.Success>(result).Value;

        if (input.Equals(expectedOutput, StringComparison.Ordinal))
        {
            Assert.False(actualResult, "Processor indicated changes, but output is identical to input.");
        }
        else
        {
            Assert.True(actualResult, "Processor indicated no changes, but output differs from input.");
            Assert.Equal(expectedOutput, output);
        }
    }

    private static async Task<(Result<bool> result, string output)> ProcessAsync(string input, (string fileName, string content)[] files)
    {
        FakeFileSystem fileSystem = new();
        foreach (var (fileName, content) in files)
        {
            fileSystem.AddFile(fileName, content);
        }

        ServiceCollection services = new();
        services.AddSingleton<IFileSystem>(fileSystem);
        services.AddSingleton<MarkdownProcessor>();
        services.AddSnippetImporters();
        services.AddSnippetFormatters();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        MarkdownProcessor processor = serviceProvider.GetRequiredService<MarkdownProcessor>();
        StringReader reader = new(input);
        StringWriter writer = new();

        Result<bool> result = await processor.ProcessMarkdownAsync(reader, writer);
        string output = writer.ToString().TrimEnd('\r', '\n');

        return (result, output);
    }

    #region MarkdownProcessorTest
    public string Test()
    {
        return string.Empty;
    }
    #endregion
}
