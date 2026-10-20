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
    private const uint WmSetText = 0x000C;
    private const uint WmCommand = 0x0111;
    private const uint DmGetDefaultId = 0x0400;
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

    internal static void Confirm(nint window) => ClickDialogButton(window, 1);

    internal static void Cancel(nint window) => ClickDialogButton(window, 2);

    internal static void SetSaveFileName(nint window, string path)
    {
        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed < TimeSpan.FromSeconds(5))
        {
            var edits = Children(window).Where(child => ClassName(child) == "Edit" && IsWindowVisible(child)).ToArray();
            // IFileDialog usa 1001; o diálogo clássico usa edt1 (1152).
            // O nome padrão é uma alternativa precisa se o Windows mudar o ID.
            var filename = edits.FirstOrDefault(child => GetDlgCtrlID(child) is 1001 or 1152);
            if (filename == 0)
                filename = edits.FirstOrDefault(child => ReadText(child).StartsWith("sonora-vendas-", StringComparison.Ordinal));
            if (filename != 0)
            {
                if (SendTextTimeout(filename, WmSetText, 0, path, 3, 2000, out _) == 0 || ReadText(filename) != path)
                    throw new InvalidOperationException("O diálogo de salvar não aceitou o caminho temporário do CSV.");
                return;
            }
            Thread.Sleep(25);
        }
        throw new InvalidOperationException($"Campo de nome do arquivo não encontrado: {Describe(window)}");
    }

    private static void ClickDialogButton(nint window, int identifier)
    {
        var deadline = Stopwatch.StartNew();
        nint button = 0;
        while (deadline.Elapsed < TimeSpan.FromSeconds(4))
        {
            var buttons = Children(window).Where(child => ClassName(child).Contains("button", StringComparison.OrdinalIgnoreCase)
                && IsWindowVisible(child) && IsWindowEnabled(child)).ToArray();
            button = buttons.FirstOrDefault(child => GetDlgCtrlID(child) == identifier);
            if (button == 0 && identifier == 1 &&
                SendNumberTimeout(window, DmGetDefaultId, 0, 0, 3, 500, out var defaultId) != 0 &&
                (((long)defaultId >> 16) & 0xffff) == 0x534B)
                button = buttons.FirstOrDefault(child => GetDlgCtrlID(child) == ((long)defaultId & 0xffff));
            if (button == 0)
            {
                var labels = identifier == 1 ? new[] { "OK", "Save", "Salvar" } : new[] { "Cancel", "Cancelar" };
                button = buttons.FirstOrDefault(child => labels.Contains(ReadText(child).Replace("&", "").Trim(), StringComparer.OrdinalIgnoreCase));
            }
            if (button != 0) break;
            Thread.Sleep(25);
        }
        Console.WriteLine($"DIALOG controls: {DescribeControls(window)}");
        if (button == 0)
            throw new InvalidOperationException($"Botão de {(identifier == 1 ? "aceitação" : "cancelamento")} não encontrado: {Describe(window)}");

        var posted = PostMessage(button, BmClick, 0, 0);
        var postError = Marshal.GetLastWin32Error();
        Console.WriteLine($"DIALOG BM_CLICK hwnd=0x{button:X} id={GetDlgCtrlID(button)} posted={posted} error={postError}");
        if (posted && WaitUntilHidden(window, TimeSpan.FromSeconds(2))) return;

        // BN_CLICKED/WM_COMMAND é a notificação enviada pelo botão ao diálogo.
        // Preserva a aceitação/cancelamento real; não força EndDialog nem WM_CLOSE.
        var parent = GetParent(button);
        var command = (nint)(GetDlgCtrlID(button) & 0xffff);
        var sent = SendNumberTimeout(parent, WmCommand, command, button, 3, 2000, out _) != 0;
        var sendError = Marshal.GetLastWin32Error();
        Console.WriteLine($"DIALOG WM_COMMAND id={command} sent={sent} error={sendError}");
        if (WaitUntilHidden(window, TimeSpan.FromSeconds(2))) return;
        throw new InvalidOperationException($"O botão nativo não concluiu o diálogo (postError={postError}, sendError={sendError}). {DescribeControls(window)} | {Describe(window)}");
    }

    internal static Size ActualWindowSize(nint window)
    {
        if (!GetWindowRect(window, out var bounds)) throw new InvalidOperationException("GetWindowRect falhou.");
        return new Size(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
    }

    internal static Size ActualClientSize(nint window)
    {
        if (!GetClientRect(window, out var bounds)) throw new InvalidOperationException("GetClientRect falhou.");
        return new Size(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
    }

    internal static void EnsureClientSize(Form window, Size requested)
    {
        // No desktop reduzido do CI, o setter gerenciado pode manter o cliente
        // em cache enquanto o HWND foi limitado. Corrige o HWND com sua moldura real.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var client = ActualClientSize(window.Handle);
            var outer = ActualWindowSize(window.Handle);
            if (client == requested && outer == window.Size) return;
            var width = requested.Width + outer.Width - client.Width;
            var height = requested.Height + outer.Height - client.Height;
            if (!SetWindowPos(window.Handle, 0, 0, 0, width, height, 0x0016))
                throw new InvalidOperationException($"SetWindowPos falhou ao ajustar o cliente: erro {Marshal.GetLastWin32Error()}.");
            Application.DoEvents();
            window.PerformLayout();
            window.Update();
            Application.DoEvents();
        }
        // ExpectWindowSize mantém a verificação estrita e registra os tamanhos reais.
    }

    private static bool WaitUntilHidden(nint window, TimeSpan timeout)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < timeout)
        {
            if (!IsWindowVisible(window)) return true;
            Thread.Sleep(25);
        }
        return !IsWindowVisible(window);
    }

    private static string DescribeControls(nint window) => string.Join(" | ", Children(window)
        .Where(child => ClassName(child).Contains("button", StringComparison.OrdinalIgnoreCase))
        .Select(child => $"hwnd=0x{child:X}, class={ClassName(child)}, id={GetDlgCtrlID(child)}, visible={IsWindowVisible(child)}, enabled={IsWindowEnabled(child)}, text={ReadText(child)}"));

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

    private static string ClassName(nint window)
    {
        var name = new StringBuilder(256);
        GetClassName(window, name, name.Capacity);
        return name.ToString();
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
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(nint window);
    [DllImport("user32.dll")]
    private static extern nint GetParent(nint window);
    [DllImport("user32.dll")]
    private static extern nint GetDlgItem(nint dialog, int item);
    [DllImport("user32.dll")]
    private static extern int GetDlgCtrlID(nint window);
    [DllImport("user32.dll", EntryPoint = "GetClassNameW", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(nint window, StringBuilder name, int count);
    [DllImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint window, uint message, nint parameter, nint value);
    [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", CharSet = CharSet.Unicode)]
    private static extern nint SendMessageTimeout(nint window, uint message, nint parameter, StringBuilder text,
        uint flags, uint timeout, out nint result);
    [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", CharSet = CharSet.Unicode)]
    private static extern nint SendTextTimeout(nint window, uint message, nint parameter, string text,
        uint flags, uint timeout, out nint result);
    [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", SetLastError = true)]
    private static extern nint SendNumberTimeout(nint window, uint message, nint parameter, nint value,
        uint flags, uint timeout, out nint result);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out WindowRectangle bounds);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(nint window, out WindowRectangle bounds);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint window, nint insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PrintWindow(nint window, nint context, uint flags);
}
