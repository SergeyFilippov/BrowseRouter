# BrowseRouter — Project Overview

## What It Is

BrowseRouter is a Windows "meta-browser": a lightweight proxy that registers itself as the system default browser. When any application opens a URL, Windows invokes BrowseRouter instead of a real browser. BrowseRouter inspects the URL, matches it against rules in a local INI config file, and launches the appropriate real browser. It makes no network connections and does no tracking.

> Forked from [BrowserSelector by DanTup](https://github.com/DanTup/BrowserSelector/), ported to modern .NET.

---

## Architecture

```
Windows OS  (link clicked anywhere)
      │
      ▼
BrowseRouter.exe <url>
      │
      ▼
Program.cs  ──── --register / --unregister ──► ElevationService ──► RegistryService
      │
      ▼  (URL argument)
ConfigService  (reads config.ini)
      │
      ├──► HistoryService.RecordUrl()   (optional rolling log, last 10 URLs)
      │
      └──► BrowserService.Launch(url)
                │
                ├── UriFactory.Get(url)              normalize / prepend https://
                ├── ConfigService.GetUrlPreferences() load [urls] rules
                ├── UrlPreferenceExtensions.TryGetPreference()  ← pattern engine
                │         matches URI against ordered rule list; first match wins
                │         falls back to catch-all wildcard  (* = first browser)
                └── Process.Start(exePath, args + url)
```

---

## Key Components

| File | Responsibility |
|---|---|
| `Program.cs` | Entry point; parses CLI args, wires services, dispatches to register or launch |
| `ConfigService.cs` | Hand-rolled INI parser; exposes browsers, URL preferences, and raw settings |
| `BrowserService.cs` | Orchestrates URL → browser resolution and process launch |
| `UrlPreferenceExtensions.cs` | Pattern-matching engine; converts INI patterns to .NET regexes |
| `UriFactory.cs` | Normalizes URLs; wraps bare hostnames in `https://` |
| `Executable.cs` | Splits a browser config value into an exe path + optional CLI args |
| `RegistryService.cs` | Writes/removes HKLM registry keys to register as a Windows protocol handler |
| `ElevationService.cs` | Guards registry operations with a Windows admin privilege check |
| `HistoryService.cs` | Optional rolling 10-entry URL history file |
| `Log.cs` | Timestamped append-to-file logger (enabled via `log = true` in config) |

---

## Config File Format (`config.ini`)

Located in the same directory as the executable.

```ini
[browsers]
; alias = /path/to/browser.exe  (or "quoted path" --with-args)
edge   = C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe
chrome = "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe" --new-window
ff     = C:\Program Files\Mozilla Firefox\firefox.exe

[urls]
; pattern = browser-alias   (first match wins)
*.internal.corp  = edge
*.atlassian.net  = ff
/(?:[0-9]{1,3}\.){3}[0-9]{1,3}/ = ff   ; regex: IP addresses
?*teams.cdn.office.net*?url*slack.com*? = chrome  ; query-string match
; implicit catch-all appended automatically: * = <first browser>

[settings]
log          = true          ; write BrowseRouter.log next to the exe
; history_file = history.txt ; optional rolling URL log (last 10 entries)
```

---

## URL Pattern Matching Engine

`UrlPreferenceExtensions.cs` supports three pattern modes, distinguished by their delimiters:

| Syntax | Mode | URI part matched | Example |
|---|---|---|---|
| `pattern` | Domain wildcard | `host[:port]` only | `*.github.com` |
| `/pattern/` | Full regex | `host + path` (no query) | `/^app\.corp\/.*/` |
| `?pattern?` | Query wildcard | `host + path + query string` | `?*example.com*?q=foo*?` |

In wildcard modes (`pattern` and `?pattern?`), `*` is converted to `.*` and the rest of the pattern is `Regex.Escape`d, then anchored with `^...$`.

**Rule evaluation:** The `[urls]` list is evaluated top-to-bottom; the first match wins. A catch-all `* = <first-defined browser>` is automatically appended so a browser is always selected.

**Edge case:** `*.github.com` matches `sub.github.com` but **not** the bare `github.com`. To match both, add a second rule: `github.com = ff`.

---

## Build & Install

**Prerequisites:** .NET 10 SDK, Windows, Visual Studio 2022 (optional).

```powershell
# Build
dotnet build BrowseRouter.sln

# Run tests
dotnet test

# Publish self-contained single-file win-x64 binary → App/publish/
dotnet publish App/BrowseRouter.csproj /p:PublishProfile=App/Properties/PublishProfiles/FolderProfile.pubxml

# Register as default browser (requires admin)
BrowseRouter.exe --register

# Unregister
BrowseRouter.exe --unregister
```

After `--register`, go to **Windows Settings → Default Apps** and set BrowseRouter as the default browser.

---

## Tech Stack

- **Language / Runtime:** C# on .NET 10 (`net10.0`), `WinExe` subsystem
- **Platform:** Windows only (uses `Microsoft.Win32.Registry`, `WindowsIdentity`)
- **External dependencies:** None in the main app (pure .NET BCL)
- **Tests:** NUnit 4 on .NET 10
- **Publish target:** Self-contained, single-file, `win-x64`, trimmed + ReadyToRun
- **CI:** GitHub Actions (`workflow_dispatch`), auto-bumps version, uploads artifact
