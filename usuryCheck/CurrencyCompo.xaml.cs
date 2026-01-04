using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace usuryCheck
{
    /// <summary>
    /// Interaction logic for CurrencyCompo.xaml
    /// </summary>
    public partial class CurrencyCompo : UserControl
    {
        public CurrencyCompo()
        {
            InitializeComponent();
            DataContext = new CurrencyViewModel();
        }
        public string SelectedCurrency {
            get {return ((CurrencyViewModel)DataContext).SelectedCurrency; }
            set { ((CurrencyViewModel)DataContext).SelectedCurrency = value; }
        }
        public static readonly DependencyProperty CurrencyProperty =
            DependencyProperty.Register("Currency", typeof(string), typeof(CurrencyCompo),
                new PropertyMetadata("USD")); // Default value

        public string Currency
        {
            get { return SelectedCurrency; }
            set { SetValue(CurrencyProperty, value); SelectedCurrency = value; }
        }

        public string CurrencySymbol
        {
            get { return (string)GetValue(CurrencyProperty); }
            set { SetValue(CurrencyProperty, value); }
        }

    }
}
