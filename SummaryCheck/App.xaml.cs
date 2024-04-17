using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SummaryCheck.Strings;
using SummaryCheck.ViewModels;
using SummaryCheck.Views;

namespace SummaryCheck
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider Services { get; }
            = BuildServiceProvider();

        private static IServiceProvider BuildServiceProvider()
        {
            return new ServiceCollection()
                .AddSingleton<AppStrings>()
                .AddSingleton<MainWindow>()
                .AddSingleton<BuildPage>()
                .AddSingleton<CheckPage>()
                .AddSingleton<AboutPage>()
                .AddSingleton<MainViewModel>()
                .AddSingleton<BuildViewModel>()
                .AddSingleton<CheckViewModel>()
                .AddSingleton<AboutViewModel>()
                .BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Services
                .GetRequiredService<MainWindow>()
                .Show();
        }
    }

}
