namespace BrowseRouter;

public static class UriFactory
{
  public static Uri Get(string url)
  {
    if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
      return uri;

    // Try to prepend https when given an incomplete URI e.g. "google.com"
    if (Uri.TryCreate($"https://{url}", UriKind.Absolute, out Uri? httpsUri))
      return httpsUri;

    // Fallback: let Uri constructor throw its normal exception.
    return new Uri(url);
  }
}
