using System.Diagnostics;

namespace BrowseRouter;

public static class Log
{
  //private static readonly EventLog eventLog_ = new("Application") { Source = "Application" };
  
  private static string logFile_ => "BrowseRouter.log";
  private static ConfigService? config;
  private static bool isLoggingEnabled = false;

  public static void Write(string message)
  {
    string msg = $"{DateTime.Now} {nameof(BrowseRouter)}: {message}";
    Console.WriteLine(msg);
    TryWrite(msg);
  }

  internal static void InjectConfig(ConfigService? injectedConfig)
  {
    if (injectedConfig == null)
    {
      config = null;
    }
    else
    {
      config = injectedConfig;
      var logging = config.GetSetting("log");
      if (string.Equals(logging, "true", StringComparison.OrdinalIgnoreCase))
      {
        isLoggingEnabled = true;
      }
    }
  }

  private static void TryWrite(string message)
  {
    if (!isLoggingEnabled)
    {
      return;
    }

    string path = Path.IsPathRooted(logFile_)
      ? logFile_
      : Path.Combine(Path.GetDirectoryName(App.ExePath)!, logFile_);

    foreach (int i in Enumerable.Range(0, 10))
    {
      try
      {
        using var writer = new StreamWriter(path, append: true);
        writer.WriteLine(message);
        return;
      }
      catch (Exception)
      {
      }
    }
  }
}
