using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace GTA_V_Lobby_Leaver
{
    public partial class MainWindow : Window
    {
        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);

        private readonly Keys keys = new Keys();
        private readonly string executableName = "GTA5";
        private bool isGameRunning;
        private bool isLeavingLobby;
        private int processId;
        private Config config;
        private Timer tmrStatus;
        private readonly Stopwatch stpLeave = new Stopwatch();

        public MainWindow()
        {
            InitializeComponent();
            if (!Directory.Exists(Config.Path.Directory)) { Directory.CreateDirectory(Config.Path.Directory); }
            LoadWindow();
            UpdateWindow(false, null);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tmrStatus = new Timer(20);
            tmrStatus.Elapsed += TmrStatus_Elapsed;
            tmrStatus.Start();

            keys.KeyDown += new Keys.KeyboardHookCallback(KeyboardHook_KeyDown);
            keys.Install();
        }

        private void TmrStatus_Elapsed(object sender, ElapsedEventArgs e)
        {
            Process[] processes = Process.GetProcessesByName(executableName);
            foreach (Process process in processes)
            {
                if (!process.HasExited) { processId = process.Id; UpdateWindow(true, process); return; }
            }
            processId = 0;
            UpdateWindow(false, null);
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            tmrStatus.Stop();
            tmrStatus.Dispose();
            if (mnuSaveWindowPosition.IsChecked)
            {
                config.WindowPositionLeft = Application.Current.MainWindow.Left;
                config.WindowPositionTop = Application.Current.MainWindow.Top;
            }
            config.ShowMilliSeconds = mnuMilliseconds.IsChecked;
            config.SaveWindowPosition = mnuSaveWindowPosition.IsChecked;
            config.WindowAlwaysOnTop = mnuTopMost.IsChecked;
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            using (StreamWriter streamWriter = File.CreateText(Config.Path.ConfigFile))
            {
                streamWriter.WriteLine(json);
                streamWriter.Close();
            }
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
        }

        private void MnuLanguage_Click(object sender, RoutedEventArgs e)
        {
            if (config.Language == 0) { config.Language = 1; }
            else if (config.Language == 1) { config.Language = 0; }
        }
        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!isGameRunning)
            {
                const string regKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432NODE\Valve\Steam";
                if (Registry.GetValue(regKey, "InstallPath", null) is string path)
                {
                    path += @"\";
                    using (Process process = new Process())
                    {
                        process.StartInfo.WorkingDirectory = path;
                        process.StartInfo.FileName = "Steam.exe";
                        process.StartInfo.Arguments = "-applaunch 271590";
                        process.Start();
                    }
                }
                else
                {
                    if (config.Language == 0) { MessageBox.ShowDialog(this, "Steam cannot be found.", "Error", "OK", config); }
                    else if (config.Language == 1) { MessageBox.ShowDialog(this, "Steam kann nicht gefunden werden.", "Fehler", "OK", config); }

                }
            }
            else
            {
                Process process = Process.GetProcessById(processId);
                if (process != null && !process.HasExited)
                {
                    if ((config.Language == 0 && MessageBox.ShowDialog(this, "Are you sure to stop the running process?", "Warning", "YesNo", config))
                        || (config.Language == 1 && MessageBox.ShowDialog(this, "Bist du sicher, dass du den Prozess beenden willst?", "Warnung", "YesNo", config)))
                    {
                        try { process.Kill(); }
                        catch
                        {
                            if (config.Language == 0) { MessageBox.ShowDialog(this, "Process cannot be stopped.", "Error", "OK", config); }
                            else if (config.Language == 1) { MessageBox.ShowDialog(this, "Prozess kann nicht beendet werden.", "Fehler", "OK", config); }
                        }
                    }
                }
            }
        }
        private void BtnLeave_Click(object sender, RoutedEventArgs e)
        {
            isLeavingLobby = true;
            stpLeave.Start();
            SuspendProcess(processId);
        }

        private void LoadWindow()
        {
            try
            {
                string json = File.ReadAllText(Config.Path.ConfigFile);
                config = JsonConvert.DeserializeObject<Config>(json);
            }
            catch { config = new Config(); }
            if (config.WindowPositionLeft != -1 && config.WindowPositionTop != -1)
            {
                Application.Current.MainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                Application.Current.MainWindow.Left = config.WindowPositionLeft;
                Application.Current.MainWindow.Top = config.WindowPositionTop;
            }
            else { this.WindowStartupLocation = WindowStartupLocation.CenterScreen; }
            mnuMilliseconds.IsChecked = config.ShowMilliSeconds;
            mnuTopMost.IsChecked = config.WindowAlwaysOnTop;
            mnuSaveWindowPosition.IsChecked = config.SaveWindowPosition;
        }
        private void UpdateWindow(bool gameStarted, Process process)
        {
            if (isLeavingLobby)
            {
                if (stpLeave.Elapsed.Seconds == 8 || processId == 0)
                {
                    isLeavingLobby = false;
                    ResumeProcess(processId);
                    stpLeave.Stop();
                    stpLeave.Reset();
                    this.Dispatcher.Invoke(() => { barLeave.Value = 0.0; });
                    return;
                }
                this.Dispatcher.Invoke(() =>
                {
                    if (config.Language == 0) { btnLeave.Content = $"Please wait ({stpLeave.Elapsed:ss\\.ff}s)"; }
                    else if (config.Language == 1) { btnLeave.Content = $"Bitte warten ({stpLeave.Elapsed:ss\\.ff}s)"; }
                    btnLeave.IsEnabled = false;
                    barLeave.Maximum = 800;
                    barLeave.Value = stpLeave.Elapsed.TotalSeconds * 100;
                });
            }
            if (gameStarted)
            {
                if (!isLeavingLobby)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        brdStatus.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#40c257");
                        txtStatus.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#1a1a1a");
                        txtPID.Text = $"PID: {processId}";
                        btnLeave.IsEnabled = true;
                    });
                    if (config.Language == 0)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            txtStatus.Text = "Status: GTA V is running";
                            btnLeave.Content = "Leave Lobby (F6)";
                            txtProcess.Text = "Process";
                        });
                    }
                    else if (config.Language == 1)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            txtStatus.Text = "Status: GTA V ist gestartet";
                            btnLeave.Content = "Lobby verlassen (F6)";
                            txtProcess.Text = "Prozess";
                        });
                    }
                }
                SetRuntime(process);
                isGameRunning = true;
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    brdStatus.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#9e4242");
                    txtStatus.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#dadada");
                    txtPID.Text = $"PID: 0";
                    btnLeave.IsEnabled = false;
                });
                if (config.Language == 0)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        if (mnuMilliseconds.IsChecked) { txtRuntime.Text = "Runtime: 00:00:00.00"; }
                        else { txtRuntime.Text = "Runtime: 00:00:00"; }
                        txtStatus.Text = "Status: GTA V is not running";
                        btnLeave.Content = "Leave Lobby (F6)";
                        txtProcess.Text = "Process";
                    });
                }
                else if (config.Language == 1)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        if (mnuMilliseconds.IsChecked) { txtRuntime.Text = "Laufzeit: 00:00:00.00"; }
                        else { txtRuntime.Text = "Laufzeit: 00:00:00"; }
                        txtStatus.Text = "Status: GTA V ist nicht gestartet";
                        btnLeave.Content = "Lobby verlassen (F6)";
                        txtProcess.Text = "Prozess";
                    });
                }
                isGameRunning = false;
            }
            if (config.Language == 0) { this.Dispatcher.Invoke(() => 
            { 
                mnuLanguage.Header = "Switch Language to German";
                mnuMilliseconds.Header = "Show Milliseconds";
                mnuTopMost.Header = "Window always on top";
                mnuSaveWindowPosition.Header = "Save Position";
                if (isGameRunning) { btnStart.Content = "Stop GTA V (Steam)"; }
                else { btnStart.Content = "Start GTA V (Steam)"; }
            });}
            else if (config.Language == 1) { this.Dispatcher.Invoke(() => 
            { 
                mnuLanguage.Header = "Wechsle Sprache zu Englisch";
                mnuMilliseconds.Header = "Zeige Millisekunden";
                mnuTopMost.Header = "Fenster immer im Vordergrund";
                mnuSaveWindowPosition.Header = "Speichere Position";
                if (isGameRunning) { btnStart.Content = "GTA V stoppen (Steam)"; }
                else { btnStart.Content = "GTA V starten (Steam)"; }
            }); }
            this.Dispatcher.Invoke(() => { this.Topmost = mnuTopMost.IsChecked; });
        }
        private void SetRuntime(Process process)
        {
            TimeSpan timespan = DateTime.Now - process.StartTime;
            this.Dispatcher.Invoke(() =>
            {
                if (config.Language == 0) { txtRuntime.Text = "Runtime: "; }
                else if (config.Language == 1) { txtRuntime.Text = "Laufzeit: "; }
                if (mnuMilliseconds.IsChecked) { txtRuntime.Text += $"{timespan:hh\\:mm\\:ss\\.ff}"; }
                else { txtRuntime.Text += $"{timespan:hh\\:mm\\:ss}"; }
            });
        }

        private static void SuspendProcess(int processId)
        {
            var process = Process.GetProcessById(processId);
            if (process.ProcessName == string.Empty) { return; }

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
                if (pOpenThread == IntPtr.Zero) { continue; }
                SuspendThread(pOpenThread);
                CloseHandle(pOpenThread);
            }
        }
        private static void ResumeProcess(int processId)
        {
            var process = Process.GetProcessById(processId);
            if (process.ProcessName == string.Empty) { return; }

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
                if (pOpenThread == IntPtr.Zero) { continue; }
                int suspendCount;
                do { suspendCount = ResumeThread(pOpenThread); }
                while (suspendCount > 0);
                CloseHandle(pOpenThread);
            }
        }

        private void KeyboardHook_KeyDown(Keys.VKeys key)
        {
            if (key == Keys.VKeys.KEY_F6) 
            { 
                if (!isLeavingLobby) { isLeavingLobby = true; SuspendProcess(processId); } 
            }
        }
    }
}