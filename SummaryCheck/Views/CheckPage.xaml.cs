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
using SummaryCheck.Strings;
using SummaryCheck.ViewModels;

namespace SummaryCheck.Views
{
    /// <summary>
    /// CheckPage.xaml 的交互逻辑
    /// </summary>
    public partial class CheckPage : Page
    {
        public CheckPage(
            AppStrings appStrings,
            CheckViewModel viewModel)
        {
            AppStrings = appStrings;
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }

        public AppStrings AppStrings { get; }
        public CheckViewModel ViewModel { get; }
    }
}
