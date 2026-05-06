namespace BrowseRouter;

public static class UrlPreferenceExtensions
{
  /// <summary>
  /// Finds the first preference whose compiled regex matches the given URI.
  /// </summary>
  public static bool TryGetPreference(this IReadOnlyList<UrlPreference> prefs, Uri uri, out UrlPreference pref)
  {
    foreach (var p in prefs)
    {
      var (subject, regex) = p.GetSubjectAndRegex(uri);
      if (regex.IsMatch(subject))
      {
        pref = p;
        return true;
      }
    }

    pref = null!;
    return false;
  }

  /// <summary>
  /// Finds the first preference whose compiled regex matches the given window title.
  /// </summary>
  public static bool TryGetPreference(this IReadOnlyList<UrlPreference> prefs, string windowTitle, out UrlPreference pref)
  {
    foreach (var p in prefs)
    {
      var (subject, regex) = p.GetSubjectAndRegex(windowTitle);
      if (regex.IsMatch(subject))
      {
        pref = p;
        return true;
      }
    }

    pref = null!;
    return false;
  }

  // IEnumerable overloads kept for test/external compatibility.

  /// <inheritdoc cref="TryGetPreference(IReadOnlyList{UrlPreference}, Uri, out UrlPreference)"/>
  public static bool TryGetPreference(this IEnumerable<UrlPreference> prefs, Uri uri, out UrlPreference pref)
    => TryGetPreference(prefs.ToList(), uri, out pref);

  /// <inheritdoc cref="TryGetPreference(IReadOnlyList{UrlPreference}, string, out UrlPreference)"/>
  public static bool TryGetPreference(this IEnumerable<UrlPreference> prefs, string windowTitle, out UrlPreference pref)
    => TryGetPreference(prefs.ToList(), windowTitle, out pref);

  // Legacy helpers retained so existing tests that call GetDomainAndPattern directly still compile.

  /// <summary>Returns the subject string and the pattern string (not pre-compiled) for the given URI.</summary>
  public static (string, string) GetDomainAndPattern(this UrlPreference pref, Uri uri)
  {
    var (subject, regex) = pref.GetSubjectAndRegex(uri);
    return (subject, regex.ToString());
  }

  /// <summary>Returns the subject string and the pattern string (not pre-compiled) for the given window title.</summary>
  public static (string, string) GetDomainAndPattern(this UrlPreference pref, string windowTitle)
  {
    var (subject, regex) = pref.GetSubjectAndRegex(windowTitle);
    return (subject, regex.ToString());
  }
}
