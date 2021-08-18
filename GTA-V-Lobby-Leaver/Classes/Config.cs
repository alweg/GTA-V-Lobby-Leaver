using System;

namespace GTA_V_Lobby_Leaver
{
    public class Config
    {
        public struct Path
        {
            public static string Directory = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\GTAVLL";
            public static string ConfigFile = $"{Directory}\\settings.cfg";
        }

        public int Language { get; set; }
        public bool WindowAlwaysOnTop { get; set; }
        public bool ShowMilliSeconds { get; set; }
        public bool SaveWindowPosition { get; set; }
        public double WindowPositionLeft { get; set; }
        public double WindowPositionTop { get; set; }

        public Config()
        {
            Language = 0;
            WindowAlwaysOnTop = false;
            ShowMilliSeconds = true;
            SaveWindowPosition = true;
            WindowPositionLeft = -1;
            WindowPositionTop = -1;
        }
    }
}