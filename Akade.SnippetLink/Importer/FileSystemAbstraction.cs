
namespace Akade.SnippetLink.Importer;

internal interface IFileSystem
{
    bool DirectoryExists(string releasePath);
    bool FileExists(string path);
    IEnumerable<string> GetDirectories(string path, string pattern);
    Task<string> ReadAllTextAsync(string path);
}

internal sealed class RealFileSystem : IFileSystem
{
    public bool DirectoryExists(string releasePath)
    {
        return Directory.Exists(releasePath);
    }

    public bool FileExists(string path) => File.Exists(path);
    
    public IEnumerable<string> GetDirectories(string path, string pattern) => Directory.GetDirectories(path, pattern);

    public Task<string> ReadAllTextAsync(string path) => File.ReadAllTextAsync(path);
}
