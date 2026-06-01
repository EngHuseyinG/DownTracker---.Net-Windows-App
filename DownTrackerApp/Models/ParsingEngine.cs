using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DownTracker.Models
{
    public class RawDownPeriod
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public static class ParsingEngine
    {
        public static (string group, string station, string robot) ExtractHierarchy(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return (null, null, null);

            // Split by '.'
            var parts = tagName.Split('.');

            // Filter out unwanted keywords
            var filtered = new List<string>();
            foreach (var p in parts)
            {
                var trimmed = p.Trim('"', ' ', '\r', '\n', '\t');
                if (string.IsNullOrEmpty(trimmed) || trimmed == "-")
                    continue;

                if (string.Equals(trimmed, "Errors", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(trimmed, "Outputs", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(trimmed, "Alarm", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(trimmed, "Transactionend", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(trimmed, "ST_DownCondition", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Contains("DiscreteAlarm", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                filtered.Add(trimmed);
            }

            if (filtered.Count == 0) return (null, null, null);

            string group = null;
            string station = null;
            string robot = null;

            // 1. Search for standalone robot segment in the hierarchy (e.g. "R1", "R2")
            int robotIndex = -1;
            for (int i = 0; i < filtered.Count; i++)
            {
                var match = Regex.Match(filtered[i], @"^[Rr](\d+)$");
                if (match.Success)
                {
                    robot = "R" + match.Groups[1].Value;
                    robotIndex = i;
                    break;
                }
            }

            if (robotIndex != -1)
            {
                station = (robotIndex > 0) ? filtered[robotIndex - 1] : null;
                group = (robotIndex > 1) ? filtered[robotIndex - 2] : null;
            }
            else
            {
                // 2. Search for robot suffix on the segments (e.g. "9A50LH-R1" or "8F35_R2"), starting from the last one
                for (int i = filtered.Count - 1; i >= 0; i--)
                {
                    var match = Regex.Match(filtered[i], @"^(.*)[-_][Rr](\d+)$");
                    if (match.Success)
                    {
                        var potentialStation = match.Groups[1].Value.Trim('-', '_', '.');
                        if (!string.IsNullOrEmpty(potentialStation))
                        {
                            robot = "R" + match.Groups[2].Value;
                            station = potentialStation;
                            group = (i > 0) ? filtered[i - 1] : null;
                            break;
                        }
                    }
                }

                // 3. Fallback: if still no robot is found, the last segment is the station
                if (station == null)
                {
                    station = filtered[filtered.Count - 1];
                    group = (filtered.Count > 1) ? filtered[filtered.Count - 2] : null;
                }
            }

            // Cleanup fields
            if (station != null)
            {
                station = station.Trim('-', '_', '.');
                if (station == "-" || string.IsNullOrEmpty(station))
                {
                    station = null;
                }
            }

            if (group != null)
            {
                group = group.Trim('-', '_', '.');
                if (group == "-" || string.IsNullOrEmpty(group))
                {
                    group = null;
                }
            }

            return (group, station, robot);
        }

        public static List<LogEntry> ParseCsv(string filePath)
        {
            var entries = new List<LogEntry>();
            if (!File.Exists(filePath)) return entries;

            // 1. Detect file encoding (UTF-16 LE, UTF-16 BE, UTF-8 with BOM, or fallback to auto-detection)
            Encoding encoding = Encoding.UTF8;
            byte[] bom = new byte[4];

            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                int bytesRead = 0;
                while (bytesRead < 4)
                {
                    int r = file.Read(bom, bytesRead, 4 - bytesRead);
                    if (r <= 0) break;
                    bytesRead += r;
                }
            }

            if (bom[0] == 0xff && bom[1] == 0xfe) // UTF-16 LE
            {
                encoding = Encoding.Unicode;
            }
            else if (bom[0] == 0xfe && bom[1] == 0xff) // UTF-16 BE
            {
                encoding = Encoding.BigEndianUnicode;
            }
            else if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) // UTF-8 with BOM
            {
                encoding = Encoding.UTF8;
            }
            else
            {
                // Inspect the file content for null bytes to detect UTF-16 without BOM
                bool hasNulls = false;
                using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    int b;
                    int bytesRead = 0;
                    while ((b = file.ReadByte()) != -1 && bytesRead < 100)
                    {
                        if (b == 0)
                        {
                            hasNulls = true;
                            break;
                        }
                        bytesRead++;
                    }
                }
                if (hasNulls)
                {
                    encoding = Encoding.Unicode;
                }
            }

            // 2. Read the lines using safe StreamReader configuration to prevent lock exceptions
            var linesList = new List<string>();
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs, encoding))
            {
                string lineText;
                while ((lineText = reader.ReadLine()) != null)
                {
                    linesList.Add(lineText);
                }
            }
            var lines = linesList.ToArray();
            int startIndex = 0;

            // Smart Header Validation
            if (lines.Length > 0)
            {
                string firstLineCleaned = lines[0].Replace("\"", "").Replace("'", "").Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");
                if (firstLineCleaned.Contains("TagName", StringComparison.OrdinalIgnoreCase) || 
                    firstLineCleaned.Contains("TagDate", StringComparison.OrdinalIgnoreCase))
                {
                    startIndex = 1;
                }
                else
                {
                    startIndex = 0;
                }
            }

            string[] formats = new string[] { "yyyy-MM-dd HH:mm:ss", "dd.MM.yyyy HH:mm:ss" };

            for (int i = startIndex; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Split by comma
                string[] parts = line.Split(',');
                if (parts.Length < 3) continue;

                // Advanced String Trimming
                string tagName = parts[0].Trim('"', ' ', '\r', '\n', '\t');
                string valStr = parts[1].Trim('"', ' ', '\r', '\n', '\t');
                string dateStr = parts[2].Trim('"', ' ', '\r', '\n', '\t');

                // Parse Date using InvariantCulture
                if (!DateTime.TryParseExact(dateStr, formats, 
                    CultureInfo.InvariantCulture, 
                    DateTimeStyles.None, out DateTime tagDate))
                {
                    if (!DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out tagDate))
                    {
                        continue;
                    }
                }

                // Parse Boolean Value
                bool val = false;
                if (valStr.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                    valStr.Equals("1") ||
                    valStr.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    val = true;
                }

                // Extract group, station, and robot
                var (group, station, robot) = ExtractHierarchy(tagName);

                // Determine signal type
                string signalType = "Other";
                if (tagName.EndsWith(".Outputs.Transactionend", StringComparison.OrdinalIgnoreCase) ||
                    tagName.Contains(".Outputs.Transactionend", StringComparison.OrdinalIgnoreCase))
                {
                    signalType = "Transactionend";
                }
                else if (tagName.EndsWith(".Outputs.ST_DownCondition", StringComparison.OrdinalIgnoreCase) ||
                         tagName.Contains(".Outputs.ST_DownCondition", StringComparison.OrdinalIgnoreCase))
                {
                    signalType = "ST_DownCondition";
                }
                else if (tagName.EndsWith(".Errors.Alarm.DiscreteAlarm", StringComparison.OrdinalIgnoreCase) ||
                         tagName.Contains(".Errors.Alarm.DiscreteAlarm", StringComparison.OrdinalIgnoreCase))
                {
                    signalType = "DiscreteAlarm";
                }

                entries.Add(new LogEntry
                {
                    TagDate = tagDate,
                    TagName = tagName,
                    StationGroup = group,
                    StationName = station,
                    RobotName = robot,
                    SignalType = signalType,
                    Value = val,
                    RawLine = line
                });
            }

            // Sort chronologically
            entries.Sort((a, b) => a.TagDate.CompareTo(b.TagDate));
            return entries;
        }

        public static void ProcessData(
            List<LogEntry> allLogs,
            out Dictionary<string, List<Cycle>> stationCycles,
            out Dictionary<string, List<DowntimeRecord>> stationDowntimes,
            out Dictionary<string, List<string>> stationRobots,
            out Dictionary<string, string> stationGroups)
        {
            stationCycles = new Dictionary<string, List<Cycle>>();
            stationDowntimes = new Dictionary<string, List<DowntimeRecord>>();
            stationRobots = new Dictionary<string, List<string>>();
            stationGroups = new Dictionary<string, string>();

            // 1. Group logs by StationName to establish strict quarantine (Zero Cross-Contamination)
            var logsByStation = allLogs
                .Where(l => !string.IsNullOrEmpty(l.StationName))
                .GroupBy(l => l.StationName);

            foreach (var stationGroup in logsByStation)
            {
                string stationName = stationGroup.Key;
                var stationLogs = stationGroup.OrderBy(l => l.TagDate).ToList();

                // Find station group name
                string groupName = stationLogs.FirstOrDefault(l => !string.IsNullOrEmpty(l.StationGroup))?.StationGroup ?? "Unknown Group";
                stationGroups[stationName] = groupName;

                // Find all valid transaction ends (Transactionend = True) belonging to the station (not to any specific robot)
                var transactionEndTimes = stationLogs
                    .Where(l => l.SignalType == "Transactionend" && l.Value && string.IsNullOrEmpty(l.RobotName))
                    .Select(l => l.TagDate)
                    .Distinct() // Ensure unique timestamps to prevent zero-duration cycles
                    .OrderBy(t => t)
                    .ToList();

                // 3. Find distinct robots
                var robots = stationLogs
                    .Where(l => !string.IsNullOrEmpty(l.RobotName))
                    .Select(l => l.RobotName)
                    .Distinct()
                    .OrderBy(r => r)
                    .ToList();
                stationRobots[stationName] = robots;

                // If no or only one Transactionend, the station's cycles and downtimes must be completely empty!
                if (transactionEndTimes.Count < 2)
                {
                    stationCycles[stationName] = new List<Cycle>();
                    stationDowntimes[stationName] = new List<DowntimeRecord>();
                    continue;
                }

                // 2. Build cycles
                var cycles = new List<Cycle>();
                for (int i = 0; i < transactionEndTimes.Count - 1; i++)
                {
                    cycles.Add(new Cycle
                    {
                        Index = i + 1,
                        StartTime = transactionEndTimes[i],
                        EndTime = transactionEndTimes[i + 1],
                        StationGroup = groupName,
                        StationName = stationName
                    });
                }
                stationCycles[stationName] = cycles;

                // 4. Process downtimes using State Machine
                var processedDowntimes = new List<DowntimeRecord>();
                var activeDowntimes = new Dictionary<string, DateTime>();
                var isSplitDowntimes = new Dictionary<string, bool>();

                foreach (var log in stationLogs)
                {
                    // Case A: Transactionend splits any currently active downtimes
                    if (log.SignalType == "Transactionend" && log.Value && string.IsNullOrEmpty(log.RobotName))
                    {
                        var activeKeys = activeDowntimes.Keys.ToList();
                        foreach (var key in activeKeys)
                        {
                            DateTime downStart = activeDowntimes[key];
                            if (downStart < log.TagDate)
                            {
                                // Split! This segment belongs to the cycle ending at log.TagDate
                                var containingCycle = cycles.FirstOrDefault(c => c.EndTime == log.TagDate);
                                if (containingCycle != null)
                                {
                                    string robotName = string.IsNullOrEmpty(key) ? null : key;
                                    var cycleAlarm = stationLogs
                                        .Where(al => al.SignalType == "DiscreteAlarm" &&
                                                     al.Value &&
                                                     al.RobotName == robotName &&
                                                     al.TagDate > containingCycle.StartTime &&
                                                     al.TagDate <= containingCycle.EndTime)
                                        .OrderBy(al => al.TagDate)
                                        .FirstOrDefault();

                                    processedDowntimes.Add(new DowntimeRecord
                                    {
                                        StartTime = downStart,
                                        EndTime = log.TagDate,
                                        StationGroup = groupName,
                                        StationName = stationName,
                                        RobotName = robotName,
                                        CycleIndex = containingCycle.Index,
                                        CycleStartTime = containingCycle.StartTime,
                                        CycleEndTime = containingCycle.EndTime,
                                        RootCauseAlarm = cycleAlarm != null ? cycleAlarm.TagName : "Boş Arıza",
                                        IsAlarmed = cycleAlarm != null,
                                        IsSplit = true
                                    });
                                }

                                // Update the active downtime start to the split boundary
                                activeDowntimes[key] = log.TagDate;
                                isSplitDowntimes[key] = true;
                            }
                        }
                    }
                    // Case B: ST_DownCondition transitions
                    else if (log.SignalType == "ST_DownCondition")
                    {
                        string key = log.RobotName ?? "";
                        if (log.Value) // Start of downtime
                        {
                            if (!activeDowntimes.ContainsKey(key))
                            {
                                activeDowntimes[key] = log.TagDate;
                                isSplitDowntimes[key] = false;
                            }
                        }
                        else // End of downtime
                        {
                            if (activeDowntimes.TryGetValue(key, out DateTime downStart))
                            {
                                var containingCycle = cycles.FirstOrDefault(c => c.StartTime <= downStart && log.TagDate <= c.EndTime);
                                if (containingCycle != null)
                                {
                                    string robotName = string.IsNullOrEmpty(key) ? null : key;
                                    var cycleAlarm = stationLogs
                                        .Where(al => al.SignalType == "DiscreteAlarm" &&
                                                     al.Value &&
                                                     al.RobotName == robotName &&
                                                     al.TagDate > containingCycle.StartTime &&
                                                     al.TagDate <= containingCycle.EndTime)
                                        .OrderBy(al => al.TagDate)
                                        .FirstOrDefault();

                                    processedDowntimes.Add(new DowntimeRecord
                                    {
                                        StartTime = downStart,
                                        EndTime = log.TagDate,
                                        StationGroup = groupName,
                                        StationName = stationName,
                                        RobotName = robotName,
                                        CycleIndex = containingCycle.Index,
                                        CycleStartTime = containingCycle.StartTime,
                                        CycleEndTime = containingCycle.EndTime,
                                        RootCauseAlarm = cycleAlarm != null ? cycleAlarm.TagName : "Boş Arıza",
                                        IsAlarmed = cycleAlarm != null,
                                        IsSplit = isSplitDowntimes.TryGetValue(key, out bool split) && split
                                    });
                                }

                                activeDowntimes.Remove(key);
                                isSplitDowntimes.Remove(key);
                            }
                        }
                    }
                }

                // Close down periods if still down at the end of the logs
                if (activeDowntimes.Count > 0 && stationLogs.Count > 0)
                {
                    var lastLogDate = stationLogs.Last().TagDate;
                    foreach (var kvp in activeDowntimes)
                    {
                        string key = kvp.Key;
                        DateTime downStart = kvp.Value;
                        if (downStart < lastLogDate)
                        {
                            var containingCycle = cycles.FirstOrDefault(c => c.StartTime <= downStart && lastLogDate <= c.EndTime);
                            if (containingCycle != null)
                            {
                                string robotName = string.IsNullOrEmpty(key) ? null : key;
                                var cycleAlarm = stationLogs
                                    .Where(al => al.SignalType == "DiscreteAlarm" &&
                                                 al.Value &&
                                                 al.RobotName == robotName &&
                                                 al.TagDate > containingCycle.StartTime &&
                                                 al.TagDate <= containingCycle.EndTime)
                                    .OrderBy(al => al.TagDate)
                                    .FirstOrDefault();

                                processedDowntimes.Add(new DowntimeRecord
                                {
                                    StartTime = downStart,
                                    EndTime = lastLogDate,
                                    StationGroup = groupName,
                                    StationName = stationName,
                                    RobotName = robotName,
                                    CycleIndex = containingCycle.Index,
                                    CycleStartTime = containingCycle.StartTime,
                                    CycleEndTime = containingCycle.EndTime,
                                    RootCauseAlarm = cycleAlarm != null ? cycleAlarm.TagName : "Boş Arıza",
                                    IsAlarmed = cycleAlarm != null,
                                    IsSplit = isSplitDowntimes.TryGetValue(key, out bool split) && split
                                });
                            }
                        }
                    }
                }

                // Sort micro-downtimes chronologically
                processedDowntimes.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));

                // 5. Downtime Consolidation logic:
                // Group by CycleIndex and RobotName (normalized)
                var consolidatedDowntimes = new List<DowntimeRecord>();
                var groups = processedDowntimes.GroupBy(d => new { d.CycleIndex, RobotKey = d.RobotName ?? "" });
                foreach (var group in groups)
                {
                    var sortedGroup = group.OrderBy(d => d.StartTime).ToList();
                    var first = sortedGroup[0];
                    var last = sortedGroup[sortedGroup.Count - 1];

                    // Find first alarmed child record
                    var firstAlarmed = sortedGroup.FirstOrDefault(d => d.IsAlarmed);
                    string rootAlarm = firstAlarmed != null ? firstAlarmed.RootCauseAlarm : "Boş Arıza";
                    bool isAlarmed = firstAlarmed != null;

                    var consolidatedRecord = new DowntimeRecord
                    {
                        StartTime = first.StartTime,
                        EndTime = last.EndTime,
                        StationGroup = groupName,
                        StationName = stationName,
                        RobotName = first.RobotName,
                        CycleIndex = first.CycleIndex,
                        CycleStartTime = first.CycleStartTime,
                        CycleEndTime = first.CycleEndTime,
                        RootCauseAlarm = rootAlarm,
                        IsAlarmed = isAlarmed,
                        IsSplit = sortedGroup.Any(d => d.IsSplit),
                        ChildDowntimes = sortedGroup
                    };

                    // Duration = SUM of all child durations
                    double totalDurationSeconds = sortedGroup.Sum(d => d.Duration.TotalSeconds);
                    consolidatedRecord.Duration = TimeSpan.FromSeconds(totalDurationSeconds);

                    consolidatedDowntimes.Add(consolidatedRecord);
                }

                // Sort consolidated downtimes chronologically
                consolidatedDowntimes.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                stationDowntimes[stationName] = consolidatedDowntimes;

                // Update cycle aggregate statistics
                foreach (var cycle in cycles)
                {
                    var cycleDowntimes = consolidatedDowntimes
                        .Where(d => d.CycleIndex == cycle.Index)
                        .ToList();

                    cycle.DowntimeCount = cycleDowntimes.Count;
                    cycle.TotalDowntimeSeconds = cycleDowntimes.Sum(d => d.Duration.TotalSeconds);
                }
            }
        }
    }
}
