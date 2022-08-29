using System.Text.RegularExpressions;

namespace BrowseRouter;

public static class UrlPreferenceExtensions
{
  public static bool TryGetPreference(this IEnumerable<UrlPreference> prefs, Uri uri, out UrlPreference pref)
  {
    pref = prefs.FirstOrDefault(pref =>
    {
      (string domain, string pattern) = pref.GetDomainAndPattern(uri);
      return Regex.IsMatch(domain, pattern);
    })!;

    return pref != null;
  }

  public static (string, string) GetDomainAndPattern(this UrlPreference pref, Uri uri)
  {
      (string? domain, string? pattern, bool isRegex) = GetDomainAndPatternRecursive(pref, uri, false);

      if(!isRegex)
      {
          // We're only checking the domain.
          domain = domain ?? uri.Authority;

          // Escape the input for regex; the only special character we support is a *
          var regex = Regex.Escape(pattern??pref.UrlPattern);

          // Unescape * as a wildcard.
          pattern = $"^{regex.Replace("\\*", ".*")}$";
      }

      if (domain == null)
      {
          throw new ApplicationException("Domain value is invalid.");
      }

      if (pattern == null)
      {
          throw new ApplicationException("Patter value is invalid.");
      }

      return (domain, pattern);
  }

  public static (string?, string?, bool) GetDomainAndPatternRecursive(this UrlPreference pref, Uri uri, bool isRegex)
  {
    string urlPattern = pref.UrlPattern;

    string? domain = null;
    string? pattern = null;
    bool isRegexPattern = isRegex;

    bool isProcessed = false;

    if (urlPattern.StartsWith("/") && urlPattern.EndsWith("/"))
    {
        // The domain from the INI file is a regex
        domain = uri.Authority + uri.AbsolutePath;
        pattern = urlPattern.Substring(1, urlPattern.Length - 2);
        isRegexPattern = true;
        isProcessed = true;
    }

    if (urlPattern.StartsWith("?") && urlPattern.EndsWith("?"))
    {
        // The domain from the INI file is a regex
        domain = uri.Authority + uri.PathAndQuery;
        pattern = urlPattern.Substring(1, urlPattern.Length - 2);
        isProcessed = true;
    }

    if (isProcessed)
    {
        var innerResult = 
            GetDomainAndPatternRecursive(
            new UrlPreference
            {
                Browser = pref.Browser, 
                UrlPattern = pattern
            }, 
            UriFactory.Get(domain), 
            isRegexPattern);

        if (!string.Equals(innerResult.Item1, domain) || !string.Equals(innerResult.Item2, pattern))
        {
            domain = innerResult.Item1 ?? domain;
            pattern = innerResult.Item2 ?? pattern;
            isRegexPattern = innerResult.Item3;
        }
    }

    return (domain, pattern, isRegexPattern);
  }
}