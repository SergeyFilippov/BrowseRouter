namespace BrowseRouter;

public interface IConfigService
{
  IEnumerable<UrlPreference> GetUrlPreferences(string configType);
  IEnumerable<WrapperPreference> GetWrapperPreferences();
  string? GetSetting(string key);
}

public class ConfigService : IConfigService
{
  /// <summary>
  /// Config lives in the same folder as the EXE, name "BrowserSelector.ini".
  /// </summary>
  public readonly string ConfigPath;

  public ConfigService()
  {
    // Fix for self-contained publishing
    this.ConfigPath = Path.Combine(Path.GetDirectoryName(App.ExePath)!, "config.ini");
  }

  public string? GetSetting(string key)
  {
    if (!File.Exists(ConfigPath))
      return null;

    var configLines =
      File.ReadAllLines(ConfigPath)
      .Select(l => l.Trim())
      .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith(";") && !l.StartsWith("#"));

    return GetConfig(configLines, "settings")
      .Select(SplitConfig)
      .FirstOrDefault(kvp => string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
      .Value;
  }

  public IEnumerable<UrlPreference> GetUrlPreferences(string configType)
  {
    if (!File.Exists(ConfigPath))
      throw new InvalidOperationException($"The config file was not found: {ConfigPath}");

    // Poor mans INI file reading... Skip comment lines (TODO: support comments on other lines).
    var configLines =
      File.ReadAllLines(ConfigPath)
      .Select(l => l.Trim())
      .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith(";") && !l.StartsWith("#"));

    // Read the browsers section into a dictionary.
    var browsers = GetConfig(configLines, "browsers")
      .Select(SplitConfig)
      .Select(kvp => new Browser { Name = kvp.Key, Location = kvp.Value })
      .ToDictionary(b => b.Name);

    if (!browsers.Any())
    {
      // There weren't any configured browsers
      return new List<UrlPreference>();
    }

    // Read the url preferences
    var urls = GetConfig(configLines, configType)
      .Select(SplitConfig)
      .Select(kvp => new UrlPreference { UrlPattern = kvp.Key, Browser = browsers[kvp.Value] })
      .Where(up => up.Browser != null);

    if (configType == "urls")
      urls = urls.Union(new[] { new UrlPreference { UrlPattern = "*", Browser = browsers.FirstOrDefault().Value } }); // Add a catch-all that uses the first browser

    return urls;
  }

  public IEnumerable<WrapperPreference> GetWrapperPreferences()
  {
    if (!File.Exists(ConfigPath))
      return Enumerable.Empty<WrapperPreference>();

    var configLines =
      File.ReadAllLines(ConfigPath)
      .Select(l => l.Trim())
      .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith(";") && !l.StartsWith("#"));

    return GetConfig(configLines, "wrappers")
      .Select(SplitConfig)
      .Select(kvp => new WrapperPreference { UrlPattern = kvp.Key, ParamName = kvp.Value });
  }

  private IEnumerable<string> GetConfig(IEnumerable<string> configLines, string configName)
  {
    // Read everything from [configName] up to the next [section].
    return configLines
      .SkipWhile(l => !l.StartsWith($"[{configName}]", StringComparison.OrdinalIgnoreCase))
      .Skip(1)
      .TakeWhile(l => !l.StartsWith("[", StringComparison.OrdinalIgnoreCase))
      .Where(l => l.Contains('='));
  }

  /// <summary>
  /// Splits a line on the first '=' (poor INI parsing).
  /// </summary>
  private KeyValuePair<string, string> SplitConfig(string configLine)
  {
    var parts = configLine.Split(new[] { '=' }, 2);
    return new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim());
  }
}
