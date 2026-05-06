namespace BrowseRouter;

public class HistoryService
{
  private const int MaxEntries = 10;

  private readonly IConfigService config;

  public HistoryService(IConfigService config)
  {
    this.config = config;
  }

  public void RecordUrl(string url)
  {
    // Feature is disabled when history_file is not set in [settings].
    string? setting = config.GetSetting("history_file");
    if (string.IsNullOrWhiteSpace(setting))
    {
      return;
    }

    // Resolve relative paths to the same directory as the EXE,
    // consistent with how config.ini and BrowseRouter.log are located.
    string path = Path.IsPathRooted(setting)
      ? setting
      : Path.Combine(Path.GetDirectoryName(App.ExePath)!, setting);

    try
    {
      // Read existing entries, ignoring blank lines.
      List<string> entries = File.Exists(path)
        ? File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToList()
        : new List<string>();

      // Skip consecutive duplicate URLs.
      if (entries.Count > 0 && entries[^1] == url)
      {
        return;
      }

      entries.Add(url);

      // Rotate: keep only the last MaxEntries entries.
      if (entries.Count > MaxEntries)
      {
        entries = entries.Skip(entries.Count - MaxEntries).ToList();
      }

      TryWrite(path, entries);
    }
    catch (Exception)
    {
      // Never let history failures affect browser launching.
    }
  }

  private static void TryWrite(string path, IEnumerable<string> lines)
  {
    foreach (int _ in Enumerable.Range(0, 10))
    {
      try
      {
        File.WriteAllLines(path, lines);
        return;
      }
      catch (Exception)
      {
      }
    }
  }
}
