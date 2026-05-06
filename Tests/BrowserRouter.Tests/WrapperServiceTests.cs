namespace BrowserRouter.Tests
{
  /// <summary>
  /// Minimal IConfigService stub used exclusively in WrapperService tests.
  /// </summary>
  internal class FakeConfigService : IConfigService
  {
    private readonly IReadOnlyList<WrapperPreference> _wrappers;

    public FakeConfigService(IEnumerable<WrapperPreference> wrappers)
    {
      _wrappers = wrappers.ToList();
    }

    public IReadOnlyList<UrlPreference> GetUrlPreferences(string configType) =>
      Array.Empty<UrlPreference>();

    public IReadOnlyList<WrapperPreference> GetWrapperPreferences() => _wrappers;

    public string? GetSetting(string key) => null;
  }

  public class WrapperServiceTests
  {
    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static WrapperService Build(params (string pattern, string param)[] rules)
    {
      var wrappers = rules.Select(r => new WrapperPreference
      {
        UrlPattern = r.pattern,
        ParamName = r.param
      });
      return new WrapperService(new FakeConfigService(wrappers));
    }

    // ------------------------------------------------------------------
    // Tests
    // ------------------------------------------------------------------

    [Test]
    public void NoWrappers_ReturnsOriginalUrl()
    {
      var svc = Build(); // no rules

      string result = svc.Unwrap("https://gitlab.example.com/some/repo");

      Assert.That(result, Is.EqualTo("https://gitlab.example.com/some/repo"));
    }

    [Test]
    public void NonWrappedUrl_ReturnsOriginalUrl()
    {
      var svc = Build(("?*teams.cdn.office.net/evergreen-assets/safelinks*?", "url"));

      string result = svc.Unwrap("https://gitlab.example.com/some/repo");

      Assert.That(result, Is.EqualTo("https://gitlab.example.com/some/repo"));
    }

    [Test]
    public void TeamsWrappedUrl_UnwrapsToRealUrl()
    {
      var svc = Build(("?*teams.cdn.office.net/evergreen-assets/safelinks*?", "url"));

      // Minimal Teams safe-link: url param holds the encoded real URL.
      string wrapped = "https://statics.teams.cdn.office.net/evergreen-assets/safelinks/2/atp-safelinks.html" +
                       "?url=https%3A%2F%2Fgitlab.example.com%2Frepo&locale=de-de";

      string result = svc.Unwrap(wrapped);

      Assert.That(result, Is.EqualTo("https://gitlab.example.com/repo"));
    }

    [Test]
    public void OutlookWrappedUrl_UnwrapsToRealUrl()
    {
      var svc = Build(("?*outlook.office.com/mail/safelink*?", "url"));

      string wrapped = "https://outlook.office.com/mail/safelink.html" +
                       "?url=https%3A%2F%2Fgitlab.example.com%2Frepo%2F-%2Fmerge_requests%2F1";

      string result = svc.Unwrap(wrapped);

      Assert.That(result, Is.EqualTo("https://gitlab.example.com/repo/-/merge_requests/1"));
    }

    [Test]
    public void ChainedWrappers_FullyUnwraps()
    {
      // Outlook wraps a Teams safe-link which itself wraps the real URL.
      var svc = Build(
        ("?*outlook.office.com/mail/safelink*?", "url"),
        ("?*teams.cdn.office.net/evergreen-assets/safelinks*?", "url")
      );

      // Inner Teams link (URL-encoded)
      string teamsLink = "https://statics.teams.cdn.office.net/evergreen-assets/safelinks/2/atp-safelinks.html" +
                         "?url=https%3A%2F%2Fgitlab.example.com%2Frepo";

      // Outer Outlook link wraps the Teams link
      string outlookLink = "https://outlook.office.com/mail/safelink.html" +
                           "?url=" + Uri.EscapeDataString(teamsLink);

      string result = svc.Unwrap(outlookLink);

      Assert.That(result, Is.EqualTo("https://gitlab.example.com/repo"));
    }

    [Test]
    public void MissingQueryParam_ReturnsCurrentUrl()
    {
      var svc = Build(("?*teams.cdn.office.net/evergreen-assets/safelinks*?", "url"));

      // Pattern matches but the expected param ("url") is absent.
      string wrapped = "https://statics.teams.cdn.office.net/evergreen-assets/safelinks/2/atp-safelinks.html" +
                       "?other=https%3A%2F%2Fgitlab.example.com%2Frepo";

      string result = svc.Unwrap(wrapped);

      // Should return the wrapped URL unchanged (graceful no-op).
      Assert.That(result, Is.EqualTo(wrapped));
    }

    [Test]
    public void MaxDepthLimit_StopsAt10()
    {
      // A wrapper that always matches and points to a param that re-wraps to the same URL.
      // This creates an infinite loop that must be stopped at MaxUnwrapDepth (10).
      const string cyclicUrl = "https://wrapper.example.com/safelink?dest=https%3A%2F%2Fwrapper.example.com%2Fsafelink%3Fdest%3Dhttps%253A%252F%252Fwrapper.example.com";

      var svc = Build(("?*wrapper.example.com/safelink*?", "dest"));

      // Should terminate rather than loop forever; exact result depends on chain depth
      // but must not throw or hang.
      string result = svc.Unwrap(cyclicUrl);

      // The result must be a non-null, non-empty string.
      Assert.That(result, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void RealWorldTeamsSafeLink_UnwrapsToGitlabUrl()
    {
      // Real URL from the issue description (truncated to the url= param for clarity).
      var svc = Build(("?*teams.cdn.office.net/evergreen-assets/safelinks*?", "url"));

      string realGitlab = "http://gitlab.raynet.corp:8080/rayventory/raynet-one/-/merge_requests/3346";
      string wrapped = "https://statics.teams.cdn.office.net/evergreen-assets/safelinks/2/atp-safelinks.html" +
                       "?url=" + Uri.EscapeDataString(realGitlab) +
                       "&locale=de-de&dest=https%3A%2F%2Fteams.microsoft.com%2Fsome%2Fpath";

      string result = svc.Unwrap(wrapped);

      Assert.That(result, Is.EqualTo(realGitlab));
    }

    [Test]
    public void RealWorldBigLink_UpwrapsToGitlabUrl()
    {
      var svc = Build(("?*teams.cdn.office.net/evergreen-assets/safelinks*?", "url"));

      var wrapped =
        "https://statics.teams.cdn.office.net/evergreen-assets/safelinks/2/atp-safelinks.html?url=http%3A%2F%2Fgitlab.raynet.corp%3A8080%2Frayventory%2Fraynet-one%2F-%2Fmerge_requests%2F3346&locale=de-de&dest=https%3A%2F%2Fteams.microsoft.com%2Fapi%2Fmt%2Femea%2Fbeta%2Fatpsafelinks%2Fgeturlreputationsitev2%2F&pc=%252b2HDlwpYuhDGA8wq8Uy4l2qb2NI0dIQmIpD6Gk75Ga06RnUjL1nEK7yNMNNjsbdV2KQVEM8ys8NiIP%252fEudH9A%252fL8nam3s69Bylg7jdhLAotTGQ41oqm5MEJ5%252bbrkuVzDNVWpe3bcaWUpC93NqXk30P34xThWf%252bIF4XfdPe8RW2ynY0LJjCCeIuh%252b25gSloe01arUgeyx5FasCrLaaioZefl8U%252fRrjAnzkHftRotOxW7Zz%252fQUZzjX5tzheZzJXUsf2%252fBU11%252f0Wy6EmZO8HVFjW%252f6pQf%252b7xn17mLGkK1xJwphMNHC2T436vQ5cZ5jjOAk0atQ%252fIXhJB6AJkGUlZC%252bYmTk1M88lMIK64EKXcjHgM7H2JR2EYv2IwNWfJSQ5NBk0zd4%252fDC%252fj7Z1NTQFCEpaANajqnQG%252bN7XMwbNNEHMuGvB747IlQPLy1UqADNpf7XsPqrR7VHz%252b0mglYvu5r3uw3cQMpgcMCjGlIXPcjTGnO0RgTZ7%252bwKdJYUAmo4NSIVf8hoHe9eD8udBElIMrBK03BA5sYFBeC1dFhlT0Gls%252bm4%252faiBSweUtu7Se2VFkTHjEFU%252fKW0azeu1E8mYrszHnRQd3uuhmldghMRpfEmpEkRjobyxiwaN2kllV5QBuwE4h6%252bVLAWGtmBDkgMwPa%252fbXTsjhmaby1S2nxB2UB%252f4jEYVhp8XX3MYLsKdJ2Uc6Gz%252bg0oksJfSSwaQtYkEjT66k3nLmGzB6g8oM25RP%252b1B61jDkNscTmdLfcugtELl%252fQRp8s67eRj%252fFOYkauUlapmexHf0w3ckbIYgQGP7TH9mCH9hd4tqsfLwjIw9z33oslOs61UsbKZBhvefle68rXGPyyokrghEX2b8FLh12l5gJWNoR2vJN7H1NKjak1pI6thKnKrbDlg4ne04tZqEXPbCP0VoJ3eeL7O%252fE6X5SaV3RQ%252b9icX%252byP3npiVL9HiFqSxRHM1NP4okKTjggS71oVUE3m%252btrYdfikb%252fI9d0c7Nl%252fbZTZt%252bzcaMq4%252fVa5RUehcp7aa4HQLbbeiRxPpupdcLEA2ZyJnitIZHVEjj%252bV8J7%252bmAknF8uBAbrQuFFCbkzbEKluxqZeVy%252bl8gUQLZypY5Ibhaap4NJnZpPR0NCyXNXFK8VAnyW1WcAtsHpigdSv9ZL3rV5%252bATi%252blt%252bjtN0crPjaLCPp9oNYJz3xhsNx8tpOrwZMluOXMe0zaFAQLltoargS9LeXVYhbQJxKx0lOtlMB7pFP8ctA%252bVtUbV6yiyKD60AIfvB%252fGF1jayTwn%252ft%252blfTben5o%252bFs4%252fv%252bLCJQT%252ffeBnACWOkZXtWOcMBIp3LqYxARs7QQpyiKYHGzODGdHAzP%252fLA%252fl1rwP10xjzaUv6ALeWSWTmMD1TIxogT%252bAT1NbJp7Mrot%252feHWl7IR9McotMly1sX0Q2Fmu2Avym5Ipwi08I%252fPae6nOeMwRIEeo4L1dVfO7o3HgoT3I60JPpAf1XzIegV1btzAHPQueqM7KThPmIAA%253d%253d%3B%20expires%3DWed%2C%2006%20May%202026%2013%3A52%3A56%20GMT%3B%20path%3D%2F%3B%20SameSite%3DNone%3B%20secure%3B%20httponly&wau=https%3A%2F%2FEUR02.safelinks.protection.outlook.com%2FGetUrlReputation&si=1777988765815%3B1777988765815%3B19%3Ac15fc6fd-06e3-4aab-9e49-2374f293265a_ee0679b0-50ac-45f1-93f6-cd7b7f2b79f3%40unq.gbl.spaces&sd=%7BconvId%3A%2019%3Ac15fc6fd-06e3-4aab-9e49-2374f293265a_ee0679b0-50ac-45f1-93f6-cd7b7f2b79f3%40unq.gbl.spaces%2C%20messageId%3A%201777988765815%7D&ce=prod&cv=49%2F26040401723&ssid=bba2b2a4-9ef5-4a72-bd90-14c4889e040c&ring=general&clickparams=eyJBcHBOYW1lIjoiVGVhbXMtRGVza3RvcCIsIkFwcFZlcnNpb24iOiI0OS8yNjA0MDQwMTcyMyIsIkhhc0ZlZGVyYXRlZFVzZXIiOmZhbHNlfQ==&bg=%23f0f0f0&fg=%23242424&fg2=%239092c1";
      var result = svc.Unwrap(wrapped);

      var expectedResult = "http://gitlab.raynet.corp:8080/rayventory/raynet-one/-/merge_requests/3346";

      Assert.That(result, Is.EqualTo(expectedResult));
    }
  }
}
