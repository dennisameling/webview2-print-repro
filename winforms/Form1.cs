using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace PrintRepro;

// Minimal reproducer for CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System).
//
// "Print (visible WebView2)" prints the page shown in the window.
// "Print (hidden 1x1 WebView2)" prints from a second WebView2 that is one pixel square, the usual
// way to print a document that is not on screen.
//
// After each press the status line reports, a few seconds later, which window is in the
// foreground and whether the print dialog sits above this form in the z-order.
public sealed class Form1 : Form
{
    private const string Page = """
        <!DOCTYPE html><html><head><meta charset="utf-8"><title>Repro page</title></head>
        <body style="font-family:Segoe UI;margin:24px">
        <h1>WebView2 print reproducer</h1>
        <p>A short page with a heading, a paragraph and a table.</p>
        <table border="1" cellpadding="6"><tr><td>One</td><td>Two</td><td>Three</td></tr></table>
        </body></html>
        """;

    private readonly WebView2 _visible = new() { Dock = DockStyle.Fill };
    private readonly WebView2 _hidden = new() { Width = 1, Height = 1, Left = 0, Top = 0 };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(8) };

    public Form1()
    {
        Text = "WebView2 print repro";
        Width = 1000;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(6) };
        var printVisible = new Button { Text = "Print (visible WebView2)", AutoSize = true, Name = "PrintVisible" };
        var printHidden = new Button { Text = "Print (hidden 1x1 WebView2)", AutoSize = true, Name = "PrintHidden" };
        printVisible.Click += (_, _) => Print(_visible);
        printHidden.Click += (_, _) => Print(_hidden);
        buttons.Controls.Add(printVisible);
        buttons.Controls.Add(printHidden);

        Controls.Add(_visible);
        Controls.Add(_hidden);
        Controls.Add(buttons);
        Controls.Add(_status);
        _hidden.BringToFront();
        Load += async (_, _) =>
        {
            await _visible.EnsureCoreWebView2Async();
            await _hidden.EnsureCoreWebView2Async();
            _visible.CoreWebView2.NavigateToString(Page);
            _hidden.CoreWebView2.NavigateToString(Page);
            _status.Text = $"WebView2 Runtime {_visible.CoreWebView2.Environment.BrowserVersionString}. Press a Print button with the mouse.";
        };
    }

    private async void Print(WebView2 view)
    {
        view.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System);
        _status.Text = "ShowPrintUI(System) called; checking the z-order in 4 seconds...";
        await Task.Delay(4000);
        _status.Text = DescribeZOrder();
    }

    // Walks the top-level windows from the top of the z-order down until it reaches this form, and
    // reports whether a print dialog was above it.
    private string DescribeZOrder()
    {
        var report = new StringBuilder($"Foreground: \"{TitleOf(GetForegroundWindow())}\". ");
        var dialogAbove = false;
        for (var window = GetTopWindow(IntPtr.Zero); window != IntPtr.Zero; window = GetWindow(window, GW_HWNDNEXT))
        {
            if (window == Handle)
            {
                break;
            }
            if (IsWindowVisible(window) && TitleOf(window).EndsWith("Print", StringComparison.Ordinal))
            {
                dialogAbove = true;
            }
        }
        report.Append(dialogAbove
            ? "The print dialog is ABOVE this form."
            : "No print dialog above this form: it is behind it, or did not open.");
        report.Append($" This form takes input: {IsWindowEnabled(Handle)}.");
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
