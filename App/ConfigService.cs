namespace BrowseRouter;

public interface IConfigService
{
  IReadOnlyList<UrlPreference> GetUrlPreferences(string configType);
  IReadOnlyList<WrapperPreference> GetWrapperPreferences();
  string? GetSetting(string key);
}

public class ConfigService : IConfigService
{
  /// <summary>
  /// Config lives in the same folder as the EXE, named "config.ini".
  /// </summary>
  public readonly string ConfigPath;

  // Parsed once at construction time.
  private readonly Dictionary<string, string> _settings;
  private readonly Dictionary<string, Browser> _browsers;
  private readonly IReadOnlyList<UrlPreference> _urlPreferences;
  private readonly IReadOnlyList<UrlPreference> _sourcePreferences;
  private readonly IReadOnlyList<WrapperPreference> _wrapperPreferences;

  public ConfigService()
  {
    ConfigPath = Path.Combine(Path.GetDirectoryName(App.ExePath)!, "config.ini");

    if (!File.Exists(ConfigPath))
    {
      _settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      _browsers = new Dictionary<string, Browser>(StringComparer.OrdinalIgnoreCase);
      _urlPreferences = Array.Empty<UrlPreference>();
      _sourcePreferences = Array.Empty<UrlPreference>();
      _wrapperPreferences = Array.Empty<WrapperPreference>();
      return;
    }

    // Single disk read — all subsequent accessors use in-memory data.
    var configLines = File.ReadAllLines(ConfigPath)
      .Select(l => l.Trim())
      .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith(";") && !l.StartsWith("#"))
      .ToList();

    _settings = ParseSection(configLines, "settings");

    _browsers = GetConfig(configLines, "browsers")
      .Select(SplitConfig)
      .Select(kvp => new Browser { Name = kvp.Key, Location = kvp.Value })
      .ToDictionary(b => b.Name!, StringComparer.OrdinalIgnoreCase);

    _urlPreferences = BuildUrlPreferences(configLines, "urls");
    _sourcePreferences = BuildUrlPreferences(configLines, "sources");
    _wrapperPreferences = BuildWrapperPreferences(configLines);
  }

  public string? GetSetting(string key)
  {
    _settings.TryGetValue(key, out string? value);
    return value;
  }

  public IReadOnlyList<UrlPreference> GetUrlPreferences(string configType)
  {
    if (!File.Exists(ConfigPath))
      throw new InvalidOperationException($"The config file was not found: {ConfigPath}");

    return configType switch
    {
      "urls" => _urlPreferences,
      "sources" => _sourcePreferences,
      _ => Array.Empty<UrlPreference>()
    };
  }

  public IReadOnlyList<WrapperPreference> GetWrapperPreferences() => _wrapperPreferences;

  // --- Private helpers ---

  private IReadOnlyList<UrlPreference> BuildUrlPreferences(List<string> configLines, string configType)
  {
    if (!_browsers.Any())
      return Array.Empty<UrlPreference>();

    var urls = GetConfig(configLines, configType)
      .Select(SplitConfig)
      .Where(kvp => _browsers.ContainsKey(kvp.Value))
      .Select(kvp => new UrlPreference { UrlPattern = kvp.Key, Browser = _browsers[kvp.Value] })
      .ToList();

    if (configType == "urls")
      urls.Add(new UrlPreference { UrlPattern = "*", Browser = _browsers.FirstOrDefault().Value });

    return urls;
  }

  private static IReadOnlyList<WrapperPreference> BuildWrapperPreferences(List<string> configLines)
  {
    return GetConfig(configLines, "wrappers")
      .Select(SplitConfig)
      .Select(kvp => new WrapperPreference { UrlPattern = kvp.Key, ParamName = kvp.Value })
      .ToList();
  }

  private static Dictionary<string, string> ParseSection(List<string> configLines, string sectionName)
  {
    return GetConfig(configLines, sectionName)
      .Select(SplitConfig)
      .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
  }

  private static IEnumerable<string> GetConfig(IEnumerable<string> configLines, string configName)
  {
    return configLines
      .SkipWhile(l => !l.StartsWith($"[{configName}]", StringComparison.OrdinalIgnoreCase))
      .Skip(1)
      .TakeWhile(l => !l.StartsWith("[", StringComparison.OrdinalIgnoreCase))
      .Where(l => l.Contains('='));
  }

  /// <summary>
  /// Splits a line on the first '=' (poor INI parsing).
  /// </summary>
  private static KeyValuePair<string, string> SplitConfig(string configLine)
  {
    var parts = configLine.Split(new[] { '=' }, 2);
    return new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim());
  }
}
