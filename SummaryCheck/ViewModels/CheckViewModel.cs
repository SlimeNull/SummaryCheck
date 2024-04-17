using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SummaryCheck.Models;
using SummaryCheck.Strings;
using SummaryCheck.Utilities;

namespace SummaryCheck.ViewModels
{
    public partial class CheckViewModel : ObservableObject
    {
        private readonly AppStrings _appStrings;
        private readonly char[] _pathSeparators = new char[]{ '/', '\\' };

        public CheckViewModel(
            AppStrings appStrings)
        {
            _appStrings = appStrings;
            _progressTipText = appStrings.StringWaitingForAction;
        }


        [ObservableProperty]
        private string? _checkFolderPath;

        [ObservableProperty]
        private string? _summaryFilePath;

        [ObservableProperty]
        private string _progressTipText;

        [ObservableProperty]
        private bool _progressIndeterminate;

        [ObservableProperty]
        private float _progressMinimum;

        [ObservableProperty]
        private float _progressMaximum;

        [ObservableProperty]
        private float _progressValue;

        [ObservableProperty]
        private int _finishedCount;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private string? _currentFile;

        public ObservableCollection<string> ModifiedFiles { get; } = new();
        public ObservableCollection<string> DeletedFiles { get; } = new();
        public ObservableCollection<string> AddedFiles { get; } = new();


        [RelayCommand]
        public void SelectCheckFolderPath()
        {
            Ookii.Dialogs.Wpf.VistaFolderBrowserDialog dialog = new()
            {
                Multiselect = false,
            };

            if (dialog.ShowDialog() ?? false)
            {
                CheckFolderPath = dialog.SelectedPath;
            }
        }


        [RelayCommand]
        public void SelectSummaryFilePath()
        {
            Ookii.Dialogs.Wpf.VistaOpenFileDialog dialog = new()
            {
                CheckFileExists = true,
                Multiselect = false,
            };

            if (dialog.ShowDialog() ?? false)
            {
                SummaryFilePath = dialog.FileName;
            }
        }


        [RelayCommand]
        public async Task CheckAsync(CancellationToken cancellationToken)
        {
            if (!Directory.Exists(CheckFolderPath))
            {
                MessageBox.Show(Application.Current.MainWindow, _appStrings.StringPleaseSelectAExistFolder, _appStrings.StringError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!File.Exists(SummaryFilePath))
            {
                MessageBox.Show(Application.Current.MainWindow, _appStrings.StringPleaseSelectAExistSummaryFile, _appStrings.StringError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                await Task.Delay(1);
                ProgressIndeterminate = true;
                ProgressTipText = _appStrings.StringIndexing;

                ModifiedFiles.Clear();
                DeletedFiles.Clear();
                AddedFiles.Clear();

                var summaryFile = File.OpenRead(SummaryFilePath);
                var memoryStream = new MemoryStream((int)summaryFile.Length);
                await summaryFile.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var buffer = memoryStream.GetBuffer();
                await BinaryUtils.XorBytesAsync(buffer, memoryStream.Length, 66);

                var files = Directory.GetFiles(CheckFolderPath, "*.*", SearchOption.AllDirectories);
                var infos = await JsonSerializer.DeserializeAsync<List<Md5Info>>(memoryStream, cancellationToken: cancellationToken);
                var md5 = MD5.Create();

                if (infos is null)
                {
                    MessageBox.Show(Application.Current.MainWindow, _appStrings.StringInvalidSummaryFile, _appStrings.StringError, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var infoDictionary = new Dictionary<string, Md5Info>(infos.Count);
                foreach (var info in infos)
                {
                    infoDictionary[info.Path] = info;
                }

                ProgressIndeterminate = false;
                ProgressMinimum = 0;
                ProgressMaximum = files.Length;
                FinishedCount = 0;
                TotalCount = files.Length;

                ProgressTipText = _appStrings.StringChecking;
                foreach (var file in files)
                {
                    var relativePath = file.Replace(CheckFolderPath, null);

                    if (!infoDictionary.TryGetValue(relativePath, out var info))
                    {
                        AddedFiles.Add(file);

                        FinishedCount++;
                        ProgressValue = FinishedCount;
                        continue;
                    }

                    infoDictionary.Remove(relativePath);

                    using var fileStream = File.OpenRead(file);
                    var hash = await md5.ComputeHashAsync(fileStream, cancellationToken);
                    var hashText = Convert.ToHexString(hash);

                    if (hashText != info.Md5)
                    {
                        ModifiedFiles.Add(file);
                    }

                    FinishedCount++;
                    ProgressValue = FinishedCount;
                }

                foreach (var remainingInfo in infoDictionary.Values)
                {
                    var abstractPath = Path.Combine(CheckFolderPath, remainingInfo.Path);
                    DeletedFiles.Add(abstractPath);
                }

                ProgressTipText = _appStrings.StringCompleted;
            }
            catch (JsonException)
            {
                MessageBox.Show(Application.Current.MainWindow, _appStrings.StringInvalidSummaryFile, _appStrings.StringError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            catch (System.IO.IOException)
            {
                MessageBox.Show(Application.Current.MainWindow, _appStrings.StringIOExceptionMessage, _appStrings.StringError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(Application.Current.MainWindow, _appStrings.StringOperationCancelled, _appStrings.StringError, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            catch (Exception e)
            {
                MessageBox.Show(Application.Current.MainWindow, e.Message, _appStrings.StringError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        [RelayCommand]
        public void CancelChecking()
        {
            CheckCommand.Cancel();
        }
    }
}
