using System;
using System.IO;

namespace DwaneEvents.Services
{
    public class AuditLogger
    {
        private readonly string _auditFile;
        private readonly object _lock = new();

        public AuditLogger(string auditFile)
        {
            _auditFile = auditFile;
        }

        public void Log(string action, string details, string? recordId = null)
        {
            var entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {action,-10} | " +
                        $"RecordId={recordId ?? "N/A",-20} | {details}";
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_auditFile, entry + Environment.NewLine);
                }
                catch
                {
                    // Audit failure must never crash the application
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("[WARN] Could not write to audit log.");
                    Console.ResetColor();
                }
            }
        }

        public void LogError(string context, string error)
            => Log("ERROR", $"Context={context} | Error={error}");
    }
}
