using System.Collections.Generic;

namespace MusicSynq
{
    public static class FileOperations
    {
        public static readonly List<string> ValidFileTypes = new List<string> {"MP3", "WAV", "WMA", "M4A", "JPG"};

        public static string FormatBytes(long bytes)
        {
            decimal megaBytes = bytes / (1024 * 1024);

            string availableSpace = megaBytes.ToString("0") + "Mb";

            if (megaBytes > 1024)
            {
                megaBytes /= 1024;
                availableSpace = megaBytes.ToString("0.00") + "Gb";
            }

            return availableSpace;
        }
    }
}