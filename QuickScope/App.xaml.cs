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
    private SelectionWindow _selectionWindow;
    
    // P/Invoke Signatures to Native Windows API
    [DllImport( "user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    
    [DllImport( "user32.dll")]
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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
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
        
        // this is bad
        _selectionWindow = new SelectionWindow();
        
        // force WPF to build the window handle invisibly
        //_selectionWindow.Opacity = 0;
        //_selectionWindow.Show();
        //_selectionWindow.Hide();
        //_selectionWindow.Opacity = 1;

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

    private void OnThreadPreprocessMessage(ref MSG msg, ref bool handled)
    {
        // intercept the WM_HOTKEY message
        if (msg.message == WM_HOTKEY && msg.wParam.ToInt32() == HOTKEY_ID_CAPTURE)
        {
            BitmapSource freezeFrame = CaptureService.CaptureInstant();
            _selectionWindow.ShowFreeze(freezeFrame);
            handled = true;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // jannies
        ComponentDispatcher.ThreadPreprocessMessage -= OnThreadPreprocessMessage;
        UnregisterHotKey(IntPtr.Zero, HOTKEY_ID_CAPTURE);

        base.OnExit(e);
    }
}