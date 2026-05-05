using System.Diagnostics;

namespace BrowseRouter;

public class BrowserService
{
  private readonly IConfigService config;
  private readonly HistoryService history;

  public BrowserService(IConfigService config, HistoryService history)
  {
    this.config = config;
    this.history = history;
  }

  public void Launch(string url)
  {
    try
    {
      Log.Write($"Attempting to launch \"{url}\"");
      history.RecordUrl(url);

      IEnumerable<UrlPreference> urlPreferences = config.GetUrlPreferences("urls");
      IEnumerable<UrlPreference> sourcePreferences = config.GetUrlPreferences("sources");
      Uri uri = UriFactory.Get(url);

      UrlPreference? pref = null;
      
      if (urlPreferences.TryGetPreference(uri, out UrlPreference urlPref))
      {
        Log.Write($"Found URL preference {urlPref}");
        pref = urlPref;
      }

      if (pref == null)
      {
        Log.Write($"Unable to find a browser matching \"{url}\".");
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
