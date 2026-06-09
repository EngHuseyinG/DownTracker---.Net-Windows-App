using System;
using System.ComponentModel;
using System.Linq;
using DownTracker.Models;

namespace DownTracker.ViewModels
{
    public class DownConditionsViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private bool _isAscending = true;
        private string _currentSortColumn = "StartTime";

        public DownConditionsViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        }

        public string CurrentSortColumn
        {
            get => _currentSortColumn;
            set
            {
                if (_currentSortColumn != value)
                {
                    _currentSortColumn = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsAscending
        {
            get => _isAscending;
            set
            {
                if (_isAscending != value)
                {
                    _isAscending = value;
                    OnPropertyChanged();
                }
            }
        }

        public BindingList<DowntimeRecord> FilteredDowntimes => _mainViewModel.FilteredDowntimes;

        public void SortData(string columnName)
        {
            if (CurrentSortColumn == columnName)
            {
                IsAscending = !IsAscending;
            }
            else
            {
                CurrentSortColumn = columnName;
                IsAscending = true;
            }

            var list = _mainViewModel.FilteredDowntimes.ToList();
            switch (columnName)
            {
                case "StartTime":
                    list = IsAscending ? list.OrderBy(x => x.StartTime).ToList() : list.OrderByDescending(x => x.StartTime).ToList();
                    break;
                case "EndTime":
                    list = IsAscending ? list.OrderBy(x => x.EndTime).ToList() : list.OrderByDescending(x => x.EndTime).ToList();
                    break;
                case "Duration":
                    list = IsAscending ? list.OrderBy(x => x.Duration).ToList() : list.OrderByDescending(x => x.Duration).ToList();
                    break;
            }

            _mainViewModel.FilteredDowntimes.RaiseListChangedEvents = false;
            _mainViewModel.FilteredDowntimes.Clear();
            foreach (var item in list)
            {
                _mainViewModel.FilteredDowntimes.Add(item);
            }
            _mainViewModel.FilteredDowntimes.RaiseListChangedEvents = true;
            _mainViewModel.FilteredDowntimes.ResetBindings();
        }
    }
}
