using Akade.SnippetLink.Importer;

namespace Akade.SnippetLink.Tests;

internal class FakeFileSystem : IFileSystem
{
    private readonly Dictionary<string, string> _files = new();

    public bool DirectoryExists(string path)
    {
        path = path.Replace('\\', '/');
        return _files.Keys.Any(f => f.StartsWith(path + "/"));
    }

    public bool FileExists(string path)
    {
        path = path.Replace('\\', '/');
        return _files.ContainsKey(path);
    }

    public Task<string> ReadAllTextAsync(string path)
    {
        path = path.Replace('\\', '/');
        if (_files.TryGetValue(path, out var content))
        {
            return Task.FromResult(content);
        }
        throw new FileNotFoundException($"File not found: {path}");
    }

    public void AddFile(string path, string content)
    {
        path = path.Replace('\\', '/');
        _files[path] = content;
    }

    public IEnumerable<string> GetDirectories(string path, string pattern)
    {
        path = path.Replace('\\', '/');

        string[] patternParts = pattern.Split('*', StringSplitOptions.RemoveEmptyEntries);
        bool startsWithWildcard = pattern.StartsWith('*');
        bool endsWithWildcard = pattern.EndsWith('*');

        var directories = new HashSet<string>();
        foreach (var filePath in _files.Keys)
        {
            if (filePath.StartsWith(path))
            {
                ReadOnlySpan<char> pathSegement = filePath.AsSpan()[(path.Length + 1)..].TrimStart('/');

                if (pathSegement.IndexOf('/') is >= 0 and int index)
                {
                    pathSegement = pathSegement[..index];
                }

                ReadOnlySpan<char> remaining = pathSegement;

                for (int i = 0; i < patternParts.Length; i++)
                {
                    int indexOfPart = remaining.IndexOf(patternParts[i]);

                    if (indexOfPart < 0)
                        continue;
                    else if (indexOfPart > 0 && i == 0 && !startsWithWildcard)
                        continue;

                    remaining = remaining[(indexOfPart + patternParts[i].Length)..];
                }

                if (remaining.Length > 0 && !endsWithWildcard)
                    continue;

                directories.Add(Path.Combine(path, pathSegement.ToString()));
            }
        }
        return directories;
    }

}
