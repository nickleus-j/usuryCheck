using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace usuryCheck
{
    
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ObservableCollection<SupportedJurisdiction> Jurisdictions { get; } =
            new ObservableCollection<SupportedJurisdiction>((SupportedJurisdiction[])Enum.GetValues(typeof(SupportedJurisdiction)));

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

        private SupportedJurisdiction selectedJurisdiction = SupportedJurisdiction.None;
        public SupportedJurisdiction SelectedJurisdiction
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
            var (capPercent, label) = GetCapForJurisdiction(SelectedJurisdiction);
            var complies = effectiveAPR <= capPercent;

            ResultTitle = complies
                ? $"Loan complies with {label}."
                : $"Potential usury under {label}.";

            ResultDetail =
                $"Computed APR: {effectiveAPR:F2}% (Nominal: {annualRatePercent:F2}%, Fees included: {IncludeFeesInAPR})\n" +
                $"Jurisdiction cap: {capPercent:F2}%.\n" +
                $"Inputs — Principal: {principal:C}, Term: {termMonths} months, Up-front Fees: {upfrontFees:C}.";
        }

        private (decimal capPercent, string label) GetCapForJurisdiction(SupportedJurisdiction j)
        {
            return j switch
            {
                SupportedJurisdiction.US_Federal => (36m, "U.S. federal (typical consumer cap proxy)"),
                SupportedJurisdiction.Philippines => (60m, "Philippines (placeholder cap)"),
                SupportedJurisdiction.EU_Generic => (20m, "EU generic (illustrative)"),
                _ => (100m, "No selected jurisdiction (no cap applied)")
            };
        }

        private void Reset()
        {
            Principal = "0";
            TermMonths = "0";
            AnnualRatePercent = "0";
            UpfrontFees = "0";
            IncludeFeesInAPR = false;
            SelectedJurisdiction = SupportedJurisdiction.None;
            ResultTitle = "Result goes here – e.g. \"Loan complies with XYZ law.\"";
            ResultDetail = string.Empty;
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
}
