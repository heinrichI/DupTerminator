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

        public LogEntry(DateTime timestamp, string level, string message, string category)
        {
            Timestamp = timestamp;
            Level = level;
            Message = message;
            Category = category;
        }

        public override string ToString()
        {
            return $"{Timestamp:HH:mm:ss.fff} [{Level}] {Category}: {Message}";
        }
    }
}
