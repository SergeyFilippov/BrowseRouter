using System.Diagnostics;

namespace BrowseRouter;

public class BrowserService
{
  private readonly IConfigService config;
  private readonly HistoryService history;
  private readonly WrapperService wrapper;

  public BrowserService(IConfigService config, HistoryService history, WrapperService wrapper)
  {
    this.config = config;
    this.history = history;
    this.wrapper = wrapper;
  }

  public void Launch(string url)
  {
    try
    {
      Log.Write($"Attempting to launch \"{url}\"");
      history.RecordUrl(url);

      // Unwrap safe-link / redirect wrappers before any rule matching.
      string unwrapped = wrapper.Unwrap(url);
      if (!string.Equals(unwrapped, url, StringComparison.Ordinal))
        Log.Write($"Unwrapped URL: \"{unwrapped}\"");

      IEnumerable<UrlPreference> urlPreferences = config.GetUrlPreferences("urls");
      IEnumerable<UrlPreference> sourcePreferences = config.GetUrlPreferences("sources");
      Uri uri = UriFactory.Get(unwrapped);

      UrlPreference? pref = null;
      
      if (urlPreferences.TryGetPreference(uri, out UrlPreference urlPref))
      {
        Log.Write($"Found URL preference {urlPref}");
        pref = urlPref;
      }

      if (pref == null)
      {
        Log.Write($"Unable to find a browser matching \"{unwrapped}\".");
        return;
      }

      (string path, string args) = Executable.GetPathAndArgs(pref.Browser.Location);

      Log.Write($"Launching {path} with args \"{args} {uri.OriginalString}\"");
      Process.Start(path, $"{args} {uri.OriginalString}");
    }
    catch (Exception e)
    {
      Log.Write($"{e}");
    }
  }
}

