using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SummaryCheck.Models;
using SummaryCheck.Strings;
using SummaryCheck.Utilities;

namespace SummaryCheck.ViewModels
{
    public partial class BuildViewModel : ObservableObject
    {
        private readonly AppStrings _appStrings;
        private readonly char[] _pathSeparators = new char[]{ '/', '\\' };

        public BuildViewModel(
            AppStrings appStrings)
        {
            _appStrings = appStrings;
            _progressTipText = appStrings.StringWaitingForAction;
        }

        [ObservableProperty]
        private string? _checkFolderPath;

        [ObservableProperty]
        private bool _progressIndeterminate;

        [ObservableProperty]
        private string _progressTipText;

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

        private Ookii.Dialogs.Wpf.VistaSaveFileDialog saveFileDialog = new()
        {
            CheckPathExists = true,
            Filter = "Any|*.*",
        };

        [RelayCommand]
        public void SelectCheckFolderPath()
        {
            if (selectCheckFolderDialog.ShowDialog() ?? false)
            {
                CheckFolderPath = selectCheckFolderDialog.SelectedPath;
            }
        }

        [RelayCommand]
        public async Task BuildAsync(CancellationToken cancellationToken)
        {
            if (!Directory.Exists(CheckFolderPath))
            {
                MessageBox.Show(Application.Current.MainWindow, _appStrings.StringPleaseSelectAExistFolder, _appStrings.StringError, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var now = DateTime.Now;
            var folderName = System.IO.Path.GetFileName(CheckFolderPath);
            saveFileDialog.FileName = $"Y{now.Year}M{now.Month}D{now.Day}-H{now.Hour}M{now.Minute}S{now.Second}-{folderName}.bytes";

            var dialogResult = saveFileDialog.ShowDialog() ?? false;
            if (!dialogResult)
            {
                return;
            }

            try
            {
                await Task.Run(async () =>
                {
                    var outputFilePath = saveFileDialog.FileName;
                    await Task.Delay(1);

                    FinishTipText = null;
                    ProgressIndeterminate = true;
                    ProgressTipText = _appStrings.StringIndexing;
                    var files = Directory.GetFiles(CheckFolderPath, "*.*", SearchOption.AllDirectories);
                    var infos = new List<Md5Info>();
                    var md5 = MD5.Create();

                    ProgressIndeterminate = false;
                    ProgressMinimum = 0;
                    ProgressMaximum = files.Length;
                    FinishedCount = 0;
                    TotalCount = files.Length;

                    ProgressTipText = _appStrings.StringBuilding;
                    foreach (var file in files)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (file.EndsWith(".meta"))
                            continue;

                        CurrentFile = file;

                        using var stream = File.OpenRead(file);
                        var relativePath = file.Replace(CheckFolderPath, null);
                        var hash = md5.ComputeHash(stream);
                        var hashText = StringUtils.ToHexString(hash);

                        infos.Add(
                            new Md5Info()
                            {
                                Path = relativePath,
                                Md5 = hashText
                            });

                        FinishedCount++;
                        ProgressValue = FinishedCount;
                    }

                    CurrentFile = null;

                    ProgressTipText = _appStrings.StringSaving;
                    using var outputFile = File.Create(outputFilePath);
                    using var memoryStream = new MemoryStream();
                    await JsonSerializer.SerializeAsync(memoryStream, infos, cancellationToken: cancellationToken);

                    var buffer = memoryStream.GetBuffer();
                    await BinaryUtils.XorBytesAsync(buffer, memoryStream.Length, 66);

                    memoryStream.Seek(0, SeekOrigin.Begin);
                    await memoryStream.CopyToAsync(outputFile);

                    ProgressTipText = _appStrings.StringCompleted;
                    FinishTipText = _appStrings.StringCompleted;
                });
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
        public void CancelBuilding()
        {
            BuildCommand.Cancel();
        }
    }
}
