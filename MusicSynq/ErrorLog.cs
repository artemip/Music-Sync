using System;
using System.IO;

namespace MusicSynq
{
    public static class ErrorLog
    {
        private static readonly StreamWriter Log = new StreamWriter("error_log.txt");

        public static void Write(Exception ex)
        {
            Write(ex.Message);
        }

        public static void Write(string message)
        {
            Log.WriteLine(message);
        }
    }
}