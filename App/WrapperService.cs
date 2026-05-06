using System.Text.RegularExpressions;

namespace BrowseRouter;

public class WrapperService
{
  private const int MaxUnwrapDepth = 10;

  private readonly IConfigService _config;

  public WrapperService(IConfigService config)
  {
    _config = config;
  }

  /// <summary>
  /// If the given URL matches a configured wrapper pattern, extracts and returns the real
  /// URL from the wrapper's query parameter. Repeats until no wrapper matches or the
  /// maximum depth of <see cref="MaxUnwrapDepth"/> is reached.
  /// Returns the original URL unchanged when no wrappers are configured or match.
  /// </summary>
  public string Unwrap(string url)
  {
    var wrappers = _config.GetWrapperPreferences().ToList();

    if (wrappers.Count == 0)
      return url;

    string current = url;

    for (int depth = 0; depth < MaxUnwrapDepth; depth++)
    {
      Uri uri;
      try
      {
        uri = UriFactory.Get(current);
      }
      catch
      {
        break;
      }

      WrapperPreference? matched = FindMatch(wrappers, uri);
      if (matched == null)
        break;

      string? inner = ExtractParam(uri, matched.ParamName!);
      if (inner == null)
      {
        Log.Write($"Wrapper pattern \"{matched.UrlPattern}\" matched but param \"{matched.ParamName}\" was not found in URL: {current}");
        break;
      }

      Log.Write($"Unwrapping (depth {depth + 1}): \"{matched.UrlPattern}\" param \"{matched.ParamName}\" => {inner}");
      current = inner;
    }

    return current;
  }

  private static WrapperPreference? FindMatch(IEnumerable<WrapperPreference> wrappers, Uri uri)
  {
    // Wrapper patterns use the same ?…? query-wildcard mode:
    // match against host + path + query string.
    string subject = uri.Authority + uri.PathAndQuery;

    foreach (var wrapper in wrappers)
    {
      string rawPattern = wrapper.UrlPattern ?? string.Empty;

      // Strip enclosing ? delimiters if present (canonical form), otherwise use as-is.
      string inner = (rawPattern.StartsWith("?") && rawPattern.EndsWith("?") && rawPattern.Length > 1)
        ? rawPattern.Substring(1, rawPattern.Length - 2)
        : rawPattern;

      // Escape for regex, then restore * as .* wildcard, anchor with ^…$.
      string pattern = $"^{Regex.Escape(inner).Replace("\\*", ".*")}$";

      if (Regex.IsMatch(subject, pattern))
        return wrapper;
    }

    return null;
  }

  private static string? ExtractParam(Uri uri, string paramName)
  {
    // Manually parse the query string to avoid a dependency on System.Web.
    // uri.Query starts with '?' when non-empty.
    string raw = uri.Query;
    if (string.IsNullOrEmpty(raw))
      return null;

    // Strip leading '?'
    if (raw.StartsWith("?"))
      raw = raw[1..];

    foreach (string pair in raw.Split('&'))
    {
      int eq = pair.IndexOf('=');
      if (eq < 0)
        continue;

      string key = Uri.UnescapeDataString(pair[..eq].Replace("+", " "));
      if (!string.Equals(key, paramName, StringComparison.OrdinalIgnoreCase))
        continue;

      return Uri.UnescapeDataString(pair[(eq + 1)..].Replace("+", " "));
    }

    return null;
  }
}
