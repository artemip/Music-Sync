using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace MusicSynq
{
    public class MusicSynqViewModel : INotifyPropertyChanged
    {
        private readonly string[] ValidFileTypes = { "MP3", "WAV", "WMA", "M4A", "JPG"};
        public static readonly StreamWriter ErrorLog = new StreamWriter("error_log.txt");
        private string _libraryPath;
        private string _devicePath;
        private string _extensionPath;
        private string _librarySize;
        private string _deviceSize;
        private string _extensionSize;
        private ICommand _performSyncCommand;

        public MusicSynqViewModel()
        {
            var setting = Properties.Settings.Default.LibraryPath;

            LibraryPath = (!string.IsNullOrEmpty(setting)) ? setting : Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

            GetDrivePaths();

            _performSyncCommand = new DelegateCommand(AllDrivesValid, p => PerformSync(SynchronizationContext.Current));
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
                    continue;
                }
            }
        }

        private string GetVolumeName(string drivePath)
        {
            return (new DriveInfo(drivePath)).Name;
        }

        private void PerformSync(SynchronizationContext uiContext)
        {
            
        }

        public string LibraryPath
        {
            get { return _libraryPath; }
            set
            {
                if (!Directory.Exists(value))
                {
                    ErrorLog.WriteLine("Library directory does not exist.");
                    return;
                }
                if (_libraryPath == value) return;

                _libraryPath = value;
                Properties.Settings.Default.LibraryPath = value;

                LibrarySize = FormatBytes(GetSizeOfFiles(value));

                NotifyPropertyChanged("LibraryPath");
            }
        }

        public string DevicePath
        {
            get { return _devicePath; }
            set
            {
                if(!Directory.Exists(value))
                {
                    ErrorLog.WriteLine("Device directory does not exist.");
                    return;
                }
                if (_devicePath == value) return;

                _devicePath = value;
                Properties.Settings.Default.DeviceName = GetVolumeName(value.Substring(0, 1));

                DeviceSize = FormatBytes((new DriveInfo(value)).AvailableFreeSpace);

                NotifyPropertyChanged("DevicePath");
            }
        }

        public string ExtensionPath
        {
            get { return _extensionPath; }
            set
            {
                if(!Directory.Exists(value))
                {
                    ErrorLog.WriteLine("Extension directory does not exist.");
                    return;
                }
                if (_extensionPath == value) return;

                _extensionPath = value;
                Properties.Settings.Default.ExtensionName = GetVolumeName(value.Substring(0, 1));
                NotifyPropertyChanged("ExtensionPath");
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

        public ICommand PerformSyncCommand
        {
            get { return _performSyncCommand; }
        }

        private long GetSizeOfFiles(string directory)
        {
            var files = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories).Where(s => ValidFileTypes.Any(s.EndsWith));
            return (from file in files let fileInfo = new FileInfo(file) select fileInfo.Length).Sum();
        }


        //private void syncBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    string primaryDrive = primaryDriveText.Text;
        //    string secondaryDrive = secondaryDriveText.Text;

        //    librarySize.Content = formatBytes(getSizeOfFiles(musicDirectory));

        //    SynchronizationContext uiContext = SynchronizationContext.Current;

        //    //Do everything
        //    ThreadPool.QueueUserWorkItem((callBackObj) =>
        //    {
        //        deviceFiles = new List<string>();
        //        libraryFiles = new List<string>();

        //        //Scan Device, and create HashSet of files in 'Artist\Album\Song' order
        //        createFileSet(primaryDrive + "Music", deviceFiles, uiContext);
        //        createFileSet(secondaryDrive + "Music", deviceFiles, uiContext);
        //        updateStatusLabel(uiContext, "Added device files to memory.");

        //        //Scan library
        //        scanLibraryFiles(musicDirectory, libraryFiles, deviceFiles, uiContext);
        //        updateStatusLabel(uiContext, "Files missing from device: " + libraryFiles.Count);

        //        //Delete removed files
        //        updateStatusLabel(uiContext, "Removing files from device");
        //        deleteFiles(deviceFiles, primaryDrive, secondaryDrive);

        //        int filesToProcess = libraryFiles.Count;
        //        int filesProcessed = 0;

        //        setupProgressBar(uiContext, filesToProcess);

        //        libraryFiles.Shuffle();

        //        //Add libraryFiles (location strings) to albums Hashtable, with KVP = (location, Album)
        //        foreach (string s in libraryFiles)
        //        {
        //            var d = new DriveInfo(primaryDrive);

        //            try
        //            {
        //                var sample = TagLib.File.Create(musicDirectory + "\\" + s);

        //                if (sample.Properties.Duration.TotalSeconds > 0 && sample.Tag.Pictures.Length > 0)
        //                    SetAlbumArtHolderImage(uiContext, sample.Tag.Pictures[0].Data.Data);
        //            }
        //            catch (Exception ex )
        //            {
        //                ErrorLog.WriteLine(ex);
        //            }

        //            int numRetries = 25;
        //            while (numRetries-- > 0)
        //            {
        //                try
        //                {
        //                    var f = new FileInfo(musicDirectory + "\\" + s);

        //                    if (d.AvailableFreeSpace - f.Length > 104857600)
        //                    {
        //                        updateAvailableSpace(uiContext, primaryDrive, d.AvailableFreeSpace - f.Length);

        //                        string destinationFilePath = primaryDrive + "Music\\" + s;
        //                        string directoryPath = destinationFilePath.Substring(0, destinationFilePath.LastIndexOf("\\"));

        //                        if (!Directory.Exists(directoryPath))
        //                        {
        //                            Directory.CreateDirectory(directoryPath);
        //                        }

        //                        updateStatusLabel(uiContext, "Moving file " + f.Name);
        //                        File.Copy(f.FullName, destinationFilePath, true);    
        //                    }
        //                    else if ((d = new DriveInfo(secondaryDrive)).AvailableFreeSpace > f.Length)
        //                    {
        //                        updateAvailableSpace(uiContext, secondaryDrive, d.AvailableFreeSpace - f.Length);

        //                        string destinationFilePath = secondaryDrive + "Music\\" + s;
        //                        string directoryPath = destinationFilePath.Substring(0, destinationFilePath.LastIndexOf("\\"));

        //                        if (!Directory.Exists(directoryPath))
        //                        {
        //                            Directory.CreateDirectory(directoryPath);
        //                        }
        //                        updateStatusLabel(uiContext, "Moving file " + f.Name);
        //                        File.Copy(f.FullName, destinationFilePath, true);
        //                    }
        //                    else
        //                    {
        //                        updateStatusLabel(uiContext, "Not enough space");
        //                        return;
        //                    }
        //                    break;
        //                }
        //                catch (Exception ex)
        //                {
        //                    ErrorLog.WriteLine("!!!!!!!!!!!!!Failed to write " + s);
        //                    ErrorLog.WriteLine(ex);
        //                    Thread.Sleep(3000);
        //                    continue;
        //                }
        //            }

        //            incrementProgressBar(uiContext, ++filesProcessed);
        //        }

        //        updateStatusLabel(uiContext, "Done!");
        //    });

            
            
        //}

        //private long getSizeOfFiles(string directory)
        //{
        //    var files = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories).Where(s => 
        //        s.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
        //        s.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
        //        s.EndsWith(".wma", StringComparison.OrdinalIgnoreCase) ||
        //        s.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
        //        s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
        //    );
        //    return (from file in files let fileInfo = new FileInfo(file) select fileInfo.Length).Sum();
        //}

        //private void createFileSet(string folderPath, List<string> fileSet, SynchronizationContext context)
        //{
        //    string fileName;
        //    int indexOfExt;

        //    updateStatusLabel(context, "Getting directory information from " + folderPath);
        //    string[] allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

        //    for (int i = 0; i < allFiles.Length; ++i)
        //    {
                
        //        fileName = allFiles[i];
        //        indexOfExt = fileName.LastIndexOf('.');

        //        updateStatusLabel(context, "Adding file to fileSet: " + fileName);

        //        if (ValidFileTypes.Contains(fileName.Substring(indexOfExt + 1, fileName.Length - indexOfExt - 1).ToUpper()))
        //        {
        //            indexOfExt = fileName.IndexOf("Music\\");
        //            fileSet.Add(fileName.Substring(indexOfExt + 6, fileName.Length - indexOfExt - 6));
        //        }

        //    }
        //}

        //private void scanLibraryFiles(string libraryPath, List<string> fileSet, List<string> compareSet, SynchronizationContext context)
        //{
        //    string fileName;
        //    int indexOfExt;
        //    List<string> allFiles = Directory.GetFiles(libraryPath).ToList<string>();

        //    updateStatusLabel(context, "Getting directory information from " + libraryPath);

        //    string[] allDirectories = Directory.GetDirectories(libraryPath);

        //    for (int i = 0; i < allDirectories.Length; ++i)
        //    {
        //        string dirName = allDirectories[i];
        //        string[] filesInDirectory = Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories);
        //        if (Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories).Length < 1)
        //        {
        //            Directory.Delete(dirName, true);
        //        }
        //        else
        //        {
        //            allFiles.AddRange(filesInDirectory);
        //        }
        //    }

        //    for (int i = 0; i < allFiles.Count;  ++i)
        //    {
        //        fileName = allFiles[i];
        //        indexOfExt = fileName.LastIndexOf('.');

        //        updateStatusLabel(context, "Adding file to fileSet: " + fileName);

        //        if (ValidFileTypes.Contains(fileName.Substring(indexOfExt + 1, fileName.Length - indexOfExt - 1).ToUpper()))
        //        {
        //            indexOfExt = fileName.IndexOf("Music\\");
        //            fileName = fileName.Substring(indexOfExt + 6, fileName.Length - indexOfExt - 6);
        //            if (!compareSet.Contains(fileName, StringComparer.OrdinalIgnoreCase))
        //            {
        //                fileSet.Add(fileName);
        //            }
        //            else
        //            {
        //                compareSet.Remove(fileName);
        //            }

        //        }

        //    }
        //}

        //private void updateStatusLabel(SynchronizationContext ui, string msg)
        //{
        //    ui.Post(delegate
        //    {
        //        statusLabel.Content = msg;
        //    }, null);
        //}

        //private void setupProgressBar(SynchronizationContext ui, int max)
        //{
        //    ui.Post(delegate
        //    {
        //        progressBar1.Maximum = max;
        //        numProcessedLabel.Content = "0/" + max;
        //    }, null);
        //}

        //private void incrementProgressBar(SynchronizationContext ui, int progress)
        //{
        //    ui.Post(delegate
        //    {
        //        progressBar1.Value = progress;
        //        numProcessedLabel.Content = progress + "/" + progressBar1.Maximum;
        //    }, null);
        //}

        

        //private void deleteFiles(List<string> filesToRemove, string devicePath, string extensionPath)
        //{
        //    foreach (string fileName in filesToRemove)
        //    {
        //        if(File.Exists(devicePath + "Music\\" + fileName)) {
        //            File.Delete(devicePath + "Music\\" + fileName);
        //        } else if(File.Exists(extensionPath + "Music\\" + fileName)) {
        //            File.Delete(extensionPath + "Music\\" + fileName);
        //        } else {
        //            ErrorLog.WriteLine(fileName + " was queued for deletion, but was not found on the device");
        //        }
        //    }
        //}

        //private void SetAlbumArtHolderImage(SynchronizationContext ui, byte[] imageBytes)
        //{
        //    if (imageBytes.Length < 1)
        //        return;

        //    ui.Post(delegate
        //    {
        //        var bitmapImg = new BitmapImage();
                
        //        bitmapImg.BeginInit();

        //        var ms = new MemoryStream(imageBytes);
        //        bitmapImg.StreamSource = ms;

        //        bitmapImg.EndInit();

        //        albumArtHolder.Source = bitmapImg;
        //    }, null);
        //}

        //private void updateAvailableSpace(SynchronizationContext ui, string drive, long availableSpace)
        //{
        //    ui.Post(delegate
        //    {
        //        DriveInfo d = new DriveInfo(drive);
        //        if (drive == primaryDriveText.Text)
        //        {
        //            deviceSpaceLabel.Content = formatBytes(availableSpace);
        //            canvas1.Width = 212 - 212*((double)availableSpace/d.TotalSize);
        //            canvas1.Background = new SolidColorBrush(Color.FromRgb((byte)canvas1.Width, (byte)(212 - canvas1.Width), 0));
        //            canvas1.Background.Opacity = 0.67;
        //        }
        //        else
        //        {
        //            extensionSpaceLabel.Content = formatBytes(availableSpace);
        //            canvas2.Width = 212 - 212 * ((double)availableSpace / d.TotalSize);
        //            canvas2.Background = new SolidColorBrush(Color.FromRgb((byte)canvas2.Width, (byte)(212 - canvas2.Width), 0));
        //            canvas2.Background.Opacity = 0.67;
        //        }

        //    }, null);
        //}
        //}

        private string FormatBytes(long bytes)
        {
            string availableSpace;
            decimal megaBytes = bytes / (1024 * 1024);

            availableSpace = megaBytes.ToString("0") + "Mb";

            if (megaBytes > 1024)
            {
                megaBytes /= 1024;
                availableSpace = megaBytes.ToString("0.00") + "Gb";
            }

            return availableSpace;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}