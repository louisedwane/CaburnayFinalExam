using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DwaneEvents.Models;
using DwaneEvents.Services;

namespace DwaneEvents.Repositories
{
    /// <summary>
    /// Flat-file repository.  One record = one line in data/registrations.dat
    /// Rewrites the whole file on every save (acceptable for console-scale data).
    /// </summary>
    public class RegistrationRepository
    {
        private readonly string _dataFile;
        private readonly AuditLogger _audit;
        private readonly object _fileLock = new();

        public RegistrationRepository(string dataFile, AuditLogger audit)
        {
            _dataFile = dataFile;
            _audit    = audit;
        }

        // ------------------------------------------------------------------
        //  Load / Save
        // ------------------------------------------------------------------

        public List<EventRegistration> LoadAll()
        {
            lock (_fileLock)
            {
                var records = new List<EventRegistration>();
                if (!File.Exists(_dataFile)) return records;

                int lineNo = 0;
                foreach (var line in File.ReadAllLines(_dataFile))
                {
                    lineNo++;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var rec = EventRegistration.Deserialize(line);
                    if (rec == null)
                    {
                        _audit.LogError("LoadAll", $"Malformed record at line {lineNo}: {line[..Math.Min(60,line.Length)]}...");
                        continue;
                    }

                    if (!rec.IsChecksumValid())
                        _audit.LogError("LoadAll",
                            $"Checksum mismatch for RecordId={rec.RecordId} at line {lineNo}");

                    records.Add(rec);
                }
                return records;
            }
        }

        private void SaveAll(List<EventRegistration> records)
        {
            lock (_fileLock)
            {
                var lines = records.Select(r => r.Serialize());
                File.WriteAllLines(_dataFile, lines);
            }
        }

        // ------------------------------------------------------------------
        //  CRUD
        // ------------------------------------------------------------------

        public (bool ok, string message) Add(EventRegistration rec)
        {
            try
            {
                var all = LoadAll();
                if (all.Any(r => r.RecordId == rec.RecordId))
                    return (false, $"RecordId {rec.RecordId} already exists.");

                rec.Checksum = rec.ComputeChecksum();
                all.Add(rec);
                SaveAll(all);
                _audit.Log("ADD", $"Added registration for '{rec.FullName}' event='{rec.EventName}'", rec.RecordId);
                return (true, "Record added successfully.");
            }
            catch (Exception ex)
            {
                _audit.LogError("Add", ex.Message);
                return (false, $"IO error: {ex.Message}");
            }
        }

        public List<EventRegistration> GetActive()
            => LoadAll().Where(r => r.IsActive).ToList();

        public EventRegistration? FindById(string id)
            => LoadAll().FirstOrDefault(r => r.RecordId == id);

        public (bool ok, string message) Update(EventRegistration updated)
        {
            try
            {
                var all = LoadAll();
                var idx = all.FindIndex(r => r.RecordId == updated.RecordId);
                if (idx < 0) return (false, "Record not found.");

                updated.UpdatedAt = DateTime.Now;
                updated.Checksum  = updated.ComputeChecksum();
                all[idx] = updated;
                SaveAll(all);
                _audit.Log("UPDATE", $"Updated registration '{updated.FullName}'", updated.RecordId);
                return (true, "Record updated successfully.");
            }
            catch (Exception ex)
            {
                _audit.LogError("Update", ex.Message);
                return (false, $"IO error: {ex.Message}");
            }
        }

        /// <summary>Soft delete — sets IsActive = false.</summary>
        public (bool ok, string message) SoftDelete(string id)
        {
            try
            {
                var all = LoadAll();
                var rec = all.FirstOrDefault(r => r.RecordId == id);
                if (rec == null) return (false, "Record not found.");
                if (!rec.IsActive) return (false, "Record is already inactive.");

                rec.IsActive  = false;
                rec.UpdatedAt = DateTime.Now;
                rec.Checksum  = rec.ComputeChecksum();
                SaveAll(all);
                _audit.Log("SOFT-DEL", $"Soft-deleted '{rec.FullName}'", id);
                return (true, "Record deactivated (soft delete).");
            }
            catch (Exception ex)
            {
                _audit.LogError("SoftDelete", ex.Message);
                return (false, $"IO error: {ex.Message}");
            }
        }

        /// <summary>Hard delete — permanently removes the record line.</summary>
        public (bool ok, string message) HardDelete(string id)
        {
            try
            {
                var all = LoadAll();
                var rec = all.FirstOrDefault(r => r.RecordId == id);
                if (rec == null) return (false, "Record not found.");

                all.Remove(rec);
                SaveAll(all);
                _audit.Log("HARD-DEL", $"Permanently deleted '{rec.FullName}'", id);
                return (true, "Record permanently deleted.");
            }
            catch (Exception ex)
            {
                _audit.LogError("HardDelete", ex.Message);
                return (false, $"IO error: {ex.Message}");
            }
        }

        /// <summary>Search active records by name, email, event, or ticket type.</summary>
        public List<EventRegistration> Search(string field, string keyword)
        {
            var active  = GetActive();
            keyword     = keyword.ToLower();
            return field.ToLower() switch
            {
                "name"    => active.Where(r => r.FullName.ToLower().Contains(keyword)).ToList(),
                "email"   => active.Where(r => r.Email.ToLower().Contains(keyword)).ToList(),
                "event"   => active.Where(r => r.EventName.ToLower().Contains(keyword)).ToList(),
                "ticket"  => active.Where(r => r.TicketType.ToLower().Contains(keyword)).ToList(),
                "date"    => active.Where(r => r.EventDate.Contains(keyword)).ToList(),
                _         => active
            };
        }
    }
}
