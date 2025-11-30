using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.ComponentModel;

namespace usuryCheck
{
    public class CurrencyViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<string> Currencies { get; }

        private string selectedCurrency;
        public string SelectedCurrency
        {
            get => selectedCurrency;
            set
            {
                if (selectedCurrency != value)
                {
                    selectedCurrency = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCurrency)));
                }
            }
        }

        public CurrencyViewModel()
        {
            // Get all specific cultures installed on the machine
            var cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);

            //var currencies = new Dictionary<string, (string Symbol, string EnglishName)>();
            var currencyList = cultures
                                .Select(ci => new RegionInfo(ci.Name))
                                .GroupBy(r => r.ISOCurrencySymbol)
                                .Select(g => g.First())
                                .OrderBy(sym=> sym.ISOCurrencySymbol)
                                .Select(r => r.ISOCurrencySymbol).ToList();
            
            Currencies = new ObservableCollection<string>(currencyList);
            // Default selection
            SelectedCurrency = "USD";
        }
        public string CurrencySymbol
        {
            get { return SelectedCurrency; }
            set
            {
                SelectedCurrency = value;
                OnPropertyChanged(nameof(CurrencySymbol));
            }
        }


        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}