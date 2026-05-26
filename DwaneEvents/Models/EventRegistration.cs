using System;
using System.Security.Cryptography;
using System.Text;

namespace DwaneEvents.Models
{
    public class EventRegistration
    {
        // --- Required Fields ---
        public string RecordId { get; set; } = string.Empty;

        // --- Domain Fields (4+) ---
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string EventDate { get; set; } = string.Empty;   // yyyy-MM-dd
        public string ContactNumber { get; set; } = string.Empty;
        public string TicketType { get; set; } = string.Empty;  // VIP | General | Student
        public decimal AmountPaid { get; set; }

        // --- Metadata ---
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string Checksum { get; set; } = string.Empty;

        // -------------------------------------------------------
        //  Serialization  (pipe-delimited, no newlines in fields)
        // -------------------------------------------------------
        public string Serialize()
        {
            return string.Join("|",
                RecordId,
                Escape(FullName),
                Escape(Email),
                Escape(EventName),
                EventDate,
                Escape(ContactNumber),
                Escape(TicketType),
                AmountPaid.ToString("F2"),
                CreatedAt.ToString("O"),
                UpdatedAt.ToString("O"),
                IsActive ? "1" : "0",
                Checksum);
        }

        public static EventRegistration? Deserialize(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            var p = line.Split('|');
            if (p.Length < 12) return null;
            try
            {
                return new EventRegistration
                {
                    RecordId      = p[0],
                    FullName      = Unescape(p[1]),
                    Email         = Unescape(p[2]),
                    EventName     = Unescape(p[3]),
                    EventDate     = p[4],
                    ContactNumber = Unescape(p[5]),
                    TicketType    = Unescape(p[6]),
                    AmountPaid    = decimal.Parse(p[7]),
                    CreatedAt     = DateTime.Parse(p[8]),
                    UpdatedAt     = DateTime.Parse(p[9]),
                    IsActive      = p[10] == "1",
                    Checksum      = p[11]
                };
            }
            catch { return null; }
        }

        // -------------------------------------------------------
        //  Checksum  (SHA-256 over core fields)
        // -------------------------------------------------------
        public string ComputeChecksum()
        {
            var raw = $"{RecordId}{FullName}{Email}{EventName}{EventDate}" +
                      $"{ContactNumber}{TicketType}{AmountPaid:F2}{IsActive}";
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash)[..12];   // first 12 hex chars
        }

        public bool IsChecksumValid() => Checksum == ComputeChecksum();

        // -------------------------------------------------------
        //  Helpers
        // -------------------------------------------------------
        private static string Escape(string s)   => s.Replace("|", "\\pipe").Replace("\n", " ");
        private static string Unescape(string s) => s.Replace("\\pipe", "|");
    }
}
