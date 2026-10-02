using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace PrintReproWinUI;

// Minimal reproducer for CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System) in WinUI 3.
//
// Press either Print button WITH THE MOUSE. After each press the status line reports, a few seconds
// later, which window is in the foreground, whether the print dialog sits above this window in the
// z-order, and whether this window is disabled (i.e. the dialog is modal to it).
public sealed partial class MainWindow : Window
{
    private const string Page = """
        <!DOCTYPE html><html><head><meta charset="utf-8"><title>Repro page</title></head>
        <body style="font-family:Segoe UI;margin:24px">
        <h1>WebView2 print reproducer</h1>
        <p>A short page with a heading, a paragraph and a table.</p>
        <table border="1" cellpadding="6"><tr><td>One</td><td>Two</td><td>Three</td></tr></table>
        </body></html>
        """;

    public MainWindow()
    {
        InitializeComponent();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        await ShownPage.EnsureCoreWebView2Async();
        await HiddenPage.EnsureCoreWebView2Async();
        ShownPage.CoreWebView2.NavigateToString(Page);
        HiddenPage.CoreWebView2.NavigateToString(Page);
        Status.Text = $"WebView2 Runtime {ShownPage.CoreWebView2.Environment.BrowserVersionString}. Press a Print button with the mouse.";
    }

    private void OnPrintVisible(object sender, RoutedEventArgs e) => Print(ShownPage);

    private void OnPrintHidden(object sender, RoutedEventArgs e) => Print(HiddenPage);

    private async void Print(WebView2 view)
    {
        view.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System);
        Status.Text = "ShowPrintUI(System) called; checking the z-order in 4 seconds...";
        await Task.Delay(4000);
        Status.Text = DescribeZOrder();
    }

    // Walks the top-level windows from the top of the z-order down until it reaches this window,
    // and reports whether a print dialog was above it.
    private string DescribeZOrder()
    {
        var self = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var report = new StringBuilder($"Foreground: \"{TitleOf(GetForegroundWindow())}\". ");
        var dialogAbove = false;
        for (var window = GetTopWindow(IntPtr.Zero); window != IntPtr.Zero; window = GetWindow(window, GW_HWNDNEXT))
        {
            if (window == self)
            {
                break;
            }
            if (IsWindowVisible(window) && TitleOf(window).EndsWith("Print", StringComparison.Ordinal))
            {
                dialogAbove = true;
            }
        }
        report.Append(dialogAbove
            ? "The print dialog is ABOVE this window."
            : "No print dialog above this window: it is behind it, or did not open.");
        report.Append($" This window takes input: {IsWindowEnabled(self)}.");
        return report.ToString();
    }

    private static string TitleOf(IntPtr window)
    {
        var text = new StringBuilder(256);
        GetWindowText(window, text, text.Capacity);
        return text.ToString();
    }

    private const uint GW_HWNDNEXT = 2;
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetTopWindow(IntPtr parent);
    [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr window, uint command);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsWindowEnabled(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
}
