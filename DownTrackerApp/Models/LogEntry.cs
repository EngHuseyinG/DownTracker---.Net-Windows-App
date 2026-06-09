using System;

namespace DownTracker.Models
{
    public class LogEntry
    {
        public DateTime TagDate { get; set; }
        public string TagName { get; set; }
        public string StationGroup { get; set; } // Station Group e.g., "8F"
        public string StationName { get; set; } // Station e.g., "8F35"
        public string RobotName { get; set; }   // Robot e.g., "R2" (No dashes/dots)
        public string SignalType { get; set; }  // "Transactionend", "ST_DownCondition", "DiscreteAlarm", or "Other"
        public bool Value { get; set; }
        public string RawLine { get; set; }
    }
}
