using Akade.SnippetLink.Benchmarks;
using BenchmarkDotNet.Running;

var _ = BenchmarkRunner.Run(typeof(SnippetLinkBenchmarks).Assembly);


