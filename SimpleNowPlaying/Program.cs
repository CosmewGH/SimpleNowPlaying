using System;
using System.Diagnostics;
using System.IO;
using System.Timers;

namespace SimpleNowPlaying
{
    class Program
    {
        private static string previousProcessTitle = "empty";
        private static Process vlcProcess;

        private static System.Timers.Timer mainTimer;

        private static bool messageCantFind = false;

        static void Main(string[] args)
        {
            SetTimer();

            Console.WriteLine("\nPress the Enter key to exit the application...\n");
            Console.WriteLine("The application started at {0:HH:mm:ss.fff}", DateTime.Now);
            Console.ReadLine();
            mainTimer.Stop();
            mainTimer.Dispose();

            Console.WriteLine("Terminating the application...");
        }

        private static void SetTimer()
        {
            mainTimer = new System.Timers.Timer(1000);
            mainTimer.Elapsed += OnTimedEvent;
            mainTimer.AutoReset = true;
            mainTimer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if (vlcProcess != null)
            {
                try { vlcProcess = Process.GetProcessById(vlcProcess.Id); }
                catch
                {
                    vlcProcess = null;
                    Console.WriteLine("VLC process exited.");
                }
                if (vlcProcess != null) if (vlcProcess.HasExited)
                    {
                        vlcProcess = null;
                        Console.WriteLine("VLC process exited.");
                    }
            }
            if (vlcProcess == null) if (!FindVLCProcess())
                {
                    if (!messageCantFind)
                    {
                        Console.WriteLine("Can't find VLC process. Retrying...");
                        messageCantFind = true;
                        SaveEmptyFileIfNotEmpty();
                    }
                    return;
                }
            if (!previousProcessTitle.Equals(vlcProcess.MainWindowTitle))
            {
                string songInfo = vlcProcess.MainWindowTitle.Replace(" - VLC media player", "");
                if (songInfo.Equals(vlcProcess.MainWindowTitle)) return;

                previousProcessTitle = vlcProcess.MainWindowTitle;

                SaveFile("Now Playing: " + songInfo + "     ");
                string log = string.Format("{0:dd:MM:yyyy - HH:mm:ss} Song changed: " + songInfo, DateTime.Now);
                Console.WriteLine(log);
                UpdateLog(log);
            }
        }

        private static void SaveFile(string text)
        {
            using (StreamWriter outputFile = new StreamWriter("now_playing.txt"))
            {
                outputFile.Write(text);
            }
        }

        private static void SaveEmptyFileIfNotEmpty()
        {
            if (!string.IsNullOrEmpty(previousProcessTitle))
            {
                previousProcessTitle = "";
                SaveFile("");
            }
        }

        private static bool FindVLCProcess()
        {
            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
            {
                if (!String.IsNullOrEmpty(p.ProcessName))
                {
                    if (p.ProcessName.Equals("vlc"))
                    {
                        vlcProcess = p;
                        messageCantFind = false;
                        return true;
                    }
                }
            }
            vlcProcess = null;
            return false;
        }

        private static void UpdateLog(string text)
        {
            using (StreamWriter outputFile = new StreamWriter("now_playing_log.txt", true))
            {
                outputFile.WriteLine(text);
            }
        }
    }
}
