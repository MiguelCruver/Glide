using System.Diagnostics;
using System.Timers;

namespace Glide.App.Services;

public record PerformanceSnapshot(
    double ProcessRamMb,
    double TotalRamMb,
    double CpuUsagePercent,
    int ThreadsCount,
    TimeSpan Uptime,
    string RamSummary,
    string CpuSummary,
    string UptimeSummary
);

public class PerformanceMetricsService : IDisposable
{
    private static PerformanceMetricsService? _instance;
    public static PerformanceMetricsService Instance => _instance ??= new PerformanceMetricsService();

    private readonly System.Timers.Timer _timer;
    private readonly Process _currentProcess;
    private TimeSpan _lastCpuTime = TimeSpan.Zero;
    private DateTime _lastCheckTime = DateTime.UtcNow;
    private bool _isDisposed = false;

    public event Action<PerformanceSnapshot>? SnapshotUpdated;

    public PerformanceSnapshot CurrentSnapshot { get; private set; }

    public PerformanceMetricsService()
    {
        _currentProcess = Process.GetCurrentProcess();
        _lastCpuTime = _currentProcess.TotalProcessorTime;
        _lastCheckTime = DateTime.UtcNow;

        CurrentSnapshot = CaptureSnapshot();

        _timer = new System.Timers.Timer(1200);
        _timer.Elapsed += (s, e) =>
        {
            var snap = CaptureSnapshot();
            CurrentSnapshot = snap;
            SnapshotUpdated?.Invoke(snap);
        };
        _timer.AutoReset = true;
        _timer.Start();
    }

    public PerformanceSnapshot CaptureSnapshot()
    {
        _currentProcess.Refresh();

        double ramMb = _currentProcess.WorkingSet64 / (1024.0 * 1024.0);

        // Soma memória de processos WebView2 filhos se existirem
        double webViewRamMb = 0;
        try
        {
            var webViewProcesses = Process.GetProcessesByName("msedgewebview2");
            foreach (var wp in webViewProcesses)
            {
                try
                {
                    webViewRamMb += wp.WorkingSet64 / (1024.0 * 1024.0);
                }
                catch {}
            }
        }
        catch {}

        double totalRamMb = ramMb + (webViewRamMb > 0 ? webViewRamMb : 0);

        // Cálculo de CPU
        var now = DateTime.UtcNow;
        var currentCpuTime = _currentProcess.TotalProcessorTime;
        var deltaCpu = (currentCpuTime - _lastCpuTime).TotalMilliseconds;
        var deltaTime = (now - _lastCheckTime).TotalMilliseconds;

        double cpuPercent = 0.0;
        if (deltaTime > 200)
        {
            cpuPercent = (deltaCpu / (deltaTime * Environment.ProcessorCount)) * 100.0;
            cpuPercent = Math.Clamp(cpuPercent, 0.0, 100.0);
            _lastCpuTime = currentCpuTime;
            _lastCheckTime = now;
        }

        int threads = _currentProcess.Threads.Count;
        TimeSpan uptime = DateTime.Now - _currentProcess.StartTime;

        string ramSummary = webViewRamMb > 0
            ? $"{ramMb:F0} MB App • {totalRamMb:F0} MB Total (com WebView)"
            : $"{ramMb:F0} MB (Processo Nativo)";

        string cpuSummary = $"{cpuPercent:F1}% CPU • {threads} Threads";
        string uptimeSummary = uptime.TotalHours >= 1 
            ? $"{(int)uptime.TotalHours}h {uptime.Minutes}m"
            : $"{uptime.Minutes}m {uptime.Seconds}s";

        return new PerformanceSnapshot(
            ProcessRamMb: ramMb,
            TotalRamMb: totalRamMb,
            CpuUsagePercent: cpuPercent,
            ThreadsCount: threads,
            Uptime: uptime,
            RamSummary: ramSummary,
            CpuSummary: cpuSummary,
            UptimeSummary: uptimeSummary
        );
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _timer.Stop();
        _timer.Dispose();
    }
}

