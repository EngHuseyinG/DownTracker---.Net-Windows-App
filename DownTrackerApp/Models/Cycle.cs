using System;

namespace DownTracker.Models
{
    public class Cycle
    {
        public int Index { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public string StationGroup { get; set; }
        public string StationName { get; set; }
        public int DowntimeCount { get; set; }
        public double TotalDowntimeSeconds { get; set; }

        public string DurationDisplay => $"{Duration.TotalSeconds:F1}s";
    }
}
