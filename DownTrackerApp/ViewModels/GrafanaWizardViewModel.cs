using System;

namespace DownTracker.ViewModels
{
    public class GrafanaWizardViewModel : BaseViewModel
    {
        private string _date = DateTime.Today.ToString("d_M_yyyy");
        private string _channel = "OPC71";
        private string _asset = "9A";
        private bool _allAssets = false;
        private string _sqlQuery;

        public string Date
        {
            get => _date;
            set
            {
                if (_date != value)
                {
                    _date = value;
                    OnPropertyChanged();
                    GenerateSqlQuery();
                }
            }
        }

        public string Channel
        {
            get => _channel;
            set
            {
                if (_channel != value)
                {
                    _channel = value;
                    OnPropertyChanged();
                    GenerateSqlQuery();
                }
            }
        }

        public string Asset
        {
            get => _asset;
            set
            {
                if (_asset != value)
                {
                    _asset = value;
                    OnPropertyChanged();
                    GenerateSqlQuery();
                }
            }
        }

        public bool AllAssets
        {
            get => _allAssets;
            set
            {
                if (_allAssets != value)
                {
                    _allAssets = value;
                    OnPropertyChanged();
                    GenerateSqlQuery();
                }
            }
        }

        public string SqlQuery
        {
            get => _sqlQuery;
            private set
            {
                if (_sqlQuery != value)
                {
                    _sqlQuery = value;
                    OnPropertyChanged();
                }
            }
        }

        public GrafanaWizardViewModel()
        {
            GenerateSqlQuery();
        }

        public void GenerateSqlQuery()
        {
            string date = Date?.Trim() ?? "";
            string channel = Channel?.Trim() ?? "";
            string asset = AllAssets ? "" : (Asset?.Trim() ?? "");

            string query = @"SELECT * FROM kepware_opc_[TARİH]
WHERE ""TagName"" LIKE '%[CHANNEL]%[ASSET]%Outputs%Down%' OR ""TagName"" LIKE '%[CHANNEL]%[ASSET]%.-.Outputs.TransactionEnd%' OR ""TagName"" LIKE '%[CHANNEL]%[ASSET]%Alarm%'";

            query = query.Replace("[TARİH]", date);
            query = query.Replace("[CHANNEL]", channel);
            query = query.Replace("[ASSET]", asset);

            if (AllAssets)
            {
                query = query.Replace("%%", "%");
            }

            SqlQuery = query;
        }
    }
}
