using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.WPF.Model
{
    public class LogEntry : PropertyChangedBase
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Category { get; set; }
        public string? Path { get; set; }

        public LogEntry(DateTime timestamp, string level, string message, string category, string? path = null)
        {
            Timestamp = timestamp;
            Level = level;
            Message = message;
            Category = category;
            Path = path;
        }

        public override string ToString()
        {
            return $"{Timestamp:HH:mm:ss.fff} [{Level}] {Category}: {Message}";
        }
    }
}
