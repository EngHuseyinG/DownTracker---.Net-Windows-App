using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DownTracker.Models
{
    public class RawDownPeriod
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public static class ParsingEngine
    {
        private static readonly Dictionary<string, string> FaultCodeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Acil Stop (M)", "FC-1000" }, { "Bakim (M)", "FC-1014" }, { "Barkod (E)", "FC-1021" },
            { "Barkod (M)", "FC-1005" }, { "Clamp Feedback (E)", "FC-1043" }, { "CLS (E)", "FC-1039" },
            { "Ekipman (E)", "FC-1026" }, { "Ekipman (M)", "FC-1003" }, { "Elektrik (E)", "FC-1019" },
            { "Elektrik (M)", "FC-1012" }, { "Enerji (M)", "FC-1007" }, { "Guvenlik (E)", "FC-1024" },
            { "Guvenlik (M)", "FC-1006" }, { "Haberlesme (E)", "FC-1031" }, { "Imalat (M)", "FC-1015" },
            { "Ip Cekme (M)", "FC-1001" }, { "Istasyon (E)", "FC-1036" }, { "Istasyon (M)", "FC-1011" },
            { "Kalite (E)", "FC-1025" }, { "Kalite (M)", "FC-1009" }, { "Malzeme (M)", "FC-1017" },
            { "Manuel (M)", "FC-1013" }, { "Mekanik (E)", "FC-1032" }, { "Mekanik (M)", "FC-1008" },
            { "Model Degisimi (M)", "FC-1016" }, { "MQL (E)", "FC-1045" }, { "Olcum (E)", "FC-1037" },
            { "Otomasyon (E)", "FC-1033" }, { "Otomasyon (M)", "FC-1004" }, { "Part Detect (E)", "FC-1041" },
            { "Plc (E)", "FC-1035" }, { "Pnomatik (E)", "FC-1023" }, { "Pozisyon (E)", "FC-1030" },
            { "Program (E)", "FC-1049" }, { "Proses (E)", "FC-1029" }, { "Proses (M)", "FC-1002" },
            { "Pvs Ngavs (E)", "FC-1028" }, { "Robot (E)", "FC-1034" }, { "Robot (M)", "FC-1038" },
            { "Safety (E)", "FC-1062" }, { "Sensor Feedback (E)", "FC-1052" }, { "Sessis Durus (E)", "FC-1027" },
            { "Temporary (M)", "FC-1018" }, { "Tip (E)", "FC-1022" }, { "Utility (E)", "FC-1064" },
            { "Safety (M)", "FC-1065" }
        };

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

                // Helper to match alarm for a downtime period (relaxed matching for main station)
                LogEntry GetMatchingAlarm(string rName, DateTime dStart, DateTime dEnd, Cycle currentCycle)
                {
                    if (string.IsNullOrEmpty(rName))
                    {
                        // Ana istasyon: Duruşun ait olduğu iki TransactionEnd arasındaki tüm robot/istasyon alarmlarını tarar
                        return stationLogs
                            .Where(al => al.SignalType == "DiscreteAlarm" &&
                                         al.Value &&
                                         al.TagDate >= currentCycle.StartTime &&
                                         al.TagDate <= currentCycle.EndTime &&
                                         al.TagName.Contains(stationName, StringComparison.OrdinalIgnoreCase))
                            .OrderBy(al => al.TagDate)
                            .FirstOrDefault();
                    }
                    else
                    {
                        // Robot: Kendi çevrim sınırları içindeki tam eşleşme mantığı korunur
                        return stationLogs
                            .Where(al => al.SignalType == "DiscreteAlarm" &&
                                         al.Value &&
                                         al.RobotName == rName &&
                                         al.TagDate >= currentCycle.StartTime &&
                                         al.TagDate <= currentCycle.EndTime)
                            .OrderBy(al => al.TagDate)
                            .FirstOrDefault();
                    }
                }

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
                                    var cycleAlarm = GetMatchingAlarm(robotName, downStart, log.TagDate, containingCycle);

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
                                    var cycleAlarm = GetMatchingAlarm(robotName, downStart, log.TagDate, containingCycle);

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
                                var cycleAlarm = GetMatchingAlarm(robotName, downStart, lastLogDate, containingCycle);

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

        public static List<ImportedAlarmRecord> ConvertImportedAlarms(string inputFilePath)
        {
            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException("Kaynak dosya bulunamadı.", inputFilePath);

            var records = new List<ImportedAlarmRecord>();
            string ext = Path.GetExtension(inputFilePath).ToLower();

            if (ext == ".xlsx")
            {
                List<string> sharedStrings = new List<string>();
                using (var fs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    // 1. Read shared strings
                    var sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml");
                    if (sharedStringsEntry != null)
                    {
                        using (var stream = sharedStringsEntry.Open())
                        {
                            XDocument sstDoc = XDocument.Load(stream);
                            XNamespace ns = sstDoc.Root.Name.Namespace;
                            foreach (var si in sstDoc.Root.Elements(ns + "si"))
                            {
                                var textNodes = si.Descendants(ns + "t").Select(tNode => tNode.Value);
                                sharedStrings.Add(string.Concat(textNodes));
                            }
                        }
                    }

                    // 2. Read sheet1
                    var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
                    if (sheetEntry != null)
                    {
                        using (var stream = sheetEntry.Open())
                        {
                            XDocument sheetDoc = XDocument.Load(stream);
                            XNamespace ns = sheetDoc.Root.Name.Namespace;
                            var sheetData = sheetDoc.Root.Element(ns + "sheetData");
                            if (sheetData != null)
                            {
                                bool isFirstRow = true;
                                foreach (var row in sheetData.Elements(ns + "row"))
                                {
                                    if (isFirstRow)
                                    {
                                        isFirstRow = false;
                                        // Skip header row
                                        continue;
                                    }

                                    // Row values mapping array
                                    string[] rowValues = new string[18];
                                    for (int j = 0; j < rowValues.Length; j++) rowValues[j] = "";

                                    foreach (var cell in row.Elements(ns + "c"))
                                    {
                                        string cellRef = (string)cell.Attribute("r") ?? "";
                                        if (string.IsNullOrEmpty(cellRef)) continue;

                                        int colIdx = GetColumnIndex(cellRef);
                                        if (colIdx >= 0 && colIdx < rowValues.Length)
                                        {
                                            rowValues[colIdx] = GetCellValue(cell, sharedStrings).Trim('"', ' ', '\r', '\n', '\t');
                                        }
                                    }

                                    // index check safety and field mappings
                                    if (rowValues.Length > 17)
                                    {
                                        var record = new ImportedAlarmRecord
                                        {
                                            DeviceName = rowValues[1],
                                            Comment = rowValues[14],
                                            ParentCategory = rowValues[15],
                                            Category = rowValues[16],
                                            FullTagName = rowValues[17],
                                            Code = ""
                                        };

                                        if (!string.IsNullOrEmpty(record.FullTagName))
                                        {
                                            records.Add(record);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else // .csv
            {
                // 1. Detect file encoding
                Encoding encoding = Encoding.UTF8;
                byte[] bom = new byte[4];

                using (var file = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
                    using (var file = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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

                using (var fs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs, encoding))
                {
                    string line;
                    bool isFirstLine = true;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        if (isFirstLine)
                        {
                            isFirstLine = false;
                            // Always skip the first line (header)
                            continue;
                        }

                        // Try semicolon first, then comma
                        string[] parts = line.Split(';');
                        if (parts.Length <= 17)
                        {
                            parts = line.Split(',');
                        }

                        // Strict Boundaries Checking (parts.Length > 17 because we access parts[17] which is R column)
                        if (parts.Length > 17)
                        {
                            var record = new ImportedAlarmRecord
                            {
                                DeviceName = parts[1].Trim('"', ' ', '\r', '\n', '\t'),
                                Comment = parts[14].Trim('"', ' ', '\r', '\n', '\t'),
                                ParentCategory = parts[15].Trim('"', ' ', '\r', '\n', '\t'),
                                Category = parts[16].Trim('"', ' ', '\r', '\n', '\t'),
                                FullTagName = parts[17].Trim('"', ' ', '\r', '\n', '\t'),
                                Code = ""
                            };
                            if (!string.IsNullOrEmpty(record.FullTagName))
                            {
                                records.Add(record);
                            }
                        }
                    }
                }
            }

            return records;
        }

        private static int GetColumnIndex(string cellRef)
        {
            int i = 0;
            while (i < cellRef.Length && char.IsLetter(cellRef[i]))
            {
                i++;
            }
            string colLetters = cellRef.Substring(0, i).ToUpperInvariant();
            int colIndex = 0;
            foreach (char c in colLetters)
            {
                colIndex = colIndex * 26 + (c - 'A' + 1);
            }
            return colIndex - 1; // 0-based
        }

        private static string GetCellValue(XElement cell, List<string> sharedStrings)
        {
            string t = (string)cell.Attribute("t");
            string rawValue = cell.Element(cell.Name.Namespace + "v")?.Value ?? "";

            if (t == "s")
            {
                if (int.TryParse(rawValue, out int idx) && idx >= 0 && idx < sharedStrings.Count)
                {
                    return sharedStrings[idx];
                }
                return "";
            }
            else if (t == "inlineStr")
            {
                return cell.Element(cell.Name.Namespace + "is")?.Element(cell.Name.Namespace + "t")?.Value ?? "";
            }
            else
            {
                return rawValue;
            }
        }

        public static void SaveToExcel(string filePath, List<ImportedAlarmRecord> records)
        {
            using (var fs = new FileStream(filePath, FileMode.Create))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                // 1. [Content_Types].xml
                var contentTypeEntry = archive.CreateEntry("[Content_Types].xml");
                using (var writer = new StreamWriter(contentTypeEntry.Open(), Encoding.UTF8))
                {
                    writer.Write(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml"" />
  <Default Extension=""xml"" ContentType=""application/xml"" />
  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"" />
  <Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"" />
  <Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"" />
</Types>");
                }

                // 2. _rels/.rels
                var relsEntry = archive.CreateEntry("_rels/.rels");
                using (var writer = new StreamWriter(relsEntry.Open(), Encoding.UTF8))
                {
                    writer.Write(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml"" />
</Relationships>");
                }

                // 3. xl/workbook.xml
                var workbookEntry = archive.CreateEntry("xl/workbook.xml");
                using (var writer = new StreamWriter(workbookEntry.Open(), Encoding.UTF8))
                {
                    writer.Write(@"<?xml version=""1.0"" encoding=""utf-8""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""Sheet1"" sheetId=""1"" r:id=""rId1"" />
  </sheets>
</workbook>");
                }

                // 4. xl/_rels/workbook.xml.rels
                var workbookRelsEntry = archive.CreateEntry("xl/_rels/workbook.xml.rels");
                using (var writer = new StreamWriter(workbookRelsEntry.Open(), Encoding.UTF8))
                {
                    writer.Write(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml"" />
  <Relationship Id=""rId3"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml"" />
</Relationships>");
                }

                // xl/styles.xml
                var stylesEntry = archive.CreateEntry("xl/styles.xml");
                using (var writer = new StreamWriter(stylesEntry.Open(), Encoding.UTF8))
                {
                    writer.Write(@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <fonts count=""3"">
    <font><sz val=""11""/><name val=""Calibri""/><family val=""2""/></font>
    <font><b/><sz val=""11""/><color rgb=""FFFFFF""/><name val=""Calibri""/><family val=""2""/></font>
    <font><b/><sz val=""11""/><color rgb=""000000""/><name val=""Calibri""/><family val=""2""/></font>
  </fonts>
  <fills count=""8"">
    <fill><patternFill patternType=""none""/></fill>
    <fill><patternFill patternType=""gray125""/></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFB7DEE8""/><bgColor indexed=""64""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFFFC000""/><bgColor indexed=""64""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFFCE4D6""/><bgColor indexed=""64""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFEAF1DD""/><bgColor indexed=""64""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFEAF1DD""/><bgColor indexed=""64""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFBFBFBF""/><bgColor indexed=""64""/></patternFill></fill>
  </fills>
  <borders count=""1""><border><left/><right/><top/><bottom/></border></borders>
  <cellXfs count=""7"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>
    <xf numFmtId=""0"" fontId=""2"" fillId=""2"" borderId=""0"" applyFont=""1"" applyFill=""1"" applyAlignment=""1""><alignment horizontal=""center"" vertical=""center""/></xf>
    <xf numFmtId=""0"" fontId=""2"" fillId=""3"" borderId=""0"" applyFont=""1"" applyFill=""1"" applyAlignment=""1""><alignment horizontal=""center"" vertical=""center""/></xf>
    <xf numFmtId=""0"" fontId=""2"" fillId=""4"" borderId=""0"" applyFont=""1"" applyFill=""1"" applyAlignment=""1""><alignment horizontal=""center"" vertical=""center""/></xf>
    <xf numFmtId=""0"" fontId=""2"" fillId=""5"" borderId=""0"" applyFont=""1"" applyFill=""1"" applyAlignment=""1""><alignment horizontal=""center"" vertical=""center""/></xf>
    <xf numFmtId=""0"" fontId=""2"" fillId=""6"" borderId=""0"" applyFont=""1"" applyFill=""1"" applyAlignment=""1""><alignment horizontal=""center"" vertical=""center""/></xf>
    <xf numFmtId=""0"" fontId=""2"" fillId=""7"" borderId=""0"" applyFont=""1"" applyFill=""1"" applyAlignment=""1""><alignment horizontal=""center"" vertical=""center""/></xf>
  </cellXfs>
</styleSheet>");
                }

                // 5. xl/worksheets/sheet1.xml
                var sheetEntry = archive.CreateEntry("xl/worksheets/sheet1.xml");
                using (var writer = new StreamWriter(sheetEntry.Open(), Encoding.UTF8))
                {
                    writer.Write(@"<?xml version=""1.0"" encoding=""utf-8""?>
<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <cols>
    <col min=""1"" max=""1"" width=""105"" customWidth=""1""/>
    <col min=""2"" max=""2"" width=""25"" customWidth=""1""/>
    <col min=""3"" max=""3"" width=""50"" customWidth=""1""/>
    <col min=""4"" max=""4"" width=""30"" customWidth=""1""/>
    <col min=""5"" max=""5"" width=""30"" customWidth=""1""/>
    <col min=""6"" max=""6"" width=""15"" customWidth=""1""/>
  </cols>
  <sheetData>");

                    // Header Row
                    writer.Write("<row r=\"1\">");
                    writer.Write("<c r=\"A1\" t=\"inlineStr\" s=\"1\"><is><t>FULLTAGNAME</t></is></c>");
                    writer.Write("<c r=\"B1\" t=\"inlineStr\" s=\"2\"><is><t>DEVICENAME</t></is></c>");
                    writer.Write("<c r=\"C1\" t=\"inlineStr\" s=\"3\"><is><t>COMMENT</t></is></c>");
                    writer.Write("<c r=\"D1\" t=\"inlineStr\" s=\"4\"><is><t>ParentCategory</t></is></c>");
                    writer.Write("<c r=\"E1\" t=\"inlineStr\" s=\"5\"><is><t>Category</t></is></c>");
                    writer.Write("<c r=\"F1\" t=\"inlineStr\" s=\"6\"><is><t>Code</t></is></c>");
                    writer.Write("</row>");

                    // Data Rows
                    int rowIndex = 2;
                    foreach (var r in records)
                    {
                        string faultCode = "Eşleşme Bulunamadı";
                        if (!string.IsNullOrEmpty(r.Category) && FaultCodeMap.TryGetValue(r.Category.Trim(), out string mappedCode))
                        {
                            faultCode = mappedCode;
                        }
                        r.Code = faultCode;

                        var formulaBuilder = new StringBuilder();
                        int openParentheses = 0;
                        foreach (var kvp in FaultCodeMap)
                        {
                            formulaBuilder.Append($"IF(E{rowIndex}=\"{kvp.Key}\",\"{kvp.Value}\",");
                            openParentheses++;
                        }
                        formulaBuilder.Append("\"Eşleşme Bulunamadı\"");
                        for (int i = 0; i < openParentheses; i++)
                        {
                            formulaBuilder.Append(")");
                        }
                        string formulaText = formulaBuilder.ToString();

                        writer.Write($"<row r=\"{rowIndex}\">");
                        writer.Write($"<c r=\"A{rowIndex}\" t=\"inlineStr\"><is><t>{EscapeXml(r.FullTagName)}</t></is></c>");
                        writer.Write($"<c r=\"B{rowIndex}\" t=\"inlineStr\"><is><t>{EscapeXml(r.DeviceName)}</t></is></c>");
                        writer.Write($"<c r=\"C{rowIndex}\" t=\"inlineStr\"><is><t>{EscapeXml(r.Comment)}</t></is></c>");
                        writer.Write($"<c r=\"D{rowIndex}\" t=\"inlineStr\"><is><t>{EscapeXml(r.ParentCategory)}</t></is></c>");
                        writer.Write($"<c r=\"E{rowIndex}\" t=\"inlineStr\"><is><t>{EscapeXml(r.Category)}</t></is></c>");
                        writer.Write($"<c r=\"F{rowIndex}\" t=\"str\"><f>{EscapeXml(formulaText)}</f><v></v></c>");
                        writer.Write("</row>");
                        rowIndex++;
                    }

                    writer.Write(@"  </sheetData>
</worksheet>");
                }
            }
        }

        private static string EscapeXml(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("&", "&amp;")
                        .Replace("<", "&lt;")
                        .Replace(">", "&gt;")
                        .Replace("\"", "&quot;")
                        .Replace("'", "&apos;");
        }

        public static void GenerateInputTemplate(string filePath)
        {
            string[] headers = new string[] { 
                "CHANNEL NAME", "DEVICE NAME", "LINE NAME (LVL1)", "LINE NAME (LVL2)", "LINE NAME (LVL3)", "STATION NAME", 
                "Res", "Res", "Tag Name", "PLC Adress", "Data Type", "A/D", "RO//R/W", "SCAN RATE", 
                "COMMENT", "Category Name", "SubCategory Name", "FULL TAG NAME" 
            };

            string[] sampleData = new string[] {
                "OPC71_Kaynak_v710_Underbody_ZONE0_PLC1_PLC1", "PLC1", "v710", "Underbody", "ZONE0", "PLC1", "Errors", "Alarm", "DiscreteAlarm1", "DB2100.DBX270.0", "Boolean", "1", "RO", "1000", "9A Power Alarm F100", "EquipmentDownCondition", "Elektrik (E)", "OPC71_Kaynak_v710_Underbody_ZONE0_PLC1_PLC1.PLC1.v710.Underbody.ZONE0.PLC1.Errors.Alarm.DiscreteAlarm1"
            };

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Create))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    // 1. [Content_Types].xml
                    var contentTypeEntry = archive.CreateEntry("[Content_Types].xml");
                    using (var writer = new StreamWriter(contentTypeEntry.Open(), Encoding.UTF8))
                    {
                        writer.Write(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml"" />
  <Default Extension=""xml"" ContentType=""application/xml"" />
  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"" />
  <Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"" />
  <Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"" />
</Types>");
                    }

                    // 2. _rels/.rels
                    var relsEntry = archive.CreateEntry("_rels/.rels");
                    using (var writer = new StreamWriter(relsEntry.Open(), Encoding.UTF8))
                    {
                        writer.Write(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml"" />
</Relationships>");
                    }

                    // 3. xl/workbook.xml
                    var workbookEntry = archive.CreateEntry("xl/workbook.xml");
                    using (var writer = new StreamWriter(workbookEntry.Open(), Encoding.UTF8))
                    {
                        writer.Write(@"<?xml version=""1.0"" encoding=""utf-8""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""Sheet1"" sheetId=""1"" r:id=""rId1"" />
  </sheets>
</workbook>");
                    }

                    // 4. xl/_rels/workbook.xml.rels
                    var workbookRelsEntry = archive.CreateEntry("xl/_rels/workbook.xml.rels");
                    using (var writer = new StreamWriter(workbookRelsEntry.Open(), Encoding.UTF8))
                    {
                        writer.Write(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml"" />
  <Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml"" />
</Relationships>");
                    }

                    // 5. xl/styles.xml
                    var stylesEntry = archive.CreateEntry("xl/styles.xml");
                    using (var writer = new StreamWriter(stylesEntry.Open(), Encoding.UTF8))
                    {
                        writer.Write(@"<?xml version=""1.0"" encoding=""utf-8""?>
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <fonts count=""3"">
    <font>
      <sz val=""11""/>
      <name val=""Calibri""/>
      <family val=""2""/>
    </font>
    <font>
      <b/>
      <sz val=""12""/>
      <name val=""Calibri""/>
      <family val=""2""/>
    </font>
    <font>
      <sz val=""11""/>
      <name val=""Calibri""/>
      <family val=""2""/>
    </font>
  </fonts>
  <fills count=""13"">
    <fill>
      <patternFill patternType=""none""/>
    </fill>
    <fill>
      <patternFill patternType=""gray125""/>
    </fill>
    <fill>
      <patternFill patternType=""solid"">
        <fgColor rgb=""FFFFC000""/>
        <bgColor indexed=""64""/>
      </patternFill>
    </fill>
    <fill>
      <patternFill patternType=""solid"">
        <fgColor rgb=""FF92D050""/>
        <bgColor indexed=""64""/>
      </patternFill>
    </fill>
    <fill>
      <patternFill patternType=""solid"">
        <fgColor rgb=""FF00B0F0""/>
        <bgColor indexed=""64""/>
      </patternFill>
    </fill>
    <fill>
      <patternFill patternType=""solid"">
        <fgColor rgb=""FFBFBFBF""/>
        <bgColor indexed=""64""/>
      </patternFill>
    </fill>
    <fill>
      <patternFill patternType=""solid"">
        <fgColor rgb=""FFFFFF00""/>
        <bgColor indexed=""64""/>
      </patternFill>
    </fill>
    <fill>
      <patternFill patternType=""solid"">
        <fgColor rgb=""FFD99694""/>
        <bgColor indexed=""64""/>
      </patternFill>
    </fill>
    <fill>
      <patternFill patternType=""solid"">
        <fgColor rgb=""FFD9D9D9""/>
        <bgColor indexed=""64""/>
      </patternFill>
    </fill>
    <fill>
      <patternFill patternType=""solid"">
        <fgColor rgb=""FF92CDDC""/>
        <bgColor indexed=""64""/>
      </patternFill>
    </fill>
    <fill>
      <patternFill patternType=""solid"">
        <fgColor rgb=""FFFCE4D6""/>
        <bgColor indexed=""64""/>
      </patternFill>
    </fill>
    <fill>
      <patternFill patternType=""solid"">
        <fgColor rgb=""FFEAF1DD""/>
        <bgColor indexed=""64""/>
      </patternFill>
    </fill>
    <fill>
      <patternFill patternType=""solid"">
        <fgColor rgb=""FFB7DEE8""/>
        <bgColor indexed=""64""/>
      </patternFill>
    </fill>
  </fills>
  <borders count=""1"">
    <border>
      <left/><right/><top/><bottom/>
    </border>
  </borders>
  <cellStyleXfs count=""1"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/>
  </cellStyleXfs>
  <cellXfs count=""13"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""2"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""3"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""4"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""5"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""6"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""7"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""8"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""9"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""10"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""11"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""12"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/>
    <xf numFmtId=""0"" fontId=""2"" fillId=""0"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/>
  </cellXfs>
  <cellStyles count=""1"">
    <cellStyle name=""Normal"" cellStyleId=""0"" xfId=""0""/>
  </cellStyles>
</styleSheet>");
                    }

                    // 6. xl/worksheets/sheet1.xml
                    var sheetEntry = archive.CreateEntry("xl/worksheets/sheet1.xml");
                    using (var writer = new StreamWriter(sheetEntry.Open(), Encoding.UTF8))
                    {
                        writer.Write(@"<?xml version=""1.0"" encoding=""utf-8""?>
<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <cols>
    <col min=""1"" max=""18"" width=""30"" customWidth=""1""/>
  </cols>
  <sheetData>");

                        // Header Row
                        writer.Write("<row r=\"1\">");
                        for (int col = 0; col < headers.Length; col++)
                        {
                            string cellRef = GetCellReference(col, 1);
                            int styleIndex = 1;
                            if (col == 0 || col == 1) styleIndex = 1;
                            else if (col == 2 || col == 3 || col == 4) styleIndex = 2;
                            else if (col == 5) styleIndex = 3;
                            else if (col == 6 || col == 7) styleIndex = 4;
                            else if (col == 8 || col == 9) styleIndex = 5;
                            else if (col == 10) styleIndex = 6;
                            else if (col == 11 || col == 12) styleIndex = 7;
                            else if (col == 13) styleIndex = 8;
                            else if (col == 14) styleIndex = 9;
                            else if (col == 15 || col == 16) styleIndex = 10;
                            else if (col == 17) styleIndex = 11;

                            writer.Write($"<c r=\"{cellRef}\" s=\"{styleIndex}\" t=\"inlineStr\"><is><t>{EscapeXml(headers[col])}</t></is></c>");
                        }
                        writer.Write("</row>");

                        // Sample Data Row
                        writer.Write("<row r=\"2\">");
                        for (int col = 0; col < sampleData.Length; col++)
                        {
                            string cellRef = GetCellReference(col, 2);
                            writer.Write($"<c r=\"{cellRef}\" s=\"12\" t=\"inlineStr\"><is><t>{EscapeXml(sampleData[col])}</t></is></c>");
                        }
                        writer.Write("</row>");

                        writer.Write(@"  </sheetData>
</worksheet>");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Excel şablonu oluşturulamadı: {ex.Message}", ex);
            }
        }

        private static string GetCellReference(int colIndex, int rowIndex)
        {
            int temp = colIndex;
            string colName = "";
            while (temp >= 0)
            {
                colName = (char)('A' + (temp % 26)) + colName;
                temp = (temp / 26) - 1;
            }
            return colName + rowIndex;
        }
    }
}
