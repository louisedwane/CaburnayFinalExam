using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DwaneEvents.Models;
using DwaneEvents.Repositories;
using DwaneEvents.Reports;
using DwaneEvents.Services;
using DwaneEvents.Utils;

namespace DwaneEvents
{
    class Program
    {
        // ── Singletons ────────────────────────────────────────────────────
        static AuditLogger        _audit   = null!;
        static RegistrationRepository _repo = null!;
        static ReportGenerator    _report  = null!;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "Dwane's Events — Registration Manager";

            // ── 1. Initialize Storage ────────────────────────────────────
            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "DwaneEventsData");

            ConsoleUi.Header("DWANE'S EVENTS — INITIALIZING");
            var (dataFile, auditFile, reportsDir) = StorageInitializer.Initialize(baseDir);

            _audit  = new AuditLogger(auditFile);
            _repo   = new RegistrationRepository(dataFile, _audit);
            _report = new ReportGenerator(reportsDir, _audit);

            _audit.Log("STARTUP", $"Application started. DataFile={dataFile}");
            ConsoleUi.Success("Storage initialized.");
            ConsoleUi.Pause();

            // ── 2. Menu Loop ─────────────────────────────────────────────
            bool running = true;
            while (running)
            {
                ShowMainMenu();
                var choice = Console.ReadLine()?.Trim() ?? "";

                try
                {
                    switch (choice)
                    {
                        case "1": MenuAddRecord();          break;
                        case "2": MenuViewRecords();        break;
                        case "3": MenuSearchRecords();      break;
                        case "4": MenuUpdateRecord();       break;
                        case "5": MenuSoftDelete();         break;
                        case "6": MenuHardDelete();         break;
                        case "7": MenuGenerateReport();     break;
                        case "8": MenuViewAuditLog();       break;
                        case "0":
                            running = false;
                            _audit.Log("EXIT", "User exited the application.");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("\n  Goodbye! — Dwane's Events\n");
                            Console.ResetColor();
                            break;
                        default:
                            ConsoleUi.Warning("Invalid option. Please choose from the menu.");
                            ConsoleUi.Pause();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _audit.LogError($"MenuChoice={choice}", ex.ToString());
                    ConsoleUi.Error($"Unexpected error: {ex.Message}");
                    ConsoleUi.Pause();
                }
            }
        }

        // ── Main Menu ────────────────────────────────────────────────────

        static void ShowMainMenu()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
  ╔══════════════════════════════════════════════════════╗
  ║         🎉  DWANE'S EVENTS REGISTRATION MGR          ║
  ╚══════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine(@"
   [1]  Add Registration
   [2]  View All Active Registrations
   [3]  Search / Filter Registrations
   [4]  Update Registration
   [5]  Deactivate Registration  (Soft Delete)
   [6]  Permanently Delete       (Hard Delete)
   [7]  Generate Summary Report
   [8]  View Audit Log (last 20 entries)
   [0]  Exit
");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("   Choose an option: ");
            Console.ResetColor();
        }

        // ─────────────────────────────────────────────────────────────────
        //  1. ADD RECORD
        // ─────────────────────────────────────────────────────────────────
        static void MenuAddRecord()
        {
            ConsoleUi.Header("ADD NEW REGISTRATION");

            try
            {
                var fullName      = ConsoleUi.Prompt("Full Name");
                var email         = ConsoleUi.Prompt("Email Address");
                var eventName     = ConsoleUi.Prompt("Event Name");
                var eventDate     = ConsoleUi.Prompt("Event Date (yyyy-MM-dd)");
                var contact       = ConsoleUi.Prompt("Contact Number");
                var ticketType    = ConsoleUi.Prompt("Ticket Type [VIP / GENERAL / STUDENT]");
                var amountRaw     = ConsoleUi.Prompt("Amount Paid (PHP)");

                var errors = Validator.ValidateRegistration(
                    fullName, email, eventName, eventDate, contact, ticketType, amountRaw);

                if (errors.Count > 0)
                {
                    ConsoleUi.Error("Validation failed:");
                    errors.ForEach(e => ConsoleUi.Info("• " + e));
                    _audit.LogError("AddRecord", $"Validation errors: {string.Join("; ", errors)}");
                    ConsoleUi.Pause();
                    return;
                }

                decimal.TryParse(amountRaw, out decimal amount);

                var rec = new EventRegistration
                {
                    RecordId      = IdGenerator.Next(),
                    FullName      = fullName.Trim(),
                    Email         = email.Trim().ToLower(),
                    EventName     = eventName.Trim(),
                    EventDate     = eventDate.Trim(),
                    ContactNumber = contact.Trim(),
                    TicketType    = ticketType.Trim().ToUpper(),
                    AmountPaid    = amount,
                    CreatedAt     = DateTime.Now,
                    UpdatedAt     = DateTime.Now,
                    IsActive      = true
                };
                rec.Checksum = rec.ComputeChecksum();

                var (ok, msg) = _repo.Add(rec);
                if (ok)
                {
                    ConsoleUi.Success(msg);
                    ConsoleUi.Info($"Assigned RecordId : {rec.RecordId}");
                    ConsoleUi.Info($"Checksum          : {rec.Checksum}");
                }
                else
                    ConsoleUi.Error(msg);
            }
            catch (Exception ex)
            {
                _audit.LogError("MenuAddRecord", ex.Message);
                ConsoleUi.Error($"Error while adding record: {ex.Message}");
            }

            ConsoleUi.Pause();
        }

        // ─────────────────────────────────────────────────────────────────
        //  2. VIEW ALL ACTIVE
        // ─────────────────────────────────────────────────────────────────
        static void MenuViewRecords()
        {
            ConsoleUi.Header("ACTIVE REGISTRATIONS");
            _audit.Log("READ", "Viewed all active registrations.");

            var records = _repo.GetActive();
            if (records.Count == 0)
            {
                ConsoleUi.Warning("No active registrations found.");
                ConsoleUi.Pause();
                return;
            }

            PrintTable(records);
            ConsoleUi.Info($"Total: {records.Count} active registration(s).");
            ConsoleUi.Pause();
        }

        // ─────────────────────────────────────────────────────────────────
        //  3. SEARCH / FILTER
        // ─────────────────────────────────────────────────────────────────
        static void MenuSearchRecords()
        {
            ConsoleUi.Header("SEARCH REGISTRATIONS");
            Console.WriteLine(@"
  Filter by field:
    [1] Full Name
    [2] Email
    [3] Event Name
    [4] Ticket Type
    [5] Event Date
");
            Console.Write("  Choose field: ");
            var fieldChoice = Console.ReadLine()?.Trim() ?? "";

            string fieldKey = fieldChoice switch
            {
                "1" => "name",
                "2" => "email",
                "3" => "event",
                "4" => "ticket",
                "5" => "date",
                _   => "name"
            };

            var keyword = ConsoleUi.Prompt($"Enter search keyword (field: {fieldKey})");
            if (string.IsNullOrWhiteSpace(keyword))
            {
                ConsoleUi.Warning("Keyword cannot be empty.");
                ConsoleUi.Pause();
                return;
            }

            var results = _repo.Search(fieldKey, keyword);
            _audit.Log("READ", $"Search field={fieldKey} keyword={keyword} results={results.Count}");

            if (results.Count == 0)
            {
                ConsoleUi.Warning($"No records matched '{keyword}' in field '{fieldKey}'.");
            }
            else
            {
                PrintTable(results);
                ConsoleUi.Info($"Found {results.Count} result(s).");
            }

            ConsoleUi.Pause();
        }

        // ─────────────────────────────────────────────────────────────────
        //  4. UPDATE
        // ─────────────────────────────────────────────────────────────────
        static void MenuUpdateRecord()
        {
            ConsoleUi.Header("UPDATE REGISTRATION");

            var id = ConsoleUi.Prompt("Enter RecordId to update");
            var rec = _repo.FindById(id);

            if (rec == null)
            {
                ConsoleUi.Error($"Record '{id}' not found.");
                ConsoleUi.Pause();
                return;
            }
            if (!rec.IsActive)
            {
                ConsoleUi.Warning("Record is inactive. Reactivate it first or restore manually.");
                ConsoleUi.Pause();
                return;
            }

            ConsoleUi.Info($"Current values — press ENTER to keep unchanged.");
            ConsoleUi.Divider();

            // Field-by-field update with inline validation
            var fullName   = ConsoleUi.PromptOptional("Full Name",      rec.FullName);
            var email      = ConsoleUi.PromptOptional("Email",          rec.Email);
            var eventName  = ConsoleUi.PromptOptional("Event Name",     rec.EventName);
            var eventDate  = ConsoleUi.PromptOptional("Event Date",     rec.EventDate);
            var contact    = ConsoleUi.PromptOptional("Contact Number", rec.ContactNumber);
            var ticket     = ConsoleUi.PromptOptional("Ticket Type",    rec.TicketType);
            var amountRaw  = ConsoleUi.PromptOptional("Amount Paid",    rec.AmountPaid.ToString("F2"));

            // Partial validation
            var errors = new List<string>();
            if (fullName.Trim().Length < 2) errors.Add("Full name too short.");
            if (!Validator.IsValidEmail(email)) errors.Add("Invalid email.");
            if (!Validator.IsValidDate(eventDate)) errors.Add("Invalid event date format.");
            if (!Validator.IsValidContact(contact)) errors.Add("Invalid contact number.");
            if (!Validator.IsValidTicketType(ticket)) errors.Add("Invalid ticket type.");
            if (!Validator.TryParseAmount(amountRaw, out decimal newAmount)) errors.Add("Invalid amount.");

            if (errors.Count > 0)
            {
                ConsoleUi.Error("Validation failed — update aborted:");
                errors.ForEach(e => ConsoleUi.Info("• " + e));
                _audit.LogError("UpdateRecord", $"Validation: {string.Join("; ", errors)}");
                ConsoleUi.Pause();
                return;
            }

            rec.FullName      = fullName.Trim();
            rec.Email         = email.Trim().ToLower();
            rec.EventName     = eventName.Trim();
            rec.EventDate     = eventDate.Trim();
            rec.ContactNumber = contact.Trim();
            rec.TicketType    = ticket.Trim().ToUpper();
            rec.AmountPaid    = newAmount;

            var (ok, msg) = _repo.Update(rec);
            if (ok) ConsoleUi.Success(msg);
            else    ConsoleUi.Error(msg);

            ConsoleUi.Pause();
        }

        // ─────────────────────────────────────────────────────────────────
        //  5. SOFT DELETE
        // ─────────────────────────────────────────────────────────────────
        static void MenuSoftDelete()
        {
            ConsoleUi.Header("DEACTIVATE REGISTRATION (Soft Delete)");

            var id = ConsoleUi.Prompt("Enter RecordId to deactivate");
            var rec = _repo.FindById(id);
            if (rec == null) { ConsoleUi.Error("Record not found."); ConsoleUi.Pause(); return; }

            ConsoleUi.Info($"Name  : {rec.FullName}");
            ConsoleUi.Info($"Event : {rec.EventName}  [{rec.EventDate}]");

            if (!ConsoleUi.Confirm("Deactivate this record?"))
            {
                ConsoleUi.Warning("Operation cancelled."); ConsoleUi.Pause(); return;
            }

            var (ok, msg) = _repo.SoftDelete(id);
            if (ok) ConsoleUi.Success(msg); else ConsoleUi.Error(msg);
            ConsoleUi.Pause();
        }

        // ─────────────────────────────────────────────────────────────────
        //  6. HARD DELETE
        // ─────────────────────────────────────────────────────────────────
        static void MenuHardDelete()
        {
            ConsoleUi.Header("PERMANENTLY DELETE REGISTRATION (Hard Delete)");
            ConsoleUi.Warning("This action is IRREVERSIBLE.");

            var id = ConsoleUi.Prompt("Enter RecordId to permanently delete");
            var rec = _repo.FindById(id);
            if (rec == null) { ConsoleUi.Error("Record not found."); ConsoleUi.Pause(); return; }

            ConsoleUi.Info($"Name  : {rec.FullName}");
            ConsoleUi.Info($"Event : {rec.EventName}  [{rec.EventDate}]");

            if (!ConsoleUi.Confirm("⚠ PERMANENTLY delete? This CANNOT be undone"))
            {
                ConsoleUi.Warning("Operation cancelled."); ConsoleUi.Pause(); return;
            }
            // Double-confirm for destructive action
            if (!ConsoleUi.Confirm("Are you absolutely sure?"))
            {
                ConsoleUi.Warning("Operation cancelled."); ConsoleUi.Pause(); return;
            }

            var (ok, msg) = _repo.HardDelete(id);
            if (ok) ConsoleUi.Success(msg); else ConsoleUi.Error(msg);
            ConsoleUi.Pause();
        }

        // ─────────────────────────────────────────────────────────────────
        //  7. REPORT
        // ─────────────────────────────────────────────────────────────────
        static void MenuGenerateReport()
        {
            ConsoleUi.Header("GENERATE SUMMARY REPORT");
            var all = _repo.LoadAll();
            var reportText = _report.GenerateSummaryReport(all);
            Console.WriteLine(reportText);
            ConsoleUi.Pause();
        }

        // ─────────────────────────────────────────────────────────────────
        //  8. AUDIT LOG VIEWER
        // ─────────────────────────────────────────────────────────────────
        static void MenuViewAuditLog()
        {
            ConsoleUi.Header("AUDIT LOG — LAST 20 ENTRIES");

            try
            {
                var logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "DwaneEventsData", "logs", "audit.log");

                if (!File.Exists(logPath))
                {
                    ConsoleUi.Warning("Audit log file not found."); ConsoleUi.Pause(); return;
                }

                var lines = File.ReadAllLines(logPath)
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                                .TakeLast(20)
                                .ToList();

                Console.ForegroundColor = ConsoleColor.DarkGray;
                foreach (var line in lines)
                    Console.WriteLine("  " + line);
                Console.ResetColor();

                ConsoleUi.Info($"Showing last {lines.Count} entries.");
            }
            catch (Exception ex)
            {
                ConsoleUi.Error($"Could not read audit log: {ex.Message}");
            }

            ConsoleUi.Pause();
        }

        // ─────────────────────────────────────────────────────────────────
        //  TABLE PRINTER
        // ─────────────────────────────────────────────────────────────────
        static void PrintTable(List<EventRegistration> records)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine();
            Console.WriteLine(
                $"  {"ID",-22} {"Name",-22} {"Event",-22} {"Date",-12} {"Ticket",-9} {"PHP",8}  {"Email",-28}");
            Console.WriteLine("  " + new string('─', 130));
            Console.ResetColor();

            foreach (var r in records)
            {
                bool csOk = r.IsChecksumValid();
                Console.ForegroundColor = csOk ? ConsoleColor.White : ConsoleColor.Red;
                Console.WriteLine(
                    $"  {r.RecordId,-22}" +
                    $" {Trunc(r.FullName,21),-22}" +
                    $" {Trunc(r.EventName,21),-22}" +
                    $" {r.EventDate,-12}" +
                    $" {r.TicketType,-9}" +
                    $" {r.AmountPaid,8:N2}" +
                    $"  {Trunc(r.Email,27),-28}" +
                    (csOk ? "" : " [!CHKSUM]"));
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        static string Trunc(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
    }
}
