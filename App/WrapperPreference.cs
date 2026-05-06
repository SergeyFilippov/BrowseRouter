namespace BrowseRouter;

public class WrapperPreference
{
  /// <summary>
  /// A ?…? URL pattern that identifies a wrapper/safe-link URL.
  /// </summary>
  public string? UrlPattern { get; set; }

  /// <summary>
  /// The query-string parameter name whose value is the real (wrapped) URL.
  /// </summary>
  public string? ParamName { get; set; }

  public override string ToString() => $"\"{UrlPattern}\" => param:{ParamName}";
}
