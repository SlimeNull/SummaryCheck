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
    /// BuildPage.xaml 的交互逻辑
    /// </summary>
    public partial class BuildPage : Page
    {
        public BuildPage(
            AppStrings appStrings,
            BuildViewModel viewModel)
        {
            AppStrings = appStrings;
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }

        public AppStrings AppStrings { get; }
        public BuildViewModel ViewModel { get; }

        private void TextBox_Drop(object sender, DragEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    textBox.Text = files[0];
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                var str = e.Data.GetData(DataFormats.StringFormat).ToString();
                textBox.Text = str;
            }
        }

        private void TextBox_DragEnterOrOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) ||
                e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

    }
}
