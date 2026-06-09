using System;
using System.ComponentModel;
using DownTracker.Models;

namespace DownTracker.ViewModels
{
    public class CycleAnalysisViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;

        public CycleAnalysisViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        }

        public BindingList<Cycle> CurrentCycles => _mainViewModel.CurrentCycles;
    }
}
