using System;
using System.Collections.Generic;

namespace DownTracker.Models
{
    public class DowntimeRecord
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        private TimeSpan? _customDuration;
        public TimeSpan Duration
        {
            get => _customDuration ?? (EndTime - StartTime);
            set => _customDuration = value;
        }

        public string StationGroup { get; set; }
        public string StationName { get; set; }
        public string RobotName { get; set; } // May be null/empty
        public int CycleIndex { get; set; } // The index of the cycle this downtime belongs to
        public DateTime CycleStartTime { get; set; }
        public DateTime CycleEndTime { get; set; }
        public string RootCauseAlarm { get; set; } // Root cause alarm tag name, or "Boş Arıza"
        public bool IsAlarmed { get; set; } // True if a real alarm was found, false if empty
        public bool IsSplit { get; set; } // True if this downtime is a split segment of a longer downtime

        public List<DowntimeRecord> ChildDowntimes { get; set; } = new List<DowntimeRecord>();

        public string DurationDisplay => $"{Duration.TotalSeconds:F1}s";
        public string CycleContextDisplay => CycleStartTime == default ? "-" : $"[{CycleStartTime:HH:mm:ss} - {CycleEndTime:HH:mm:ss}]";
        public string AssetName => string.IsNullOrEmpty(RobotName) ? StationName : $"{StationName}-{RobotName}";
    }
}
