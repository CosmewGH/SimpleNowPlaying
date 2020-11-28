using System;
using System.Diagnostics;
using System.IO;
using System.Timers;

namespace SimpleNowPlaying
{
    class Program
    {
        private static string previousProcessTitle = "empty";
        private static Process appProcess;

        private static System.Timers.Timer mainTimer;

        private static bool messageCantFind = false;

        private static string configOutputPath = "now_playing.txt";
        private static string configProcessName = "vlc";
        private static string configSearchTitleText = " - VLC media player";
        private static string configPrefix = "Now Playing: ";
        private static string configSuffix = "     ";
        private static int configTrimBefore = 0;
        private static int configTrimAfter = 0;
        private static int configRefreshRate = 1000;

        static void Main(string[] args)
        {
            Console.WriteLine("\nSimpleNowPlaying v1.0");
            Console.WriteLine("https://github.com/Saiyan197/SimpleNowPlaying\n");
            LoadConfig();

            if (!string.IsNullOrEmpty(Path.GetDirectoryName(configOutputPath))) if (!Directory.Exists(Path.GetDirectoryName(configOutputPath))) Directory.CreateDirectory(Path.GetDirectoryName(configOutputPath));

            SetTimer();

            Console.WriteLine("\nPress the Enter key or close the window to exit the application...\n");
            Console.WriteLine("The application started at {0:HH:mm:ss.fff}\n", DateTime.Now);
            Console.ReadLine();
            mainTimer.Stop();
            mainTimer.Dispose();

            Console.WriteLine("Terminating the application...");
        }

        static void LoadConfig()
        {
            if (!File.Exists("Settings.txt"))
            {
                string defaultSettings =
                    "#Settings for SimpleNowPlaying\n" +
                    "\n" +
                    "#OutputPath: Directory and filename to output the text file to. Can be relative.\n" +
                    "#Example of absolute directory: " + '"' + "C:/SimpleNowPlaying/now_playing.txt" + '"' + "\n" +
                    "#Example of relative directory: " + '"' + "../Texts/now_playing.txt" + '"' + "\n" +
                    "OutputPath = " + '"' + configOutputPath + '"' + "\n" +
                    "\n" +
                    "#ProcessName: Process name to search for.\n" +
                    "ProcessName = " + '"' + configProcessName + '"' + "\n" +
                    "\n" +
                    "#SearchTitleText: Text the application title must contain to be considered as playing a song.\n" +
                    "#(This text is also removed from the output of the title, before additional trimming.)\n" +
                    "SearchTitleText = " + '"' + configSearchTitleText + '"' + "\n" +
                    "\n" +
                    "#TrimBefore: Characters to trim off the beginning of the title in the output. Should be 0 or greater.\n" +
                    "TrimBefore = " + configTrimBefore + "\n" +
                    "\n" +
                    "#TrimAfter: Characters to trim off the end of the title in the output. Should be 0 or greater.\n" +
                    "TrimAfter = " + configTrimAfter + "\n" +
                    "\n" +
                    "#Prefix: Text to add to the beginning of the output.\n" +
                    "Prefix = " + '"' + configPrefix + '"' + "\n" +
                    "\n" +
                    "#Suffix: Text to add to the end of the output.\n" +
                    "Suffix = " + '"' + configSuffix + '"' + "\n" +
                    "\n" +
                    "#RefreshRate: Milliseconds per refresh of the application and output text.\n" +
                    "#Low refresh rates may influence performance. 1000 recommended (1 second)\n" +
                    "RefreshRate = " + configRefreshRate + "\n";
                File.WriteAllText("Settings.txt", defaultSettings);
                Console.WriteLine("Settings.txt created.");
                return;
            }
            var file = File.ReadLines("Settings.txt");
            foreach (string line in file)
            {
                line.Replace("\r", "");
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("#") || line.StartsWith("\n") || !line.Contains("=")) continue;
                int equalsIndex = line.IndexOf("=");
                string property = line.Substring(0, equalsIndex).Replace(" ","");
                string lineafter = line.Substring(equalsIndex + 1, line.Length - equalsIndex - 1);
                string value = null;
                if (lineafter.Contains('"'))
                {
                    int firstIndex = lineafter.IndexOf('"');
                    int secondIndex = lineafter.IndexOf('"', firstIndex + 1);
                    if (secondIndex > 0) value = lineafter.Substring(firstIndex + 1, secondIndex - firstIndex - 1);
                }
                if (value == null) value = lineafter.Replace("\n", "").Replace(" ", "");
                switch (property)
                {
                    case "OutputPath":
                        configOutputPath = value;
                        Console.WriteLine("Config Loaded: OutputPath: " + '"' + configOutputPath + '"');
                        break;
                    case "ProcessName":
                        configProcessName = value;
                        Console.WriteLine("Config Loaded: ProcessName: " + '"' + configProcessName + '"');
                        break;
                    case "SearchTitleText":
                        configSearchTitleText = value;
                        Console.WriteLine("Config Loaded: SearchTitleText: " + '"' + configSearchTitleText + '"');
                        break;
                    case "TrimBefore":
                        if (int.TryParse(value, out configTrimBefore))
                            Console.WriteLine("Config Loaded: TrimBefore: " + '"' + configTrimBefore + '"');
                        break;
                    case "TrimAfter":
                        if (int.TryParse(value, out configTrimAfter))
                            Console.WriteLine("Config Loaded: TrimAfter: " + '"' + configTrimAfter + '"');
                        break;
                    case "Prefix":
                        configPrefix = value;
                        Console.WriteLine("Config Loaded: Prefix: " + '"' + configPrefix + '"');
                        break;
                    case "Suffix":
                        configSuffix = value;
                        Console.WriteLine("Config Loaded: Suffix: " + '"' + configSuffix + '"');
                        break;
                    case "RefreshRate":
                        if (int.TryParse(value, out configRefreshRate))
                            Console.WriteLine("Config Loaded: RefreshRate: " + '"' + configRefreshRate + '"');
                        break;
                }
            }
        }

        private static void SetTimer()
        {
            mainTimer = new System.Timers.Timer(configRefreshRate);
            mainTimer.Elapsed += OnTimedEvent;
            mainTimer.AutoReset = true;
            mainTimer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if (appProcess != null)
            {
                try { appProcess = Process.GetProcessById(appProcess.Id); }
                catch
                {
                    appProcess = null;
                    Console.WriteLine("Media player process exited.");
                }
                if (appProcess != null) if (appProcess.HasExited)
                    {
                        appProcess = null;
                        Console.WriteLine("Media player process exited.");
                    }
            }
            if (appProcess == null) if (!FindVLCProcess())
                {
                    if (!messageCantFind)
                    {
                        Console.WriteLine("Can't find media player process. Retrying...");
                        messageCantFind = true;
                        SaveEmptyFileIfNotEmpty();
                    }
                    return;
                }
            if (!previousProcessTitle.Equals(appProcess.MainWindowTitle))
            {
                string songInfo = appProcess.MainWindowTitle.Replace(configSearchTitleText, "");
                if (songInfo.Equals(appProcess.MainWindowTitle)) return;

                previousProcessTitle = appProcess.MainWindowTitle;

                SaveFile(configPrefix + songInfo.Substring(configTrimBefore, songInfo.Length - configTrimBefore - configTrimAfter) + configSuffix);
                string log = string.Format("{0:dd:MM:yyyy - HH:mm:ss} Song changed: " + songInfo, DateTime.Now);
                Console.WriteLine(log);
                UpdateLog(log);
            }
        }

        private static void SaveFile(string text)
        {
            using (StreamWriter outputFile = new StreamWriter(configOutputPath))
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
                    if (p.ProcessName.Equals(configProcessName))
                    {
                        appProcess = p;
                        messageCantFind = false;
                        return true;
                    }
                }
            }
            appProcess = null;
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
