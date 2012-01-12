using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MusicSynq
{
    public class MusicSynqViewModel : INotifyPropertyChanged
    {
        private string _libraryPath;
        private string _devicePath;
        private string _extensionPath;
        private string _librarySize;
        private string _deviceSize;
        private string _extensionSize;
        private string _status;
        private readonly ICommand _performSyncCommand;
        private BitmapImage _albumArt;
        private int _progressBarMaximum;
        private int _processedCount;
        private int _totalToProcessCount;
        private SolidColorBrush _deviceProgressBarColor;
        private SolidColorBrush _extensionProgressBarColor;
        private double _extensionProgressBarWidth;
        private double _deviceProgressBarWidth;

        public MusicSynqViewModel()
        {
            var setting = Properties.Settings.Default.LibraryPath;

            LibraryPath = (!string.IsNullOrEmpty(setting)) ? setting : Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

            GetDrivePaths();

            _performSyncCommand = new BackgroundCommand(AllDrivesValid, p => PerformSync(Application.Current.Dispatcher));
        }

        private bool AllDrivesValid()
        {
            return Directory.Exists(_libraryPath) && Directory.Exists(_devicePath) && Directory.Exists(_extensionPath);
        }

        private void GetDrivePaths()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                var volumeName = GetVolumeName(drive.Name.Substring(0, 1));

                if (Properties.Settings.Default.DeviceName == volumeName)
                {
                    DevicePath = drive.Name;
                    continue;
                }

                if (Properties.Settings.Default.ExtensionName == volumeName)
                {
                    ExtensionPath = drive.Name;
                }
            }
        }

        private string GetVolumeName(string drivePath)
        {
            return (new DriveInfo(drivePath)).Name;
        }

        private void PerformSync(Dispatcher uiDispatcher)
        {
            var synchronizer = new Synchronizer(this, _libraryPath, _devicePath, _extensionPath);

            synchronizer.AnalyzeAndSynchronize();
        }

        public string LibraryPath
        {
            get { return _libraryPath; }
            set
            {
                if (!Directory.Exists(value))
                {
                    ErrorLog.Write("Library directory does not exist.");
                    return;
                }
                if (_libraryPath == value) return;

                _libraryPath = value;
                Properties.Settings.Default.LibraryPath = value;

                LibrarySize = FileOperations.FormatBytes(GetSizeOfFiles(value));

                NotifyPropertyChanged("LibraryPath");
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string DevicePath
        {
            get { return _devicePath; }
            set
            {
                if(!Directory.Exists(value))
                {
                    ErrorLog.Write("Device directory does not exist.");
                    return;
                }
                if (_devicePath == value) return;

                _devicePath = value;
                Properties.Settings.Default.DeviceName = GetVolumeName(value.Substring(0, 1));

                DeviceSize = FileOperations.FormatBytes((new DriveInfo(value)).AvailableFreeSpace);

                NotifyPropertyChanged("DevicePath");
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string ExtensionPath
        {
            get { return _extensionPath; }
            set
            {
                if(!Directory.Exists(value))
                {
                    ErrorLog.Write("Extension directory does not exist.");
                    return;
                }
                if (_extensionPath == value) return;

                _extensionPath = value;
                Properties.Settings.Default.ExtensionName = GetVolumeName(value.Substring(0, 1));

                ExtensionSize = FileOperations.FormatBytes((new DriveInfo(value)).AvailableFreeSpace);

                NotifyPropertyChanged("ExtensionPath");
                CommandManager.InvalidateRequerySuggested();
            }
        }
        public string LibrarySize 
        { 
            get { return _librarySize; } 
            set
            {
                _librarySize = value;
                NotifyPropertyChanged("LibrarySize");
            } 
        }
        public string DeviceSize
        {
            get { return _deviceSize; }
            set
            {
                _deviceSize = value;
                NotifyPropertyChanged("DeviceSize");
            }
        }
        public string ExtensionSize
        {
            get { return _extensionSize; }
            set
            {
                _extensionSize = value;
                NotifyPropertyChanged("ExtensionSize");
            }
        }
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value; 
                NotifyPropertyChanged("Status");
            }
        }

        public int ProgressBarMaximum
        {
            get { return _progressBarMaximum; }
            set 
            {
                _progressBarMaximum = value;
                NotifyPropertyChanged("ProgressBarMaximum");
            }
        }

        public int ProcessedCount
        {
            get { return _processedCount; }
            set
            {
                _processedCount = value;
                NotifyPropertyChanged("ProcessedCount");
                NotifyPropertyChanged("ProcessedCountString");
            }
        }

        public int TotalToProcessCount
        {
            get { return _totalToProcessCount; }
            set
            {
                _totalToProcessCount = value;
                NotifyPropertyChanged("TotalToProcessCount");
                NotifyPropertyChanged("ProcessedCountString");
            }
        }

        public string ProcessedCountString
        {
            get { return ProcessedCount + "/" + TotalToProcessCount; }
        }

        public ICommand PerformSyncCommand
        {
            get { return _performSyncCommand; }
        }

        private long GetSizeOfFiles(string directory)
        {
            var files = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
                .Where(s => FileOperations.ValidFileTypes.Any(ext => s.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
            return (from file in files let fileInfo = new FileInfo(file) select fileInfo.Length).Sum();
        }

        public SolidColorBrush DeviceProgressBarColor
        {
            get { return _deviceProgressBarColor; }
            set 
            { 
                _deviceProgressBarColor = value;
                NotifyPropertyChanged("DeviceProgressBarColor");
            }
        }

        public SolidColorBrush ExtensionProgressBarColor
        {
            get { return _extensionProgressBarColor; }
            set
            {
                _extensionProgressBarColor = value;
                NotifyPropertyChanged("ExtensionProgressBarColor");
            }
        }

        public double DeviceProgressBarWidth
        {
            get { return _deviceProgressBarWidth; }
            set
            {
                _deviceProgressBarWidth = value;
                NotifyPropertyChanged("DeviceProgressBarWidth");
            }
        }

        public double ExtensionProgressBarWidth
        {
            get { return _extensionProgressBarWidth; }
            set
            {
                _extensionProgressBarWidth = value;
                NotifyPropertyChanged("ExtensionProgressBarWidth");
            }
        }

        public BitmapImage AlbumArt
        {
            get { return _albumArt; }
            set { 
                    _albumArt = value;
                    NotifyPropertyChanged("AlbumArt");
                }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}