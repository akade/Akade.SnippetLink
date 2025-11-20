using System.Collections.Specialized;
using System.Web;

namespace Akade.SnippetLink;

internal sealed record class Options
{
    private readonly NameValueCollection _queryParams;

    public Options(string queryString)
    {
        _queryParams = HttpUtility.ParseQueryString(queryString);
    }

    public T Get<T>(string key, T defaultValue)
    {
        string? value = _queryParams[key];
        if (value is null)
            return defaultValue;
        return (T)Convert.ChangeType(value, typeof(T));
    }
}
