using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;
using SummaryCheck.Models;
using SummaryCheck.Strings;
using SummaryCheck.ViewModels;

namespace SummaryCheck
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;

        public MainWindow(
            AppStrings appStrings,
            MainViewModel viewModel,
            IServiceProvider serviceProvider)
        {
            AppStrings = appStrings;
            ViewModel = viewModel;
            _serviceProvider = serviceProvider;
            DataContext = this;

            InitializeComponent();
        }

        public AppStrings AppStrings { get; }
        public MainViewModel ViewModel { get; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedNavigationItem = ViewModel.NavigationItems.FirstOrDefault();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && 
                e.AddedItems[0] is NavigationItem navigationItem &&
                navigationItem.PageType != null)
            {
                AppFrame.Navigate(_serviceProvider.GetRequiredService(navigationItem.PageType));
            }
            else
            {
                AppFrame.Navigate(null);
            }
        }
    }
}