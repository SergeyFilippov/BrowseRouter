using System.Text.RegularExpressions;

namespace BrowseRouter;

public class UrlPreference
{
  private string? _urlPattern;

  /// <summary>
  /// The raw pattern string from config.ini (e.g. "*.github.com", "/regex/", "?query?").
  /// Setting this also compiles the cached regex.
  /// </summary>
  public string? UrlPattern
  {
    get => _urlPattern;
    set
    {
      _urlPattern = value;
      _compiledPattern = value != null ? BuildRegex(value) : null;
    }
  }

  public Browser? Browser { get; set; }

  // Compiled once; null only when UrlPattern is null.
  private Regex? _compiledPattern;

  /// <summary>
  /// Returns the subject string from <paramref name="uri"/> appropriate for this pattern mode,
  /// and the pre-compiled <see cref="Regex"/> to match against it.
  /// </summary>
  public (string subject, Regex regex) GetSubjectAndRegex(Uri uri)
  {
    string pattern = _urlPattern ?? string.Empty;
    string subject = pattern.StartsWith("/") && pattern.EndsWith("/")
      ? uri.Authority + uri.AbsolutePath           // regex mode: host + path
      : pattern.StartsWith("?") && pattern.EndsWith("?")
        ? uri.Authority + uri.PathAndQuery          // query mode: host + path + query
        : uri.Authority;                            // domain-only mode

    return (subject, _compiledPattern!);
  }

  /// <summary>
  /// Returns the subject string from <paramref name="windowTitle"/> appropriate for this pattern mode,
  /// and the pre-compiled <see cref="Regex"/> to match against it.
  /// </summary>
  public (string subject, Regex regex) GetSubjectAndRegex(string windowTitle)
  {
    return (windowTitle, _compiledPattern!);
  }

  public override string ToString() => $"\"{UrlPattern}\" => {Browser}";

  // --- Static helpers ---

  private static Regex BuildRegex(string urlPattern)
  {
    const RegexOptions opts = RegexOptions.Compiled | RegexOptions.CultureInvariant;

    if (urlPattern.StartsWith("/") && urlPattern.EndsWith("/"))
    {
      // Raw regex between slashes — use as-is.
      string inner = urlPattern.Substring(1, urlPattern.Length - 2);
      return new Regex(inner, opts);
    }

    if (urlPattern.StartsWith("?") && urlPattern.EndsWith("?"))
    {
      // Query-wildcard mode: strip delimiters, escape, restore *.
      string inner = urlPattern.Substring(1, urlPattern.Length - 2);
      string escaped = $"^{Regex.Escape(inner).Replace("\\*", ".*")}$";
      return new Regex(escaped, opts);
    }

    // Domain-wildcard mode.
    string domainEscaped = $"^{Regex.Escape(urlPattern).Replace("\\*", ".*")}$";
    return new Regex(domainEscaped, opts);
  }
}
