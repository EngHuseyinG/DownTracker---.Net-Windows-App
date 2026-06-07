using System;
using System.ComponentModel;
using DownTracker.Models;

namespace DownTracker.ViewModels
{
    public class RawLogsViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;

        public RawLogsViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        }

        public BindingList<LogEntry> RawLogs => _mainViewModel.RawLogs;
    }
}
