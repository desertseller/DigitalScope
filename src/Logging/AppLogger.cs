using System.IO;

namespace DigitalScope.Core;

public static class AppLogger
{
    private static readonly object _sync = new();
    private static StreamWriter?   _writer;
    private static bool _initialised;

    public static void Initialise()
    {
        lock (_sync)
        {
            if (_initialised) return;
            _initialised = true;
        }

        try
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DigitalScope");
            Directory.CreateDirectory(appData);
            var logPath = Path.Combine(appData, "log.txt");

            _writer = new StreamWriter(logPath, append: false, encoding: System.Text.Encoding.UTF8)
            {
                AutoFlush = true,
            };

            Info($"{AppSettings.AppName} {AppSettings.AppVersion} started");
            Info($"Log file: {logPath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Logger] Cannot open log file: {ex.Message}");
        }
    }

    public static void Info(string message)  => Write("INFO ", message);
    public static void Warn(string message)  => Write("WARN ", message);
    public static void Error(string message) => Write("ERROR", message);
    public static void Debug(string message) => Write("DEBUG", message);

    private static void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";

        lock (_sync)
        {
            Console.WriteLine(line);
            try { _writer?.WriteLine(line); }
            catch { }
        }
    }

    public static void Close()
    {
        lock (_sync)
        {
            try { _writer?.Close(); }
            catch { }
            _writer = null;
        }
    }
}
