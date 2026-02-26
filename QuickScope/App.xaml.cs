using System;
using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using QuickScope.Services;
using System.Windows.Media.Imaging;

namespace QuickScope;

/// <summary>
/// Interaction logic for App.xaml
/// App will be triggered by a keystroke
/// </summary>
public partial class App : Application
{
    // keep a reference to the selection window 
    //private SelectionWindow _selectionWindow;
    
    // P/Invoke Signatures to Native Windows API
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    
    // Windows Constants
    private const int WM_HOTKEY = 0x0312;
    
    // Modifers
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    
    // Virtual Key Code for 'S'
    private const uint VK_S = 0x53;

    // id for main capture
    private const int HOTKEY_ID_CAPTURE = 1;
    
    // track current windows so we can hide it
    private SelectionWindow? _currentWindow;
    
    // systray tracking
    private MainWindow? _settingsWindow;
    private System.Windows.Forms.NotifyIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Load User Settings
        Models.SettingsManager.Load();
        
        AudioService.Initialize(Models.SettingsManager.Current.SelectedSound);
        
        //var mainWindow = new MainWindow();
        //mainWindow.Show();
        
        // setup systray icon
        InitializeTrayIcon();
        
        // show settings (MainWindows) on Launch
        ShowSettingsWindow();
        
        // Register hotkeys
        // Passing IntPtr.Zero attaches the hotkey to the current thread rather than a specific window.
        bool registered = RegisterHotKey(IntPtr.Zero, HOTKEY_ID_CAPTURE, MOD_CONTROL | MOD_SHIFT, VK_S);

        if (!registered)
        {
            MessageBox.Show("Failed to register hotkeys. Another program might be using them", "QuickScope Err");
            Current.Shutdown();
            return;
        }
        
        //  Hook into the WPF component dispatcher to listen to raw Windows messages
        ComponentDispatcher.ThreadPreprocessMessage += OnThreadPreprocessMessage;
        
        Task.Run(() =>
        {
            try
            {
                CaptureService.CaptureInstant();
                Console.WriteLine("Background warmup");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });
    }
    
        private void InitializeTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "QuickScope - Ready to Capture",
            Visible = true
        };

        // Load the icon we embedded in the project
        try
        {
            var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/QS.ico"))?.Stream;
            if (iconStream != null)
            {
                _trayIcon.Icon = new System.Drawing.Icon(iconStream);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load tray icon: {ex.Message}");
        }

        // Handle double-clicking the tray icon to open settings
        _trayIcon.DoubleClick += (s, args) => ShowSettingsWindow();

        // Add a right-click Context Menu
        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        
        var settingsItem = new System.Windows.Forms.ToolStripMenuItem("Settings");
        settingsItem.Click += (s, args) => ShowSettingsWindow();
        contextMenu.Items.Add(settingsItem);
        
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        
        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit QuickScope");
        exitItem.Click += (s, args) => Current.Shutdown();
        contextMenu.Items.Add(exitItem);

        _trayIcon.ContextMenuStrip = contextMenu;
    }

    private void ShowSettingsWindow()
    {
        // If the window was closed from "X" need to recreate it
        // or just bring it to the front
        if (_settingsWindow == null || !_settingsWindow.IsLoaded)
        {
            _settingsWindow = new MainWindow();
            _settingsWindow.Show();
        }
        else
        {
            if (_settingsWindow.WindowState == WindowState.Minimized)
                _settingsWindow.WindowState = WindowState.Normal;
                
            _settingsWindow.Activate();
        }
    }

    private void OnThreadPreprocessMessage(ref MSG msg, ref bool handled)
    {
        // intercept the WM_HOTKEY message
        if (msg.message == WM_HOTKEY && msg.wParam.ToInt32() == HOTKEY_ID_CAPTURE)
        {
            handled = true;
            
            // Launch the capture safely in the background so we don't block the UI thread
            _ = TriggerCaptureAsync();
        }
    }

    private async Task TriggerCaptureAsync()
    {
        // we MUST hide it and give Windows a moment to physically erase it before capturing.
        if (_currentWindow is {IsVisible: true})
        {
            _currentWindow.Close();
            _currentWindow = null;
            await Task.Delay(50);
        }

        BitmapSource freezeFrame = CaptureService.CaptureInstant();
        
        // Cache the window instead of allocating a heavy WPF layout every time
        _currentWindow = new SelectionWindow();
        
        _currentWindow.ShowFreeze(freezeFrame);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // ghosts aren't real, no ghost icons
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        
        // jannie
        ComponentDispatcher.ThreadPreprocessMessage -= OnThreadPreprocessMessage;
        UnregisterHotKey(IntPtr.Zero, HOTKEY_ID_CAPTURE);

        base.OnExit(e);
    }
}