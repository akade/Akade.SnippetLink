using Akade.SnippetLink.Importer;
using Microsoft.Extensions.DependencyInjection;

namespace Akade.SnippetLink.Tests;

public class MarkdownProcessorTests
{
    [Fact]
    public async Task No_snippet()
    {
        await Run(
            "Input markdown content",
            [],
            "Input markdown content"
        );
    }

    [Fact]
    public async Task Cs_comment_snippet_first_time()
    {
        await Run(
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
                    // begin-snippet: MySnippet
                    public class MyClass
                    {
                        public void MyMethod()
                        {
                            // method body
                        }
                    }
                    // end-snippet
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
        );
    }

    [Fact]
    public async Task Cs_region_snippet_first_time()
    {
        await Run(
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
        );
    }

    [Fact]
    public async Task Cs_region_snippet_replacement()
    {
        await Run(
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
        );
    }

    [Fact]
    public async Task Cs_method_snippet_implicit_namespace_first_time()
    {
        await Run(
            """
        Here is a code snippet:
        <!-- begin-snippet: Example.cs MyClass.HelloWorldSample -->
        ```cs
        ```
        <!-- end-snippet -->
        """,
            [
                (
                "Example.cs",
                """
                // Some C# code
                public class MyClass
                {
                    public void HelloWorldSample()
                    {
                        Console.WriteLine("Hello World");
                    }
                }
                """
            )
            ],
            """
        Here is a code snippet:
        <!-- begin-snippet: Example.cs MyClass.HelloWorldSample -->
        ```cs
        public void HelloWorldSample()
        {
            Console.WriteLine("Hello World");
        }
        ```
        <!-- end-snippet -->
        """
        );
    }

    [Fact]
    public async Task Cs_method_body_snippet_explicit_namespace_first_time()
    {
        await Run(
            """
    Here is a code snippet:
    <!-- begin-snippet: Example.cs MyClass.HelloWorldSample(importer:cs?body-only=true) -->
    ```cs
    ```
    <!-- end-snippet -->
    """,
            [
                (
            "Example.cs",
            """
            // Some C# code
            namespace SampleNamespace;
            
            public class MyClass
            {
                public void HelloWorldSample()
                {
                    Console.WriteLine("Hello World");
                }
            }
            """
        )
            ],
            """
    Here is a code snippet:
    <!-- begin-snippet: Example.cs MyClass.HelloWorldSample(importer:cs?body-only=true) -->
    ```cs
    Console.WriteLine("Hello World");
    ```
    <!-- end-snippet -->
    """
        );
    }

    [Fact]
    public async Task Cs_method_wrapped_in_preprocessor_conditional()
    {
        await Run(
            """
            Here is a code snippet:
            <!-- begin-snippet: Example.cs MyClass.DebugMethod -->
            ```cs
            ```
            <!-- end-snippet -->
            """,
            [
                (
                    "Example.cs",
                    """
                    // Some C# code
                    public class MyClass
                    {
                    #if DEBUG
                        public void DebugMethod()
                        {
                            Console.WriteLine("Debug mode");
                        }
                    #endif
                    }
                    """
                )
            ],
            """
            Here is a code snippet:
            <!-- begin-snippet: Example.cs MyClass.DebugMethod -->
            ```cs
            public void DebugMethod()
            {
                Console.WriteLine("Debug mode");
            }
            ```
            <!-- end-snippet -->
            """
        );
    }

    [Fact]
    public async Task Cs_class()
    {
        await Run(
            """
Here is a code snippet:
<!-- begin-snippet: Example.cs MyClass -->
```cs
```
<!-- end-snippet -->
""",
            [
                (
        "Example.cs",
        """
        // Some C# code
        namespace SampleNamespace;
        
        [SampleAttribute]
        public class MyClass
        {
            public void HelloWorldSample()
            {
                Console.WriteLine("Hello World");
            }
        }
        """
    )
            ],
            """
Here is a code snippet:
<!-- begin-snippet: Example.cs MyClass -->
```cs
[SampleAttribute]
public class MyClass
{
    public void HelloWorldSample()
    {
        Console.WriteLine("Hello World");
    }
}
```
<!-- end-snippet -->
"""
        );
    }

    [Fact]
    public async Task BenchmarkDotNet_result_snippet_without_env()
    {
        await Run(
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
        );
    }

    [Fact]
    public async Task BenchmarkDotNet_result_snippet_with_env()
    {
        await Run(
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
        );
    }

    private static async Task Run(string input, (string fileName, string content)[] files, string expectedOutput)
    {
        (Result<bool> result, string output) = await ProcessAsync(input, files);

        if (result is Result<bool>.Failure failure)
        {
            Assert.Fail($"Processing failed: {failure.ErrorMessage}");
        }

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
}
