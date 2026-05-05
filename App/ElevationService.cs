using System.Runtime.Versioning;
using System.Security.Principal;

namespace BrowseRouter;

public class ElevationService
{
  [SupportedOSPlatform("windows")]
  public void RequireAdmin()
  {
    if (Environment.OSVersion.Platform != PlatformID.Win32NT)
    {
      throw new PlatformNotSupportedException();
    }
    
    WindowsPrincipal principal = new(WindowsIdentity.GetCurrent());
    if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
    {
      Log.Write($"{nameof(BrowseRouter)} needs elevated privileges. Try to run it as admin.");
      Environment.Exit(-1);
    }
  }
}
