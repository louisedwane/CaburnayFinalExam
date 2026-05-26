using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DwaneEvents.Models;
using DwaneEvents.Services;

namespace DwaneEvents.Reports
{
    public class ReportGenerator
    {
        private readonly string _reportsDir;
        private readonly AuditLogger _audit;

        public ReportGenerator(string reportsDir, AuditLogger audit)
        {
            _reportsDir = reportsDir;
            _audit      = audit;
        }

        /// <summary>
        /// Full summary report:
        ///  • Total registrations (active / inactive)
        ///  • Revenue breakdown per event
        ///  • Ticket-type distribution
        ///  • Top 5 events by registrant count
        ///  • Records with invalid checksums flagged
        /// </summary>
        public string GenerateSummaryReport(List<EventRegistration> all)
        {
            var sb       = new System.Text.StringBuilder();
            var now      = DateTime.Now;
            var active   = all.Where(r => r.IsActive).ToList();
            var inactive = all.Where(r => !r.IsActive).ToList();

            Banner(sb, "DWANE'S EVENTS — REGISTRATION SUMMARY REPORT");
            sb.AppendLine($"  Generated : {now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"  Report ID : RPT-{now:yyyyMMddHHmmss}");
            sb.AppendLine();

            // --- Overview ---
            Section(sb, "OVERVIEW");
            sb.AppendLine($"  Total Records    : {all.Count}");
            sb.AppendLine($"  Active           : {active.Count}");
            sb.AppendLine($"  Inactive/Deleted : {inactive.Count}");
            sb.AppendLine($"  Total Revenue    : PHP {active.Sum(r => r.AmountPaid):N2}");
            sb.AppendLine();

            // --- Revenue per event ---
            Section(sb, "REVENUE BY EVENT");
            var byEvent = active
                .GroupBy(r => r.EventName)
                .OrderByDescending(g => g.Sum(r => r.AmountPaid));

            Columns(sb, "Event", "Registrants", "Revenue (PHP)");
            Divider(sb);
            foreach (var g in byEvent)
                Columns(sb, g.Key, g.Count().ToString(), g.Sum(r => r.AmountPaid).ToString("N2"));
            sb.AppendLine();

            // --- Ticket type distribution ---
            Section(sb, "TICKET TYPE DISTRIBUTION");
            var byTicket = active
                .GroupBy(r => r.TicketType.ToUpper())
                .OrderByDescending(g => g.Count());

            Columns(sb, "Ticket Type", "Count", "% Share");
            Divider(sb);
            foreach (var g in byTicket)
            {
                double pct = active.Count == 0 ? 0 : g.Count() * 100.0 / active.Count;
                Columns(sb, g.Key, g.Count().ToString(), $"{pct:F1}%");
            }
            sb.AppendLine();

            // --- Top 5 events by headcount ---
            Section(sb, "TOP EVENTS BY REGISTRANTS");
            var top5 = active
                .GroupBy(r => r.EventName)
                .OrderByDescending(g => g.Count())
                .Take(5);

            int rank = 1;
            foreach (var g in top5)
                sb.AppendLine($"  #{rank++,-4} {g.Key,-40} ({g.Count()} registrant/s)");
            sb.AppendLine();

            // --- Upcoming events (future dates) ---
            Section(sb, "UPCOMING EVENTS (future dates)");
            var upcoming = active
                .Where(r => DateTime.TryParseExact(r.EventDate, "yyyy-MM-dd",
                    null, System.Globalization.DateTimeStyles.None, out var d) && d >= now.Date)
                .OrderBy(r => r.EventDate)
                .GroupBy(r => new { r.EventDate, r.EventName });

            bool anyUpcoming = false;
            foreach (var g in upcoming)
            {
                anyUpcoming = true;
                sb.AppendLine($"  {g.Key.EventDate}  {g.Key.EventName,-38} ({g.Count()} registrant/s)");
            }
            if (!anyUpcoming) sb.AppendLine("  (none)");
            sb.AppendLine();

            // --- Checksum integrity ---
            Section(sb, "DATA INTEGRITY AUDIT");
            var corrupted = all.Where(r => !r.IsChecksumValid()).ToList();
            if (corrupted.Count == 0)
            {
                sb.AppendLine("  ✓ All records passed checksum verification.");
            }
            else
            {
                sb.AppendLine($"  ⚠ {corrupted.Count} record(s) with checksum mismatch:");
                foreach (var r in corrupted)
                    sb.AppendLine($"    - RecordId={r.RecordId}  Name={r.FullName}");
            }
            sb.AppendLine();

            Banner(sb, "END OF REPORT");

            var reportText = sb.ToString();

            // Save to file
            try
            {
                var fileName = Path.Combine(_reportsDir, $"report_{now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(fileName, reportText);
                _audit.Log("REPORT", $"Summary report saved to {fileName}");
            }
            catch (Exception ex)
            {
                _audit.LogError("ReportSave", ex.Message);
            }

            return reportText;
        }

        // ------------------------------------------------------------------
        //  Formatting helpers
        // ------------------------------------------------------------------
        private static void Banner(System.Text.StringBuilder sb, string title)
        {
            var line = new string('=', 70);
            sb.AppendLine(line);
            sb.AppendLine($"  {title}");
            sb.AppendLine(line);
        }

        private static void Section(System.Text.StringBuilder sb, string title)
        {
            sb.AppendLine($"  ── {title} ──────────────────────────────────────────");
        }

        private static void Divider(System.Text.StringBuilder sb)
            => sb.AppendLine($"  {new string('-', 66)}");

        private static void Columns(System.Text.StringBuilder sb, string c1, string c2, string c3)
            => sb.AppendLine($"  {c1,-38} {c2,-14} {c3}");
    }
}
