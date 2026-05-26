using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DwaneEvents.Services
{
    public static class Validator
    {
        private static readonly string[] ValidTicketTypes = { "VIP", "GENERAL", "STUDENT" };

        // Returns a list of error messages; empty = valid
        public static List<string> ValidateRegistration(
            string fullName,
            string email,
            string eventName,
            string eventDate,
            string contactNumber,
            string ticketType,
            string amountPaidRaw)
        {
            var errors = new List<string>();

            // FullName
            if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length < 2)
                errors.Add("Full name must be at least 2 characters.");
            if (fullName.Length > 100)
                errors.Add("Full name must not exceed 100 characters.");

            // Email
            if (!Regex.IsMatch(email.Trim(),
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase))
                errors.Add("Email address is invalid.");

            // EventName
            if (string.IsNullOrWhiteSpace(eventName) || eventName.Trim().Length < 3)
                errors.Add("Event name must be at least 3 characters.");
            if (eventName.Length > 120)
                errors.Add("Event name must not exceed 120 characters.");

            // EventDate  (yyyy-MM-dd)
            if (!DateTime.TryParseExact(eventDate.Trim(), "yyyy-MM-dd",
                    null, System.Globalization.DateTimeStyles.None, out _))
                errors.Add("Event date must be in yyyy-MM-dd format (e.g., 2025-12-31).");

            // ContactNumber  (7–15 digits, optional leading +)
            if (!Regex.IsMatch(contactNumber.Trim(), @"^\+?\d{7,15}$"))
                errors.Add("Contact number must be 7–15 digits (optional leading +).");

            // TicketType
            var tt = ticketType.Trim().ToUpper();
            if (Array.IndexOf(ValidTicketTypes, tt) < 0)
                errors.Add($"Ticket type must be one of: {string.Join(", ", ValidTicketTypes)}.");

            // AmountPaid
            if (!decimal.TryParse(amountPaidRaw.Trim(), out decimal amount) || amount < 0)
                errors.Add("Amount paid must be a non-negative number.");

            return errors;
        }

        public static bool TryParseAmount(string raw, out decimal result)
            => decimal.TryParse(raw.Trim(), out result) && result >= 0;

        public static bool IsValidTicketType(string t)
            => Array.IndexOf(ValidTicketTypes, t.Trim().ToUpper()) >= 0;

        public static bool IsValidDate(string d)
            => DateTime.TryParseExact(d.Trim(), "yyyy-MM-dd",
                null, System.Globalization.DateTimeStyles.None, out _);

        public static bool IsValidEmail(string e)
            => Regex.IsMatch(e.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);

        public static bool IsValidContact(string c)
            => Regex.IsMatch(c.Trim(), @"^\+?\d{7,15}$");
    }
}
