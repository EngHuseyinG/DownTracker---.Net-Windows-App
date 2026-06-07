using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using DownTracker.Models;

namespace DownTracker.ViewModels
{
    public enum DowntimeFilter
    {
        All,
        Alarmed,
        Empty
    }

    public class MainViewModel : BaseViewModel
    {
        private string _filePath;
        private string _statusText = "Hazır. Lütfen bir CSV dosyası yükleyin.";
        private bool _isFileLoaded;
        private string _selectedFilterType = "All";
        private string _selectedGroupName;
        private string _selectedStationName;
        private string _selectedRobotName;
        private DowntimeFilter _currentFilter = DowntimeFilter.All;
        private string _searchText;

        // Raw logs loaded from the CSV
        private List<LogEntry> _allRawLogs = new List<LogEntry>();

        // Processed data dictionaries
        private Dictionary<string, List<Cycle>> _stationCycles = new Dictionary<string, List<Cycle>>();
        private Dictionary<string, List<DowntimeRecord>> _stationDowntimes = new Dictionary<string, List<DowntimeRecord>>();
        private Dictionary<string, List<string>> _stationRobots = new Dictionary<string, List<string>>();
        private Dictionary<string, string> _stationGroups = new Dictionary<string, string>();

        public int GlobalTotalDowntimes { get; set; }
        public int GlobalAlarmedDowntimes { get; set; }
        public int GlobalUnalarmedDowntimes { get; set; }

        public int SelectedTotalDowntimes { get; set; }
        public int SelectedAlarmedDowntimes { get; set; }
        public int SelectedUnalarmedDowntimes { get; set; }

        // Bindable properties for the UI
        public BindingList<LogEntry> RawLogs { get; } = new BindingList<LogEntry>();
        public BindingList<DowntimeRecord> FilteredDowntimes { get; } = new BindingList<DowntimeRecord>();
        public BindingList<Cycle> CurrentCycles { get; } = new BindingList<Cycle>();
        public BindingList<string> StationsList { get; } = new BindingList<string>();

        public string FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsFileLoaded
        {
            get => _isFileLoaded;
            set
            {
                if (_isFileLoaded != value)
                {
                    _isFileLoaded = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedFilterType
        {
            get => _selectedFilterType;
            set
            {
                if (_selectedFilterType != value)
                {
                    _selectedFilterType = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedGroupName
        {
            get => _selectedGroupName;
            set
            {
                if (_selectedGroupName != value)
                {
                    _selectedGroupName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedStationName
        {
            get => _selectedStationName;
            set
            {
                if (_selectedStationName != value)
                {
                    _selectedStationName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedRobotName
        {
            get => _selectedRobotName;
            set
            {
                if (_selectedRobotName != value)
                {
                    _selectedRobotName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedStation
        {
            get => SelectedStationName;
            set
            {
                if (SelectedStationName != value)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        SelectedFilterType = "All";
                        SelectedGroupName = null;
                        SelectedStationName = null;
                        SelectedRobotName = null;
                    }
                    else
                    {
                        SelectedFilterType = "Station";
                        SelectedStationName = value;
                        SelectedRobotName = null;
                        if (_stationGroups.TryGetValue(value, out var group))
                        {
                            SelectedGroupName = group;
                        }
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedRobot));
                    RefreshData();
                }
            }
        }

        public string SelectedRobot
        {
            get => SelectedRobotName;
            set
            {
                if (SelectedRobotName != value)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        if (!string.IsNullOrEmpty(SelectedStationName))
                        {
                            SelectedFilterType = "Station";
                            SelectedRobotName = null;
                        }
                    }
                    else
                    {
                        SelectedFilterType = "Robot";
                        SelectedRobotName = value;
                    }
                    OnPropertyChanged();
                    RefreshData();
                }
            }
        }

        public DowntimeFilter CurrentFilter
        {
            get => _currentFilter;
            set
            {
                if (_currentFilter != value)
                {
                    _currentFilter = value;
                    OnPropertyChanged();
                    RefreshData();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                }
            }
        }

        // Hierarchical asset mapping for TreeView
        public Dictionary<string, List<string>> StationRobots => _stationRobots;
        public Dictionary<string, string> StationGroups => _stationGroups;

        public void ApplyProcessedData(
            string filePath,
            List<LogEntry> logs,
            Dictionary<string, List<Cycle>> cycles,
            Dictionary<string, List<DowntimeRecord>> downtimes,
            Dictionary<string, List<string>> robots,
            Dictionary<string, string> groups)
        {
            // Enforce chronological sorting by StartTime for each station
            foreach (var key in downtimes.Keys.ToList())
            {
                downtimes[key] = downtimes[key].OrderBy(d => d.StartTime).ToList();
            }

            // Update state
            _allRawLogs = logs;
            _stationCycles = cycles;
            _stationDowntimes = downtimes;
            _stationRobots = robots;
            _stationGroups = groups;

            // Calculate global KPI counters across all stations
            int globalTotal = 0;
            int globalAlarmed = 0;
            int globalUnalarmed = 0;
            foreach (var list in downtimes.Values)
            {
                foreach (var record in list)
                {
                    globalTotal++;
                    if (record.IsAlarmed)
                    {
                        globalAlarmed++;
                    }
                    else
                    {
                        globalUnalarmed++;
                    }
                }
            }
            GlobalTotalDowntimes = globalTotal;
            GlobalAlarmedDowntimes = globalAlarmed;
            GlobalUnalarmedDowntimes = globalUnalarmed;

            FilePath = filePath;
            IsFileLoaded = true;

            // Populate stations list
            StationsList.RaiseListChangedEvents = false;
            StationsList.Clear();
            var sortedStations = _stationCycles.Keys.OrderBy(k => k).ToList();
            foreach (var st in sortedStations)
            {
                StationsList.Add(st);
            }
            StationsList.RaiseListChangedEvents = true;
            StationsList.ResetBindings();

            StatusText = $"Dosya başarıyla yüklendi: {logs.Count} log satırı, {sortedStations.Count} istasyon algılandı.";

            // Select All by default
            SelectedFilterType = "All";
            SelectedGroupName = null;
            SelectedStationName = null;
            SelectedRobotName = null;
            RefreshData();
        }

        public bool LoadAndProcessData(string filePath)
        {
            return LoadCsvFile(filePath);
        }

        public bool LoadCsvFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("Belirtilen dosya bulunamadı.", filePath);
                }

                StatusText = "Dosya okunuyor ve çözümleniyor...";
                var logs = ParsingEngine.ParseCsv(filePath);

                if (logs.Count == 0)
                {
                    throw new Exception("Log dosyasından geçerli veri satırları çözümlenemedi. Lütfen dosya içeriğini kontrol edin.");
                }

                // Process data in parsing engine
                ParsingEngine.ProcessData(logs, out var cycles, out var downtimes, out var robots, out var groups);

                // Enforce chronological sorting by StartTime for each station
                foreach (var key in downtimes.Keys.ToList())
                {
                    downtimes[key] = downtimes[key].OrderBy(d => d.StartTime).ToList();
                }

                // Update state
                _allRawLogs = logs;
                _stationCycles = cycles;
                _stationDowntimes = downtimes;
                _stationRobots = robots;
                _stationGroups = groups;

                // Calculate global KPI counters across all stations
                int globalTotal = 0;
                int globalAlarmed = 0;
                int globalUnalarmed = 0;
                foreach (var list in downtimes.Values)
                {
                    foreach (var record in list)
                    {
                        globalTotal++;
                        if (record.IsAlarmed)
                        {
                            globalAlarmed++;
                        }
                        else
                        {
                            globalUnalarmed++;
                        }
                    }
                }
                GlobalTotalDowntimes = globalTotal;
                GlobalAlarmedDowntimes = globalAlarmed;
                GlobalUnalarmedDowntimes = globalUnalarmed;

                FilePath = filePath;
                IsFileLoaded = true;

                // Populate stations list
                StationsList.RaiseListChangedEvents = false;
                StationsList.Clear();
                var sortedStations = _stationCycles.Keys.OrderBy(k => k).ToList();
                foreach (var st in sortedStations)
                {
                    StationsList.Add(st);
                }
                StationsList.RaiseListChangedEvents = true;
                StationsList.ResetBindings();

                StatusText = $"Dosya başarıyla yüklendi: {logs.Count} log satırı, {sortedStations.Count} istasyon algılandı.";

                // Select All by default
                SelectedFilterType = "All";
                SelectedGroupName = null;
                SelectedStationName = null;
                SelectedRobotName = null;
                RefreshData();

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Sistem Hatası Yakalandı!\n\nMesaj: {ex.Message}\n\nHata Yeri: {ex.TargetSite}\n\nDetay: {ex.ToString()}", "Kök Hata Raporu", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return false;
            }
        }

        public void RefreshData()
        {
            // Clear current list bindings
            FilteredDowntimes.RaiseListChangedEvents = false;
            FilteredDowntimes.Clear();

            CurrentCycles.RaiseListChangedEvents = false;
            CurrentCycles.Clear();

            if (SelectedFilterType == "All")
            {
                foreach (var cyclesList in _stationCycles.Values)
                {
                    foreach (var cycle in cyclesList)
                    {
                        CurrentCycles.Add(cycle);
                    }
                }

                foreach (var downtimesList in _stationDowntimes.Values)
                {
                    foreach (var d in downtimesList)
                    {
                        if (PassesAlarmFilter(d))
                        {
                            FilteredDowntimes.Add(d);
                        }
                    }
                }
            }
            else if (SelectedFilterType == "Group")
            {
                foreach (var cyclesList in _stationCycles.Values)
                {
                    foreach (var cycle in cyclesList)
                    {
                        if (cycle.StationGroup == SelectedGroupName)
                        {
                            CurrentCycles.Add(cycle);
                        }
                    }
                }

                foreach (var downtimesList in _stationDowntimes.Values)
                {
                    foreach (var d in downtimesList)
                    {
                        if (d.StationGroup == SelectedGroupName)
                        {
                            if (PassesAlarmFilter(d))
                            {
                                FilteredDowntimes.Add(d);
                            }
                        }
                    }
                }
            }
            else if (SelectedFilterType == "Station")
            {
                if (_stationCycles.TryGetValue(SelectedStationName, out var cycles))
                {
                    foreach (var cycle in cycles)
                    {
                        if (cycle.StationName == SelectedStationName)
                        {
                            CurrentCycles.Add(cycle);
                        }
                    }
                }

                if (_stationDowntimes.TryGetValue(SelectedStationName, out var downtimes))
                {
                    foreach (var d in downtimes)
                    {
                        if (d.StationName == SelectedStationName)
                        {
                            if (PassesAlarmFilter(d))
                            {
                                FilteredDowntimes.Add(d);
                            }
                        }
                    }
                }
            }
            else if (SelectedFilterType == "Robot")
            {
                // Show parent station's cycles with stats scoped exclusively to the selected robot
                if (_stationCycles.TryGetValue(SelectedStationName, out var cycles))
                {
                    List<DowntimeRecord> robotDowntimes = new List<DowntimeRecord>();
                    if (_stationDowntimes.TryGetValue(SelectedStationName, out var downtimes))
                    {
                        robotDowntimes = downtimes
                            .Where(d => d.StationName == SelectedStationName && d.RobotName == SelectedRobotName)
                            .ToList();
                    }

                    foreach (var cycle in cycles)
                    {
                        if (cycle.StationName == SelectedStationName)
                        {
                            var cycleDowntimes = robotDowntimes
                                .Where(d => d.CycleIndex == cycle.Index)
                                .ToList();

                            var cycleCopy = new Cycle
                            {
                                Index = cycle.Index,
                                StartTime = cycle.StartTime,
                                EndTime = cycle.EndTime,
                                StationGroup = cycle.StationGroup,
                                StationName = cycle.StationName,
                                DowntimeCount = cycleDowntimes.Count,
                                TotalDowntimeSeconds = cycleDowntimes.Sum(d => d.Duration.TotalSeconds)
                            };

                            CurrentCycles.Add(cycleCopy);
                        }
                    }
                }

                if (_stationDowntimes.TryGetValue(SelectedStationName, out var downtimesList))
                {
                    foreach (var d in downtimesList)
                    {
                        if (d.StationName == SelectedStationName && d.RobotName == SelectedRobotName)
                        {
                            if (PassesAlarmFilter(d))
                            {
                                FilteredDowntimes.Add(d);
                            }
                        }
                    }
                }
            }

            // Calculate selected KPI counters based on filtered downtimes list
            int selTotal = 0;
            int selAlarmed = 0;
            int selUnalarmed = 0;
            foreach (var d in FilteredDowntimes)
            {
                selTotal++;
                if (d.IsAlarmed)
                {
                    selAlarmed++;
                }
                else
                {
                    selUnalarmed++;
                }
            }
            SelectedTotalDowntimes = selTotal;
            SelectedAlarmedDowntimes = selAlarmed;
            SelectedUnalarmedDowntimes = selUnalarmed;

            FilteredDowntimes.RaiseListChangedEvents = true;
            FilteredDowntimes.ResetBindings();

            CurrentCycles.RaiseListChangedEvents = true;
            CurrentCycles.ResetBindings();

            // Populate and filter RawLogs dynamically based on asset selection and SearchText
            RawLogs.RaiseListChangedEvents = false;
            RawLogs.Clear();

            var filteredLogs = _allRawLogs.AsEnumerable();

            if (SelectedFilterType == "Group")
            {
                filteredLogs = filteredLogs.Where(log => log.StationGroup == SelectedGroupName);
            }
            else if (SelectedFilterType == "Station")
            {
                filteredLogs = filteredLogs.Where(log => log.StationName == SelectedStationName);
            }
            else if (SelectedFilterType == "Robot")
            {
                filteredLogs = filteredLogs.Where(log => log.StationName == SelectedStationName && log.RobotName == SelectedRobotName);
            }

            if (!string.IsNullOrEmpty(SearchText))
            {
                filteredLogs = filteredLogs.Where(log =>
                    (log.TagName != null && log.TagName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (log.StationName != null && log.StationName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (log.RobotName != null && log.RobotName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (log.StationGroup != null && log.StationGroup.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                );
            }

            foreach (var log in filteredLogs)
            {
                RawLogs.Add(log);
            }

            RawLogs.RaiseListChangedEvents = true;
            RawLogs.ResetBindings();
        }

        private bool PassesAlarmFilter(DowntimeRecord d)
        {
            if (CurrentFilter == DowntimeFilter.All)
            {
                return true;
            }
            if (CurrentFilter == DowntimeFilter.Alarmed)
            {
                return d.IsAlarmed;
            }
            if (CurrentFilter == DowntimeFilter.Empty)
            {
                return !d.IsAlarmed;
            }
            return true;
        }
    }
}
