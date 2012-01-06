using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading;

namespace MusicSynq
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static StreamWriter ErrorLog = new StreamWriter("error_log.txt");

        public string[] ValidFileTypes = { "MP3", "WAV", "WMA", "M4A", "JPG"};

        public static string musicDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

        //public static string apiKey = "f6294eda0ac4b1826056f4927ec4c763";

        List<string> deviceFiles;
        List<string> libraryFiles;

        public MainWindow()
        {
            InitializeComponent();
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            string volumeLabel;
            foreach (DriveInfo d in allDrives)
            {
                try
                {
                    volumeLabel = d.VolumeLabel;
                    if (volumeLabel == "COWON J3")
                    {
                        primaryDriveText.Text = d.Name;
                        deviceSpaceLabel.Content = formatBytes(d.AvailableFreeSpace);
                    }
                    if (volumeLabel == "COWON_EXT")
                    {
                        secondaryDriveText.Text = d.Name;
                        extensionSpaceLabel.Content = formatBytes(d.AvailableFreeSpace);
                    }
                }
                catch (IOException e) 
                {
                    ErrorLog.WriteLine(e);
                }
            }

            libraryPathText.Text = musicDirectory;
        }

        private void syncBtn_Click(object sender, RoutedEventArgs e)
        {
            string primaryDrive = primaryDriveText.Text;
            string secondaryDrive = secondaryDriveText.Text;

            librarySize.Content = formatBytes(getSizeOfFiles(musicDirectory));

            SynchronizationContext uiContext = SynchronizationContext.Current;

            //Do everything
            ThreadPool.QueueUserWorkItem((callBackObj) =>
            {
                deviceFiles = new List<string>();
                libraryFiles = new List<string>();

                //Scan Device, and create HashSet of files in 'Artist\Album\Song' order
                createFileSet(primaryDrive + "Music", deviceFiles, uiContext);
                createFileSet(secondaryDrive + "Music", deviceFiles, uiContext);
                updateStatusLabel(uiContext, "Added device files to memory.");

                //Scan library
                scanLibraryFiles(musicDirectory, libraryFiles, deviceFiles, uiContext);
                updateStatusLabel(uiContext, "Files missing from device: " + libraryFiles.Count);

                //Delete removed files
                updateStatusLabel(uiContext, "Removing files from device");
                deleteFiles(deviceFiles, primaryDrive, secondaryDrive);

                int filesToProcess = libraryFiles.Count;
                int filesProcessed = 0;

                setupProgressBar(uiContext, filesToProcess);

                libraryFiles.Shuffle();

                //Add libraryFiles (location strings) to albums Hashtable, with KVP = (location, Album)
                foreach (string s in libraryFiles)
                {
                    var d = new DriveInfo(primaryDrive);

                    try
                    {
                        var sample = TagLib.File.Create(musicDirectory + "\\" + s);

                        if (sample.Properties.Duration.TotalSeconds > 0 && sample.Tag.Pictures.Length > 0)
                            SetAlbumArtHolderImage(uiContext, sample.Tag.Pictures[0].Data.Data);
                    }
                    catch (Exception ex )
                    {
                        ErrorLog.WriteLine(ex);
                    }

                    int numRetries = 25;
                    while (numRetries-- > 0)
                    {
                        try
                        {
                            var f = new FileInfo(musicDirectory + "\\" + s);

                            if (d.AvailableFreeSpace - f.Length > 104857600)
                            {
                                updateAvailableSpace(uiContext, primaryDrive, d.AvailableFreeSpace - f.Length);

                                string destinationFilePath = primaryDrive + "Music\\" + s;
                                string directoryPath = destinationFilePath.Substring(0, destinationFilePath.LastIndexOf("\\"));

                                if (!Directory.Exists(directoryPath))
                                {
                                    Directory.CreateDirectory(directoryPath);
                                }

                                updateStatusLabel(uiContext, "Moving file " + f.Name);
                                File.Copy(f.FullName, destinationFilePath, true);    
                            }
                            else if ((d = new DriveInfo(secondaryDrive)).AvailableFreeSpace > f.Length)
                            {
                                updateAvailableSpace(uiContext, secondaryDrive, d.AvailableFreeSpace - f.Length);

                                string destinationFilePath = secondaryDrive + "Music\\" + s;
                                string directoryPath = destinationFilePath.Substring(0, destinationFilePath.LastIndexOf("\\"));

                                if (!Directory.Exists(directoryPath))
                                {
                                    Directory.CreateDirectory(directoryPath);
                                }
                                updateStatusLabel(uiContext, "Moving file " + f.Name);
                                File.Copy(f.FullName, destinationFilePath, true);
                            }
                            else
                            {
                                updateStatusLabel(uiContext, "Not enough space");
                                return;
                            }
                            break;
                        }
                        catch (Exception ex)
                        {
                            ErrorLog.WriteLine("!!!!!!!!!!!!!Failed to write " + s);
                            ErrorLog.WriteLine(ex);
                            Thread.Sleep(3000);
                            continue;
                        }
                    }

                    incrementProgressBar(uiContext, ++filesProcessed);
                }

                updateStatusLabel(uiContext, "Done!");
            });

            
            
        }

        private long getSizeOfFiles(string directory)
        {
            var files = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories).Where(s => 
                s.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".wma", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            );
            return (from file in files let fileInfo = new FileInfo(file) select fileInfo.Length).Sum();
        }

        private void createFileSet(string folderPath, List<string> fileSet, SynchronizationContext context)
        {
            string fileName;
            int indexOfExt;

            updateStatusLabel(context, "Getting directory information from " + folderPath);
            string[] allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

            for (int i = 0; i < allFiles.Length; ++i)
            {
                
                fileName = allFiles[i];
                indexOfExt = fileName.LastIndexOf('.');

                updateStatusLabel(context, "Adding file to fileSet: " + fileName);

                if (ValidFileTypes.Contains(fileName.Substring(indexOfExt + 1, fileName.Length - indexOfExt - 1).ToUpper()))
                {
                    indexOfExt = fileName.IndexOf("Music\\");
                    fileSet.Add(fileName.Substring(indexOfExt + 6, fileName.Length - indexOfExt - 6));
                }

            }
        }

        private void scanLibraryFiles(string libraryPath, List<string> fileSet, List<string> compareSet, SynchronizationContext context)
        {
            string fileName;
            int indexOfExt;
            List<string> allFiles = Directory.GetFiles(libraryPath).ToList<string>();

            updateStatusLabel(context, "Getting directory information from " + libraryPath);

            string[] allDirectories = Directory.GetDirectories(libraryPath);

            for (int i = 0; i < allDirectories.Length; ++i)
            {
                string dirName = allDirectories[i];
                string[] filesInDirectory = Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories);
                if (Directory.GetFiles(dirName, "*.*", SearchOption.AllDirectories).Length < 1)
                {
                    Directory.Delete(dirName, true);
                }
                else
                {
                    allFiles.AddRange(filesInDirectory);
                }
            }

            for (int i = 0; i < allFiles.Count;  ++i)
            {
                fileName = allFiles[i];
                indexOfExt = fileName.LastIndexOf('.');

                updateStatusLabel(context, "Adding file to fileSet: " + fileName);

                if (ValidFileTypes.Contains(fileName.Substring(indexOfExt + 1, fileName.Length - indexOfExt - 1).ToUpper()))
                {
                    indexOfExt = fileName.IndexOf("Music\\");
                    fileName = fileName.Substring(indexOfExt + 6, fileName.Length - indexOfExt - 6);
                    if (!compareSet.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                    {
                        fileSet.Add(fileName);
                    }
                    else
                    {
                        compareSet.Remove(fileName);
                    }

                }

            }
        }

        private void updateStatusLabel(SynchronizationContext ui, string msg)
        {
            ui.Post(delegate
            {
                statusLabel.Content = msg;
            }, null);
        }

        private void setupProgressBar(SynchronizationContext ui, int max)
        {
            ui.Post(delegate
            {
                progressBar1.Maximum = max;
                numProcessedLabel.Content = "0/" + max;
            }, null);
        }

        private void incrementProgressBar(SynchronizationContext ui, int progress)
        {
            ui.Post(delegate
            {
                progressBar1.Value = progress;
                numProcessedLabel.Content = progress + "/" + progressBar1.Maximum;
            }, null);
        }

        private string formatBytes(long bytes)
        {
            string availableSpace = "";
            decimal megaBytes = bytes / (1024 * 1024);

            availableSpace = megaBytes.ToString("0") + "Mb";

            if (megaBytes > 1024)
            {
                megaBytes /= 1024;
                availableSpace = megaBytes.ToString("0.00") + "Gb";
            }

            return availableSpace;
        }

        private void deleteFiles(List<string> filesToRemove, string devicePath, string extensionPath)
        {
            foreach (string fileName in filesToRemove)
            {
                if(File.Exists(devicePath + "Music\\" + fileName)) {
                    File.Delete(devicePath + "Music\\" + fileName);
                } else if(File.Exists(extensionPath + "Music\\" + fileName)) {
                    File.Delete(extensionPath + "Music\\" + fileName);
                } else {
                    ErrorLog.WriteLine(fileName + " was queued for deletion, but was not found on the device");
                }
            }
        }

        private void SetAlbumArtHolderImage(SynchronizationContext ui, byte[] imageBytes)
        {
            if (imageBytes.Length < 1)
                return;

            ui.Post(delegate
            {
                var bitmapImg = new BitmapImage();
                
                bitmapImg.BeginInit();

                var ms = new MemoryStream(imageBytes);
                bitmapImg.StreamSource = ms;

                bitmapImg.EndInit();

                albumArtHolder.Source = bitmapImg;
            }, null);
        }

        private void updateAvailableSpace(SynchronizationContext ui, string drive, long availableSpace)
        {
            ui.Post(delegate
            {
                DriveInfo d = new DriveInfo(drive);
                if (drive == primaryDriveText.Text)
                {
                    deviceSpaceLabel.Content = formatBytes(availableSpace);
                    canvas1.Width = 212 - 212*((double)availableSpace/d.TotalSize);
                    canvas1.Background = new SolidColorBrush(Color.FromRgb((byte)canvas1.Width, (byte)(212 - canvas1.Width), 0));
                    canvas1.Background.Opacity = 0.67;
                }
                else
                {
                    extensionSpaceLabel.Content = formatBytes(availableSpace);
                    canvas2.Width = 212 - 212 * ((double)availableSpace / d.TotalSize);
                    canvas2.Background = new SolidColorBrush(Color.FromRgb((byte)canvas2.Width, (byte)(212 - canvas2.Width), 0));
                    canvas2.Background.Opacity = 0.67;
                }

            }, null);
        }
    }
}