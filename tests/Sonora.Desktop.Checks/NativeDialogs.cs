using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace Sonora.Desktop.Checks;

internal sealed record DialogStep(string Title, Action<nint> Handle);

internal static class NativeDialogs
{
    private static readonly TimeSpan DialogTimeout = TimeSpan.FromSeconds(12);
    private const uint WmClose = 0x0010;
    private const uint WmGetText = 0x000D;
    private const uint BmClick = 0x00F5;

    internal static void Run(Action trigger, params DialogStep[] steps)
    {
        var originalWindows = VisibleWindows().Select(window => window.Handle).ToHashSet();
        var observed = new ConcurrentQueue<string>();
        Exception? failure = null;
        using var cancel = new CancellationTokenSource();
        // A thread de automação também pode falhar ao fechar um diálogo. Este limite
        // independente impede que um modal bloqueie indefinidamente o job do Windows.
        using var watchdog = new System.Threading.Timer(_ =>
        {
            Console.Error.WriteLine($"FAIL: prazo máximo dos diálogos excedido. Detectados: {string.Join(" | ", observed)}");
            Environment.Exit(1);
        }, null, TimeSpan.FromSeconds(steps.Length * DialogTimeout.TotalSeconds + 8), Timeout.InfiniteTimeSpan);
        var worker = new Thread(() =>
        {
            try
            {
                foreach (var step in steps)
                {
                    var window = WaitForWindow(step.Title, observed, cancel.Token);
                    observed.Enqueue(Describe(window));
                    Console.WriteLine($"DIALOG {step.Title}");
                    step.Handle(window);
                }
            }
            catch (Exception error)
            {
                failure = error;
                foreach (var window in VisibleWindows().Where(window => !originalWindows.Contains(window.Handle)))
                    PostMessage(window.Handle, WmClose, 0, 0);
            }
        }) { IsBackground = true, Name = "Sonora modal checks" };
        worker.Start();
        try
        {
            trigger();
            if (!worker.Join(TimeSpan.FromSeconds(steps.Length * DialogTimeout.TotalSeconds + 1)))
                throw new TimeoutException("A automação dos diálogos não terminou no prazo.");
            if (failure is not null)
                throw new InvalidOperationException($"Falha no diálogo. Detectados: {string.Join(" | ", observed)}", failure);
        }
        finally
        {
            cancel.Cancel();
        }
    }

    internal static string Describe(nint window) => string.Join(" | ",
        new[] { ReadText(window) }.Concat(Children(window).Select(ReadText)).Where(text => !string.IsNullOrWhiteSpace(text)));

    internal static void Confirm(nint window)
    {
        var button = GetDlgItem(window, 1); // IDOK pertence ao MessageBox, independente do idioma do Windows.
        if (button == 0 || !IsWindowVisible(button) || !PostMessage(button, BmClick, 0, 0))
            throw new InvalidOperationException($"Não foi possível confirmar o diálogo: {Describe(window)}");
    }

    internal static void Capture(nint window, string? destination)
    {
        if (destination is null) return;
        if (!GetWindowRect(window, out var bounds)) throw new InvalidOperationException("Não foi possível medir o diálogo nativo.");
        using var bitmap = new Bitmap(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            var context = graphics.GetHdc();
            try
            {
                if (!PrintWindow(window, context, 2)) throw new InvalidOperationException("PrintWindow não capturou o diálogo nativo.");
            }
            finally { graphics.ReleaseHdc(context); }
        }
        bitmap.Save(destination, ImageFormat.Png);
    }

    private static nint WaitForWindow(string title, ConcurrentQueue<string> observed, CancellationToken cancel)
    {
        var deadline = Stopwatch.StartNew();
        var titles = new HashSet<string>();
        while (deadline.Elapsed < DialogTimeout)
        {
            cancel.ThrowIfCancellationRequested();
            foreach (var window in VisibleWindows())
            {
                if (titles.Add(window.Title)) observed.Enqueue(window.Title);
                if (window.Title == title) return window.Handle;
            }
            Thread.Sleep(25);
        }
        throw new TimeoutException($"O diálogo '{title}' não apareceu em {DialogTimeout.TotalSeconds} segundos.");
    }

    private static List<(nint Handle, string Title)> VisibleWindows()
    {
        var windows = new List<(nint, string)>();
        EnumWindows((window, _) =>
        {
            GetWindowThreadProcessId(window, out var process);
            if (process == (uint)Environment.ProcessId && IsWindowVisible(window))
                windows.Add((window, ReadText(window)));
            return true;
        }, 0);
        return windows;
    }

    private static List<nint> Children(nint parent)
    {
        var children = new List<nint>();
        EnumChildWindows(parent, (window, _) => { children.Add(window); return true; }, 0);
        return children;
    }

    private static string ReadText(nint window)
    {
        var text = new StringBuilder(8192);
        if (SendMessageTimeout(window, WmGetText, (nint)text.Capacity, text, 3, 500, out _) == 0) return "[texto indisponível]";
        return text.ToString();
    }

    private delegate bool EnumerateWindow(nint window, nint parameter);

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowRectangle { internal int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumerateWindow callback, nint parameter);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows(nint parent, EnumerateWindow callback, nint parameter);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")]
    private static extern nint GetDlgItem(nint dialog, int item);
    [DllImport("user32.dll", EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint window, uint message, nint parameter, nint value);
    [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", CharSet = CharSet.Unicode)]
    private static extern nint SendMessageTimeout(nint window, uint message, nint parameter, StringBuilder text,
        uint flags, uint timeout, out nint result);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out WindowRectangle bounds);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PrintWindow(nint window, nint context, uint flags);
}
