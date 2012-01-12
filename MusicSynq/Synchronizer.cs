using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MusicSynq
{
    public class Synchronizer
    {
        private const int NumRetries = 25;
        private MusicSynqViewModel _viewModel;
        private readonly string _libraryPath;
        private readonly string _devicePath;
        private readonly string _extensionPath;
        private List<string> _deviceFiles;
        private List<string> _libraryFiles;

        public Synchronizer(MusicSynqViewModel viewModel, string libraryPath, string devicePath, string extensionPath)
        {
            _viewModel = viewModel;
            _libraryPath = libraryPath;
            _devicePath = devicePath;
            _extensionPath = extensionPath;
        }

        public void AnalyzeAndSynchronize()
        {
            AnalyzeDirectories();
            DeleteOldFiles();
            Synchronize();
        }

        private void AnalyzeDirectories()
        {
            _deviceFiles = new List<string>(CreateFileSet(_devicePath + "\\Music").Concat(CreateFileSet(_extensionPath + "\\Music")));
            UpdateStatusLabel(string.Format("{0} files read from device.", _deviceFiles.Count));
            Thread.Sleep(1000);

            _libraryFiles = CreateFileSet(_libraryPath).ToList();
            UpdateStatusLabel(string.Format("{0} files read from library.", _libraryFiles.Count));
            Thread.Sleep(1000);
        }

        private void DeleteOldFiles()
        {
            var filesToDelete = _deviceFiles.Except(_libraryFiles).AsParallel().ToList();

            foreach (var fileName in filesToDelete)
            {
                _deviceFiles.Remove(fileName);

                UpdateStatusLabel("Deleting file: " + fileName);

                if (File.Exists(_devicePath + fileName))
                {
                    File.Delete(_devicePath + "Music\\" + fileName);
                }
                else if (File.Exists(_extensionPath + "Music\\" + fileName))
                {
                    File.Delete(_extensionPath + "Music\\" + fileName);
                }
                else
                {
                    ErrorLog.Write(fileName + " was queued for deletion, but was not found on the device");
                }
            }
        }

        private void Synchronize()
        {
            var filesToAdd = _libraryFiles.Except(_deviceFiles).ToList();
            var numRetries = NumRetries;
            var filesProcessed = 0;

            SetProgressBarMaximum(filesToAdd.Count);

            foreach (var file in filesToAdd)
            {
                var driveInfo = new DriveInfo(_devicePath);

                try
                {
                    var sample = TagLib.File.Create(_libraryPath + "\\" + file);

                    if (sample.Properties.Duration.TotalSeconds > 0)
                    {
                        if (sample.Tag.Pictures.Length > 0)
                            SetAlbumArtHolderImage(sample.Tag.Pictures[0].Data.Data);
                    }   
                }
                catch (Exception ex)
                {
                    ErrorLog.Write(ex);
                }

                while (numRetries-- > 0)
                {
                    try
                    {
                        var fileInfo = new FileInfo(_libraryPath + "\\" + file);

                        if (driveInfo.AvailableFreeSpace - fileInfo.Length > 104857600)
                        {
                            UpdateDeviceAvailableSpace(driveInfo.AvailableFreeSpace - fileInfo.Length);

                            var destinationFilePath = _devicePath + "Music\\" + file;

                            var directoryPath = destinationFilePath.Substring(0, destinationFilePath.LastIndexOf("\\"));

                            if (!Directory.Exists(directoryPath))
                                Directory.CreateDirectory(directoryPath);

                            UpdateStatusLabel("Moving file " + fileInfo.Name);

                            File.Copy(fileInfo.FullName, destinationFilePath, true);
                        }
                        else if ((driveInfo = new DriveInfo(_extensionPath)).AvailableFreeSpace > fileInfo.Length)
                        {
                            UpdateExtensionAvailableSpace(driveInfo.AvailableFreeSpace - fileInfo.Length);

                            var destinationFilePath = _extensionPath + "Music\\" + file;

                            var directoryPath = destinationFilePath.Substring(0, destinationFilePath.LastIndexOf("\\"));

                            if (!Directory.Exists(directoryPath))
                                Directory.CreateDirectory(directoryPath);
                            
                            UpdateStatusLabel("Moving file " + fileInfo.Name);

                            File.Copy(fileInfo.FullName, destinationFilePath, true);
                        }
                        else
                        {
                            UpdateStatusLabel("Not enough space");
                            return;
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        UpdateStatusLabel("!!!!!!!!!!!!!Failed to write: " + file);
                        ErrorLog.Write(ex);
                        Thread.Sleep(3000);
                        continue;
                    }
                }

                UpdateProgressBar(++filesProcessed);
            }

            UpdateStatusLabel("Done!");
        }

        private void SetProgressBarMaximum(int max)
        {
            _viewModel.ProgressBarMaximum = _viewModel.TotalToProcessCount = max;
        }

        private void UpdateProgressBar(int progress)
        {
            _viewModel.ProcessedCount = progress;
        }

        private void UpdateExtensionAvailableSpace(long availableSpace)
        {
            var driveInfo = new DriveInfo(_extensionPath);
            var progressBarWidth = GetProgressBarWidth(availableSpace, driveInfo.TotalSize);

            var backgroundBrush = GetBackgroundBrushColor(progressBarWidth);

            backgroundBrush.Freeze();

            _viewModel.ExtensionSize = FileOperations.FormatBytes(availableSpace);
            _viewModel.ExtensionProgressBarWidth = progressBarWidth;
            _viewModel.ExtensionProgressBarColor = backgroundBrush;
        }

        private void UpdateDeviceAvailableSpace(long availableSpace)
        {
            var driveInfo = new DriveInfo(_devicePath);
            var progressBarWidth = GetProgressBarWidth(availableSpace, driveInfo.TotalSize);

            var backgroundBrush = GetBackgroundBrushColor(progressBarWidth);

            backgroundBrush.Freeze();

            _viewModel.DeviceSize = FileOperations.FormatBytes(availableSpace);
            _viewModel.DeviceProgressBarWidth = progressBarWidth;
            _viewModel.DeviceProgressBarColor = backgroundBrush;
        }
        
        private double GetProgressBarWidth(double availableSpace, double totalSize)
        {
            return 212 - 212 * (availableSpace / totalSize);
        }

        private SolidColorBrush GetBackgroundBrushColor(double progressBarWidth)
        {
            return new SolidColorBrush(Color.FromRgb((byte)progressBarWidth, (byte)(212 - progressBarWidth), 0))
            {
                Opacity = 0.67
            };
        }

        private IEnumerable<string> CreateFileSet(string folderPath)
        {
            var allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

            foreach (var fileName in allFiles)
            {
                var indexOfExt = fileName.LastIndexOf('.');

                UpdateStatusLabel("Analyzing file: " + fileName);

                if (FileOperations.ValidFileTypes.Contains(fileName.Substring(indexOfExt + 1, fileName.Length - indexOfExt - 1).ToUpper()))
                {
                    indexOfExt = fileName.IndexOf("Music\\");
                    yield return fileName.Substring(indexOfExt + 6, fileName.Length - indexOfExt - 6);
                }
            }
        }

        private void UpdateStatusLabel(string msg)
        {
            _viewModel.Status = msg;
        }

        private void SetAlbumArtHolderImage(byte[] imageBytes)
        {
            if (imageBytes.Length < 1)
                return;

            var bitmapImg = new BitmapImage();

            bitmapImg.BeginInit();

            var ms = new MemoryStream(imageBytes);
            bitmapImg.StreamSource = ms;

            bitmapImg.EndInit();

            bitmapImg.Freeze();

            _viewModel.AlbumArt = bitmapImg;
        }
    }
}