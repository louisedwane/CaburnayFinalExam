using System;
using System.IO;

namespace DwaneEvents.Utils
{
    public static class StorageInitializer
    {
        public static (string dataFile, string auditFile, string reportsDir) Initialize(string baseDir)
        {
            var dataDir    = Path.Combine(baseDir, "data");
            var logsDir    = Path.Combine(baseDir, "logs");
            var reportsDir = Path.Combine(baseDir, "reports");

            foreach (var dir in new[] { dataDir, logsDir, reportsDir })
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    Console.WriteLine($"  [INIT] Created directory: {dir}");
                }
            }

            var dataFile  = Path.Combine(dataDir,  "registrations.dat");
            var auditFile = Path.Combine(logsDir,  "audit.log");

            // Touch files so they exist
            if (!File.Exists(dataFile))
            {
                File.WriteAllText(dataFile, string.Empty);
                Console.WriteLine($"  [INIT] Created data file: {dataFile}");
            }
            if (!File.Exists(auditFile))
            {
                File.WriteAllText(auditFile, $"# Dwane's Events Audit Log — created {DateTime.Now:O}{Environment.NewLine}");
                Console.WriteLine($"  [INIT] Created audit file: {auditFile}");
            }

            return (dataFile, auditFile, reportsDir);
        }
    }
}
