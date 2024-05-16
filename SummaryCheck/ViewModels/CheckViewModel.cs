using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
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

        public CheckViewModel(
            AppStrings appStrings)
        {
            _appStrings = appStrings;
            _progressTipText = appStrings.StringWaitingForAction;

            BindingOperations.EnableCollectionSynchronization(ModifiedFiles, ModifiedFiles);
            BindingOperations.EnableCollectionSynchronization(DeletedFiles, DeletedFiles);
            BindingOperations.EnableCollectionSynchronization(AddedFiles, AddedFiles);
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

        [ObservableProperty]
        private string? _finishTipText;

        private Ookii.Dialogs.Wpf.VistaFolderBrowserDialog selectCheckFolderDialog = new()
        {
            Multiselect = false,
        };

        private Ookii.Dialogs.Wpf.VistaOpenFileDialog selectSummaryFileDialog = new()
        {
            CheckFileExists = true,
            Multiselect = false,
        };

        public ObservableCollection<string> ModifiedFiles { get; } = new();
        public ObservableCollection<string> DeletedFiles { get; } = new();
        public ObservableCollection<string> AddedFiles { get; } = new();


        [RelayCommand]
        public void SelectCheckFolderPath()
        {
            if (selectCheckFolderDialog.ShowDialog() ?? false)
            {
                CheckFolderPath = selectCheckFolderDialog.SelectedPath;
            }
        }


        [RelayCommand]
        public void SelectSummaryFilePath()
        {
            if (selectSummaryFileDialog.ShowDialog() ?? false)
            {
                SummaryFilePath = selectSummaryFileDialog.FileName;
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
                await Task.Run(async () =>
                {
                    await Task.Delay(1);
                    ProgressIndeterminate = true;
                    ProgressTipText = _appStrings.StringIndexing;
                    FinishTipText = null;

                    ModifiedFiles.Clear();
                    DeletedFiles.Clear();
                    AddedFiles.Clear();

                    var summaryFile = File.OpenRead(SummaryFilePath);
                    var memoryStream = new MemoryStream((int)summaryFile.Length);
                    await summaryFile.CopyToAsync(memoryStream);
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
                        cancellationToken.ThrowIfCancellationRequested();

                        if (file.EndsWith(".meta"))
                            continue;

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
                        var hash = md5.ComputeHash(fileStream);
                        var hashText = StringUtils.ToHexString(hash);

                        if (hashText != info.Md5)
                        {
                            ModifiedFiles.Add(file);
                        }


                        FinishedCount++;
                        ProgressValue = FinishedCount;
                    }

                    foreach (var remainingInfo in infoDictionary.Values)
                    {
                        var abstractPath = CheckFolderPath + remainingInfo.Path;
                        DeletedFiles.Add(abstractPath);
                    }

                    ProgressTipText = _appStrings.StringCompleted;

                    if (ModifiedFiles.Count == 0 && DeletedFiles.Count == 0 && AddedFiles.Count == 0)
                    {
                        FinishTipText = _appStrings.StringFilesNoProblem;
                    }
                    else
                    {
                        FinishTipText = _appStrings.StringFilesChanged;
                    }
                });
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
            finally
            {
                ProgressTipText = _appStrings.StringWaitingForAction;
                ProgressIndeterminate = false;
            }
        }

        [RelayCommand]
        public void CancelChecking()
        {
            CheckCommand.Cancel();
        }
    }
}
