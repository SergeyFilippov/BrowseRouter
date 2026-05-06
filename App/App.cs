namespace BrowseRouter;

public static class App
{
  private static string? _exePath;

  public static string ExePath
  {
    get
    {
      if (_exePath != null)
        return _exePath;

      // Environment.ProcessPath is available from .NET 6+ and is a direct native call —
      // no reflection, no string manipulation needed.
      _exePath = Environment.ProcessPath;

      if (!string.IsNullOrEmpty(_exePath))
        return _exePath;

      // Fallback for edge cases (e.g. single-file publish on older hosts).
      var dir = AppDomain.CurrentDomain.BaseDirectory;
      _exePath = Path.Combine(dir, AppDomain.CurrentDomain.FriendlyName + ".exe");
      return _exePath;
    }
  }
}
