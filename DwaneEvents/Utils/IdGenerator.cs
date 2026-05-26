using System;

namespace DwaneEvents.Utils
{
    public static class IdGenerator
    {
        private static int _seq = 0;
        private static readonly object _lock = new();

        /// <summary>
        /// Generates a unique ID like  EVT-20251231-0001
        /// The sequence resets per session; uniqueness is guaranteed by
        /// the repository checking for collisions.
        /// </summary>
        public static string Next()
        {
            lock (_lock)
            {
                _seq++;
                return $"EVT-{DateTime.Now:yyyyMMdd}-{_seq:D4}";
            }
        }
    }
}
