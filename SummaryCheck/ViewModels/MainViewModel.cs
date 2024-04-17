using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SummaryCheck.Models;
using SummaryCheck.Strings;
using SummaryCheck.Views;

namespace SummaryCheck.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly AppStrings _appStrings;

        public MainViewModel(AppStrings appStrings)
        {
            _appStrings = appStrings;

            NavigationItems = new()
            {
                new NavigationItem()
                {
                    Title = _appStrings.StringBuild,
                    Description = _appStrings.StringBuildPageDescription,
                    PageType = typeof(BuildPage)
                },
                new NavigationItem()
                {
                    Title = _appStrings.StringCheck,
                    Description = _appStrings.StringCheckPageDescription,
                    PageType = typeof(CheckPage)
                },
                new NavigationItem()
                {
                    Title = _appStrings.StringAbout,
                    Description = _appStrings.StringAboutPageDescription,
                    PageType = typeof(AboutPage)
                }
            };
        }

        public ObservableCollection<NavigationItem> NavigationItems { get; }

        [ObservableProperty]
        private NavigationItem? _selectedNavigationItem;
    }
}
