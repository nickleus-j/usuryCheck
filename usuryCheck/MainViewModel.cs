using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using System.IO;
using System.Text.Json;

namespace usuryCheck
{
    
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ObservableCollection<ComboBoxItemWrapper> Jurisdictions { get; } = WrapJurisdictions();

        private decimal principal;
        public string Principal
        {
            get => principal.ToString();
            set
            {
                principal = ParseDecimal(value);
                OnPropertyChanged();
            }
        }
        private static IList<Jurisdiction> LoadJurisdictions()
        {
            try
            {
                var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jurisdictions.json");
                if (!File.Exists(file))
                {
                    // if missing, create a minimal example file
                    var sample = new List<Jurisdiction>
                {
                    new Jurisdiction { Code = "EXAMPLE", Name = "Example land", MaxAprPercent = 100m, Description = "100 cap configured (example)"}
                };
                    File.WriteAllText(file, JsonSerializer.Serialize(sample, new JsonSerializerOptions { WriteIndented = true }));
                }

                var json = File.ReadAllText(file); 
                var _jurisdictions = JsonSerializer.Deserialize<List<Jurisdiction>>(json) ?? new List<Jurisdiction>();

                return _jurisdictions;
            }
            catch (Exception ex)
            {
                return new List<Jurisdiction>();
            }
        }
        static ObservableCollection<ComboBoxItemWrapper> WrapJurisdictions()
        {
            ObservableCollection<ComboBoxItemWrapper> wrapped= new ObservableCollection<ComboBoxItemWrapper>();
            IList<Jurisdiction> givenJurisdictions = LoadJurisdictions();
            foreach (var j in givenJurisdictions)
            {
                wrapped.Add(new ComboBoxItemWrapper { Text = $"{j.Name} ({j.Code})", Value = j }); 
            }
            return wrapped;
        }
        private int termMonths;
        public string TermMonths
        {
            get => termMonths.ToString();
            set
            {
                termMonths = ParseInt(value);
                OnPropertyChanged();
            }
        }
        public int TermYears => termMonths / 12;
        private decimal annualRatePercent;
        public string AnnualRatePercent
        {
            get => annualRatePercent.ToString();
            set
            {
                annualRatePercent = ParseDecimal(value);
                OnPropertyChanged();
            }
        }

        private decimal upfrontFees;
        public string UpfrontFees
        {
            get => upfrontFees.ToString();
            set
            {
                upfrontFees = ParseDecimal(value);
                OnPropertyChanged();
            }
        }

        private bool includeFeesInAPR;
        public bool IncludeFeesInAPR
        {
            get => includeFeesInAPR;
            set { includeFeesInAPR = value; OnPropertyChanged(); }
        }

        private ComboBoxItemWrapper selectedJurisdiction = new ComboBoxItemWrapper { Text=""};
        public ComboBoxItemWrapper SelectedJurisdiction
        {
            get => selectedJurisdiction;
            set { selectedJurisdiction = value; OnPropertyChanged(); }
        }

        private string resultTitle = "Result goes here – e.g. \"Loan complies with XYZ law.\"";
        public string ResultTitle
        {
            get => resultTitle;
            set { resultTitle = value; OnPropertyChanged(); }
        }

        private string resultDetail = string.Empty;
        public string ResultDetail
        {
            get => resultDetail;
            set { resultDetail = value; OnPropertyChanged(); }
        }

        public ICommand CalculateCommand { get; }
        public ICommand ResetCommand { get; }

        public MainViewModel()
        {
            CalculateCommand = new RelayCommand(_ => Calculate());
            ResetCommand = new RelayCommand(_ => Reset());
        }

        private void Calculate()
        {
            // Validate
            if (principal <= 0 || termMonths <= 0)
            {
                ResultTitle = "Missing or invalid inputs.";
                ResultDetail = "Please enter positive values for principal and term.";
                return;
            }

            var nominalAPR = annualRatePercent / 100m; // as decimal rate
            var monthlyRate = nominalAPR / 12m;

            // Effective APR approximation including fees: spread upfront fees over term
            var financedAmount = principal;
            var totalFees = IncludeFeesInAPR ? upfrontFees : 0m;
            
            // Simple effective rate: add fee amortization as a monthly cost factor
            var monthlyFeeRate = totalFees / financedAmount / termMonths; // rough allocation
            var effectiveMonthlyRate = monthlyRate + monthlyFeeRate;
            var effectiveAPR = effectiveMonthlyRate * 12m * 100m; // back to percent

            // Compare to jurisdiction cap (placeholder values; replace with actual legal caps)
            var (capPercent, label) = GetCapForJurisdiction((Jurisdiction)selectedJurisdiction.Value);
            var complies = effectiveAPR <= capPercent;

            ResultTitle = complies
                ? $"Loan complies with {label}."
                : $"Potential usury under {label}.";

            ResultDetail =
                $"Computed APR: {effectiveAPR:F2}% (Nominal: {annualRatePercent:F2}%, Fees included: {IncludeFeesInAPR})\n" +
                $"Jurisdiction cap: {capPercent:F2}%.\n" +
                $"Inputs — Principal: {CurrentCurrency} {principal}, Term: {termMonths} months, Up-front Fees: {upfrontFees:C}.";
        }

        private (decimal capPercent, string label) GetCapForJurisdiction(Jurisdiction j)
        {
            return (j.MaxAprPercent, j.Name);
        }

        private void Reset()
        {
            Principal = "0";
            TermMonths = "0";
            AnnualRatePercent = "0";
            UpfrontFees = "0";
            IncludeFeesInAPR = false;
            SelectedJurisdiction = WrapJurisdictions().First();
            ResultTitle = "Result goes here – e.g. \"Loan complies with XYZ law.\"";
            ResultDetail = string.Empty;
        }
        private string _currentCurrency = "USD"; // Default value
        public string CurrentCurrency
        {
            get { return _currentCurrency; }
            set
            {
                _currentCurrency = value;
                OnPropertyChanged();
            }
        }

        private static decimal ParseDecimal(string? s)
            => decimal.TryParse(s, out var v) ? v : 0m;

        private static int ParseInt(string? s)
            => int.TryParse(s, out var v) ? v : 0;
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute; _canExecute = canExecute;
        }
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged;
    }
    public class ComboBoxItemWrapper
    {
        public string Text { get; set; }
        public object Value { get; set; }
        public override string ToString() => Text;
    }
}
