# WebView2 `ShowPrintUI(System)` reproducer

Two minimal apps that call `CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System)` on a
short page, from a visible WebView2 and from a hidden 1×1 one:

| Folder | Host | What it shows |
|---|---|---|
| `winui/` | WinUI 3 (Windows App SDK 2.5.1, unpackaged) | the dialog opens **behind** the app window and is not modal to it; no print preview |
| `winforms/` | WinForms (.NET 10) | control case: the dialog opens in front and is modal to the form; no print preview |

After each press, the app's status line reports, four seconds later, which window is in the
foreground, whether a print dialog is above the app window in the z-order, and whether the app
window still takes input (a modal dialog disables its owner).

## Run

```powershell
# WinUI 3 (use -p:Platform=x64 -r win-x64 on an x64 machine)
cd winui
dotnet build -c Debug -p:Platform=ARM64 -r win-arm64
.\bin\ARM64\Debug\net10.0-windows10.0.26100.0\win-arm64\PrintReproWinUI.exe

# WinForms
cd winforms
dotnet run
```

Press **Print (visible WebView2)** with the mouse.

## Observed (Windows 11 26200, WebView2 Runtime 154.0.4258.48, arm64)

- WinUI 3: `Foreground: "WebView2 print repro (WinUI 3)". No print dialog above this window: it is
  behind it, or did not open. This window takes input: True.` The dialog ("Microsoft Edge WebView2 -
  Print") is open behind the app window. Its modal owner is an ownerless top-level
  `Chrome_WidgetWin_1` window of the WebView2 browser process, which is disabled while the app
  window is not. The same happens with the hidden 1×1 WebView2.
- WinForms: `Foreground: "Microsoft Edge WebView2 - Print". The print dialog is ABOVE this form.
  This form takes input: False.`
- Both: the dialog's preview pane reads "This app doesn't support print preview".

`screenshots/` holds one capture of each.
