using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;

namespace usuryCheck
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Jurisdiction> _jurisdictions = new();

        public MainWindow()
        {
            InitializeComponent();
            LoadJurisdictions();
            ResetForm();
        }

        private void LoadJurisdictions()
        {
            try
            {
                var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jurisdictions.json");
                if (!File.Exists(file))
                {
                    // if missing, create a minimal example file
                    var sample = new List<Jurisdiction>
                    {
                        new Jurisdiction { Code = "EXAMPLE", Name = "Exampleland", MaxAprPercent = 0.0, Description = "No cap configured (example)"}
                    };
                    File.WriteAllText(file, JsonSerializer.Serialize(sample, new JsonSerializerOptions { WriteIndented = true }));
                }

                var json = File.ReadAllText(file); _jurisdictions = JsonSerializer.Deserialize<List<Jurisdiction>>(json) ?? new List<Jurisdiction>();

                JurisdictionBox.Items.Clear();
                JurisdictionBox.Items.Add(new ComboBoxItemWrapper { Text = "— Select —", Value = null });
                foreach (var j in _jurisdictions)
                {
                    JurisdictionBox.Items.Add(new ComboBoxItemWrapper { Text = $"{j.Name} ({j.Code})", Value = j });
                }
                JurisdictionBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                SetResult("Failed to load jurisdictions.json: ",[ ex.Message]);
            }
        }

        private void ResetForm()
        {
            PrincipalBox.Text = "";
            AnnualRateBox.Text = "";
            TermBox.Text = "";
            FeesBox.Text = "0.00";
            IncludeFeesCheck.IsChecked = true;
            SetResult("Result goes here",  detailLines: null);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e) => ResetForm();

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            // Parse inputs
            if (!TryParseDecimal(PrincipalBox.Text, out decimal principal) || principal <= 0)
            {
                SetResult("Enter a valid principal > 0.", []);
                return;
            }
            if (!TryParseDecimal(AnnualRateBox.Text, out decimal nominalAnnualRate) || nominalAnnualRate < 0)
            {
                SetResult("Enter a valid annual rate (>= 0).", []);
                return;
            }
            if (!int.TryParse(TermBox.Text, out int months) || months <= 0)
            {
                SetResult("Enter a valid term in months (> 0).", []);
                return;
            }
            if (!TryParseDecimal(FeesBox.Text, out decimal fees) || fees < 0)
            {
                SetResult("Enter a valid fees amount (>= 0).", []);
                return;
            }

            bool includeFeesInApr = IncludeFeesCheck.IsChecked == true;

            // monthly interest decimal
            double nominalAnnual = (double)nominalAnnualRate / 100.0;
            double monthlyNominal = nominalAnnual / 12.0;

            // compute monthly payment (standard amortizing loan)
            double P = (double)principal;
            double payment;
            if (monthlyNominal == 0)
            {
                payment = P / months;
            }
            else
            {
                double r = monthlyNominal;
                double factor = Math.Pow(1 + r, months);
                payment = P * (r * factor) / (factor - 1);
            }

            // compute APR by solving IRR for the cashflows: CF0 = P - (includeFees ? fees : 0)
            double upfrontReceived = P - (includeFeesInApr ? (double)fees : 0.0);
            // cash flows: t=0: +upfrontReceived, t=1..months: -payment
            double aprAnnual = CalculateAnnualAprFromCashflows(upfrontReceived, -payment, months);

            // prepare details
            var detailLines = new List<string>
            {
                $"Nominal annual rate: {nominalAnnualRate.ToString("0.###", CultureInfo.InvariantCulture)}%",
                $"Monthly payment: {payment.ToString("N2", CultureInfo.InvariantCulture)}",
                $"APR {(includeFeesInApr ? "(fees included)" : "(fees excluded)")} : { (aprAnnual * 100.0).ToString("0.###", CultureInfo.InvariantCulture) }%"
            };

            // check jurisdiction
            Jurisdiction? selected = (Jurisdiction?)((JurisdictionBox.SelectedItem as ComboBoxItemWrapper)?.Value);
            string resultMessage;
            if (selected == null)
            {
                resultMessage = "No jurisdiction selected — cannot check legal cap.";
                
            }
            else if (selected.MaxAprPercent <= 0.0)
            {
                resultMessage = $"Jurisdiction {selected.Name} ({selected.Code}) has no cap configured in file.";
            }
            else
            {
                double cap = selected.MaxAprPercent / 100.0;
                if (aprAnnual > cap + 1e-9)
                {
                    resultMessage = $"Loan exceeds {selected.Name} cap ({selected.MaxAprPercent:0.##}%).";
                }
                else
                {
                    resultMessage = $"Loan complies with {selected.Name} cap ({selected.MaxAprPercent:0.##}%).";
                }
                detailLines.Add($"Jurisdiction cap: {selected.MaxAprPercent:0.###}%");
                if (!string.IsNullOrWhiteSpace(selected.Description))
                    detailLines.Add($"Note: {selected.Description}");
            }

            SetResult(resultMessage, detailLines);
        }

        private static bool TryParseDecimal(string s, out decimal value)
        {
            return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
                || decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out value);
        }

        private void SetResult(string message,  List<string> detailLines)
        {
            ResultMainText.Text = message;

            DetailLine1.Text = detailLines?.ElementAtOrDefault(0) ?? "—";
            DetailLine2.Text = detailLines?.ElementAtOrDefault(1) ?? "—";
            DetailLine3.Text = detailLines?.ElementAtOrDefault(2) ?? "—";
            DetailLine4.Text = detailLines?.ElementAtOrDefault(3) ?? "—";
        }


        /// <summary>
        /// Given an upfront received amount and a fixed periodic outgoing payment, find the APR (annual effective) by solving IRR.
        /// Returns annual rate as decimal (e.g. 0.123 means 12.3% APR).
        /// CF0 = received (positive), CF1..n = outflows (negative).
        /// Uses Brent-style safe root-finding by searching for periodic rate r that solves NPV = 0,
        /// then converts to annual effective as (1+r)^12 - 1.
        /// </summary>
        private double CalculateAnnualAprFromCashflows(double upfrontReceived, double periodicOutflow, int months)
        {
            // NPV(r) = upfrontReceived + sum_{t=1..months} periodicOutflow / (1+r)^t
            // we want NPV = 0. unknown: r (periodic rate)
            // handle trivial zero case:
            if (Math.Abs(periodicOutflow) < 1e-12) return 0.0;

            Func<double, double> npv = (r) =>
            {
                if (r <= -1.0) return double.PositiveInfinity;
                double sum = upfrontReceived;
                double denom = 1.0 + r;
                double disc = 1.0;
                for (int t = 1; t <= months; t++)
                {
                    disc *= denom;
                    sum += periodicOutflow / disc;
                }
                return sum;
            };

            // search interval for r (monthly)
            double low = -0.9999999;
            double high = 10.0; // 1000% per month improbable
            double fLow = npv(low);
            double fHigh = npv(high);

            // expand high until sign changes
            int expand = 0;
            while (fLow * fHigh > 0 && expand < 200)
            {
                high *= 2;
                fHigh = npv(high);
                expand++;
            }

            if (fLow * fHigh > 0)
            {
                // fallback: use approximate rate via formula: if payments amortize principal with nominal rate -> compute monthly nominal from payment formula inverse:
                // approximate monthly rate = (payment/principal) for small terms
                double approxMonthly = Math.Max(0.0, Math.Pow((upfrontReceived / (upfrontReceived + periodicOutflow * months)), 1.0 / months) - 1.0);
                return approxMonthly * 12.0;
            }

            // bisection
            double a = low, b = high;
            double fa = fLow, fb = fHigh;
            for (int iter = 0; iter < 200; iter++)
            {
                double c = 0.5 * (a + b);
                double fc = npv(c);
                if (Math.Abs(fc) < 1e-12 || (b - a) < 1e-12) // converged
                {
                    double annual = Math.Pow(1.0 + c, 12.0) - 1.0;
                    return annual;
                }

                // choose side
                if (fa * fc <= 0)
                {
                    b = c; fb = fc;
                }
                else
                {
                    a = c; fa = fc;
                }
            }

            // fail: return best midpoint
            double mid = 0.5 * (a + b);
            return Math.Pow(1.0 + mid, 12.0) - 1.0;
        }
    }
    public class ComboBoxItemWrapper
    {
        public string Text { get; set; }
        public object Value { get; set; }
        public override string ToString() => Text;
    }
}