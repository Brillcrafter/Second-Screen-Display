# WPF → Avalonia Migration Notes

## Files changed

| Original | New | Action |
|---|---|---|
| `BrillcrafterSSD.csproj` | `BrillcrafterSSD.csproj` | Rewritten (SDK-style, net6.0, Avalonia packages) |
| `resources/SecondWindow.xaml` | `resources/SecondWindow.axaml` | Namespace swap, same Canvas layout |
| `resources/SecondWindow.xaml.cs` | `resources/SecondWindow.axaml.cs` | Static methods → instance methods, Avalonia types |
| `SecondWindow_g.cs` | *(deleted)* | Auto-generated WPF boilerplate, not needed in Avalonia |
| `App.config` | *(deleted)* | .NET Framework assembly redirects, not needed on .NET 6+ |
| `WindowsThreadManager.cs` | `WindowsThreadManager.cs` | One shared Avalonia UI thread replaces per-window STA threads |
| `WindowThreadsInter.cs` | `WindowThreadsInter.cs` | `window.Dispatcher.BeginInvoke` → `Dispatcher.UIThread.InvokeAsync` |
| `HudLCDPatch.cs` | `HudLCDPatch.cs` | `System.Windows.Media.Color` → `Avalonia.Media.Color` |
| `SSDApplication.cs` | `SSDApplication.cs` | **New file** — Avalonia Application bootstrap |
| All other files | unchanged | `Config.cs`, `Plugin.cs`, `SessionComp.cs`, Settings/* |

---

## The key architectural change: threading

### WPF (original)
- Each `SecondWindow` ran on its **own STA thread** with its own `Dispatcher.Run()` loop.
- Static methods on `SecondWindow` used `[ThreadStatic]` storage so each thread's call
  landed on its own window instance.
- Dispatch looked like: `window.Dispatcher.BeginInvoke(() => SecondWindow.SomeStaticMethod(...))`

### Avalonia (new)
- A **single background thread** (`SSD-Avalonia-UI`) hosts Avalonia's entire event loop.
- Static methods become **instance methods** on `SecondWindow`.
- Dispatch looks like: `Dispatcher.UIThread.InvokeAsync(() => window.InstanceMethod(...))`
- `WindowsThreadManager.EnsureAvaloniaStarted()` uses a `ManualResetEventSlim` to block
  the game thread until `AppBuilder.SetupWithoutStarting()` completes, so the first
  `InvokeAsync` is always safe.

---

## Font loading

The custom TTF is now an `<AvaloniaResource>` (embedded in the assembly) rather than
`<Content CopyToOutputDirectory>`. In `SecondWindow.axaml.cs` it is referenced by:

```csharp
new FontFamily("avares://SecondScreenDisplay/resources#BigBlueTerm Plus Nerd Font Mono")
```

The part after `#` is the font *family name* as reported by the .ttf file itself.
If the window renders with a fallback font, check the family name with a tool like
`fc-query BigBlueTermPlusNerdFontMono-Regular.ttf` on Linux.

---

## Build requirements

- **.NET 6.0 SDK** or later (dotnet 8 recommended)
- Avalonia 11.x NuGet packages (pulled automatically by the new csproj)
- The `$(Bin64)` MSBuild property must point to your SE `Bin64` directory, e.g.
  set it in `Directory.Build.props` or your IDE's build properties:

```xml
<!-- Directory.Build.props (place next to the .csproj) -->
<Project>
  <PropertyGroup>
    <Bin64>C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Bin64</Bin64>
    <!-- Linux example: -->
    <!-- <Bin64>/home/user/.steam/steam/steamapps/common/SpaceEngineers/Bin64</Bin64> -->
    <SEBin64>$(Bin64)</SEBin64>
  </PropertyGroup>
</Project>
```

---

## Platform notes

| Platform | Avalonia backend used by `UsePlatformDetect()` |
|---|---|
| Windows | Win32 |
| Linux (X11) | X11 |
| Linux (Wayland) | Wayland |
| macOS | macOS (bonus — it just works) |

No code changes are needed to switch platforms; `UsePlatformDetect()` handles it.
