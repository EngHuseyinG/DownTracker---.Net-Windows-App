using System;
using System.Collections.Generic;

namespace DownTracker.ViewModels
{
    public class FaultyImportViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private List<string[]> _oldP360Rows = new List<string[]>();

        public MainViewModel MainViewModel => _mainViewModel;

        public FaultyImportViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        }

        public string StatusText
        {
            get => _mainViewModel.StatusText;
            set
            {
                if (_mainViewModel.StatusText != value)
                {
                    _mainViewModel.StatusText = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<string[]> OldP360Rows
        {
            get => _oldP360Rows;
            set
            {
                if (_oldP360Rows != value)
                {
                    _oldP360Rows = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
