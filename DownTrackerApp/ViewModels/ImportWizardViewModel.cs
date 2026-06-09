using System;

namespace DownTracker.ViewModels
{
    public class ImportWizardViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;

        public MainViewModel MainViewModel => _mainViewModel;

        public ImportWizardViewModel(MainViewModel mainViewModel)
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
    }
}
